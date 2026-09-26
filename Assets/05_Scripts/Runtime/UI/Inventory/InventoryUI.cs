using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using TMPro;

using SW.Popup;
using SW.Util;

using ProjectT.Data;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 보관소와 필터·스크롤·상세·장착·버리기 입력을 연결합니다. 전투 시간은 변경하지 않습니다.
    /// </summary>
    public sealed class InventoryUI : SWPopupBase, IPointerClickHandler
    {
        #region 필드
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private WindowDragHandleUI dragHandle;
        [SerializeField] private ScrollRect itemScroll;
        [SerializeField] private InventoryItemSlotUI slotPrefab;
        [SerializeField] private InventoryCategoryUI[] categories;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private InventorySlotInfoUI tooltip;
        [SerializeField] private InventoryDiscardPopup discardPopup;
        [SerializeField] private RectTransform discardSlotRect;
        [SerializeField] private RectTransform carriedRect;
        [SerializeField] private Image carriedImage;
        [SerializeField] private TMP_Text carriedText;
        [SerializeField] private TMP_Text carriedCountText;
        private readonly List<InventoryItemSlotUI> slots = new List<InventoryItemSlotUI>();
        private readonly List<int> slotIndices = new List<int>();
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();
        private InventoryStore inventory;
        private InventoryCategoryUI filter;
        private InventoryItemSlotUI carriedSlot;
        private InventoryStack carriedItem;
        private InventoryItemSlotUI hoveredSlot;
        private InputAction pointerPosition;
        private InputAction cancel;
        private InputAction releaseItem;
        private Camera eventCamera;
        private bool subscribed;
        private bool rightClickConsumed;
        private int consumedInputFrame = -1;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 아이템 조작과 확인창에 사용한 입력이 필드 선택·이동에도 전달되지 않게 합니다.
        /// </summary>
        public bool BlocksFieldInput => consumedInputFrame == Time.frameCount
            || (IsVisible && (carriedSlot != null || discardPopup.IsOpen));

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 보관소와 표시 데이터를 연결합니다. 보관소가 준비되지 않으면 기능을 켜지 않습니다.
        /// </summary>
        public void Initialize(InventoryStore store, ColorData colors, SpriteData sprites)
        {
            Hide();
            if (subscribed)
            {
                return;
            }

            if (store == null)
            {
                SWLog.LogWarning("[InventoryUI] 연결 중단: 보관소가 준비되지 않았습니다.");
                return;
            }

            inventory = store;
            tooltip.Initialize(colors, sprites);
            discardPopup.Initialize(colors, sprites);
            discardPopup.Closed += CancelCarry;
            dragHandle.Initialize(panelRect, CanMoveWindow, HideTooltip);
            filter = categories[0];
            foreach (InventoryCategoryUI category in categories)
            {
                category.Selected += ChangeFilter;
            }

            foreach (InventoryItemSlotUI slot in itemScroll.content.GetComponentsInChildren<InventoryItemSlotUI>(true))
            {
                ConnectSlot(slot);
            }

            pointerPosition = new InputAction("InventoryPointer", InputActionType.PassThrough, "<Pointer>/position");
            cancel = new InputAction("InventoryCancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Mouse>/rightButton");
            releaseItem = new InputAction("InventoryRelease", InputActionType.Button, "<Mouse>/leftButton");
            pointerPosition.performed += MovePointer;
            cancel.performed += CancelInput;
            cancel.canceled += OnCancelReleased;
            releaseItem.canceled += OnPointerReleased;
            closeButton.onClick.AddListener(Hide);
            itemScroll.onValueChanged.AddListener(OnScrolled);
            inventory.Changed += OnInventoryChanged;
            subscribed = true;
            Refresh();
        }

        /// <summary>
        /// 슬롯 입력 알림을 한 번 연결합니다.
        /// </summary>
        private void ConnectSlot(InventoryItemSlotUI slot)
        {
            slots.Add(slot);
            slot.Hovered += ShowTooltip;
            slot.Exited += HideTooltip;
            slot.Picked += Pick;
            slot.DragStarted += BeginCarry;
            slot.Dropped += Drop;
            slot.Used += Use;
        }

        /// <summary>
        /// 포인터 입력을 시작합니다. 인벤토리를 열어도 전투는 계속 진행합니다.
        /// </summary>
        private void OnEnable()
        {
            if (!subscribed)
            {
                return;
            }

            pointerPosition.Enable();
            cancel.Enable();
            releaseItem.Enable();
            Refresh();
        }

        /// <summary>
        /// 닫기 연출 시작 시 임시 선택과 모달을 취소합니다.
        /// </summary>
        protected override void OnHide()
        {
            if (subscribed)
            {
                dragHandle.CancelDrag();
                CancelCarry();
                discardPopup.Hide();
                pointerPosition.Disable();
                cancel.Disable();
                releaseItem.Disable();
            }
        }

        /// <summary>
        /// 비활성화 시 임시 선택과 입력을 취소합니다.
        /// </summary>
        private void OnDisable()
        {
            OnHide();
        }

        /// <summary>
        /// 창 전환 중에는 집은 아이템을 되돌리고 파괴 요청을 취소합니다.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && subscribed)
            {
                dragHandle.CancelDrag();
                CancelCarry();
                discardPopup.Hide();
            }
        }

        /// <summary>
        /// 보관소·슬롯·입력 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (!subscribed)
            {
                return;
            }

            inventory.Changed -= OnInventoryChanged;
            discardPopup.Closed -= CancelCarry;
            closeButton.onClick.RemoveListener(Hide);
            itemScroll.onValueChanged.RemoveListener(OnScrolled);
            foreach (InventoryCategoryUI category in categories)
            {
                category.Selected -= ChangeFilter;
            }

            foreach (InventoryItemSlotUI slot in slots)
            {
                if (slot != null)
                {
                    slot.Hovered -= ShowTooltip;
                    slot.Exited -= HideTooltip;
                    slot.Picked -= Pick;
                    slot.DragStarted -= BeginCarry;
                    slot.Dropped -= Drop;
                    slot.Used -= Use;
                }
            }

            pointerPosition.performed -= MovePointer;
            cancel.performed -= CancelInput;
            cancel.canceled -= OnCancelReleased;
            releaseItem.canceled -= OnPointerReleased;
            pointerPosition.Dispose();
            cancel.Dispose();
            releaseItem.Dispose();
        }

        #endregion // 초기화

        #region 창 이동
        /// <summary>
        /// 인벤토리가 열려 있고 아이템 집기·파괴 확인 중이 아닐 때만 제목 드래그를 허용합니다.
        /// </summary>
        private bool CanMoveWindow()
        {
            return subscribed && IsVisible && carriedSlot == null && !discardPopup.IsOpen;
        }

        #endregion // 창 이동

        #region 목록과 상세
        /// <summary>
        /// 전투 중 보상이 들어와도 집기와 입력 중인 버리기 수량을 보존합니다. 사라진 아이템은 집기를 종료합니다.
        /// </summary>
        private void OnInventoryChanged()
        {
            string identifier = carriedItem?.Identifier;
            bool confirmingDiscard = discardPopup.IsOpen;
            string quantity = discardPopup.RequestedQuantity;
            Camera camera = eventCamera;
            Refresh();
            if (identifier == null || !IsVisible)
            {
                return;
            }
            foreach (InventoryItemSlotUI slot in slots)
            {
                if (slot.Item?.Identifier != identifier)
                {
                    continue;
                }
                StartCarry(slot, pointerPosition.ReadValue<Vector2>(), camera);
                if (confirmingDiscard)
                {
                    carriedRect.gameObject.SetActive(false);
                    discardPopup.Present(inventory, identifier, quantity);
                }
                return;
            }
        }

        /// <summary>
        /// 외부 화면과 동일한 전투 코인 잔액을 표시합니다.
        /// </summary>
        public void PresentCoin(double? balance)
        {
            coinText.text = balance.ExToCurrencyText();
        }

        /// <summary>
        /// 저장된 배치와 빈칸을 표시하고 다른 분류의 아이템만 제외합니다. 이동할 여유 칸을 유지합니다.
        /// </summary>
        private void Refresh()
        {
            CancelCarry();
            discardPopup.Hide();
            RebuildSlotIndices();
            PresentSlots();
            emptyText.gameObject.SetActive(false);
            foreach (InventoryCategoryUI category in categories)
            {
                category.Present(category == filter);
            }

            LayoutRebuilder.MarkLayoutForRebuild(itemScroll.content);
        }

        /// <summary>
        /// 화면 슬롯을 저장 위치에 대응시키고 보이지 않는 다른 분류의 아이템은 이동 대상에서 제외합니다.
        /// </summary>
        private void RebuildSlotIndices()
        {
            slotIndices.Clear();
            for (int index = 0; index < inventory.Slots.Count; index++)
            {
                InventoryStack item = inventory.Slots[index];
                if (item != null && !MatchesFilter(item))
                {
                    continue;
                }
                slotIndices.Add(index);
            }

            int visibleCount = Mathf.Max(ProjectDefine.Inventory.MinimumVisibleSlots, slotIndices.Count + 1);
            int nextIndex = inventory.Slots.Count;
            while (slotIndices.Count < visibleCount)
            {
                slotIndices.Add(nextIndex++);
            }
        }

        /// <summary>
        /// 배치에 필요한 슬롯을 확보하고 아이템 또는 빈칸을 표시합니다.
        /// </summary>
        private void PresentSlots()
        {
            int visibleCount = slotIndices.Count;
            while (slots.Count < visibleCount)
            {
                ConnectSlot(Instantiate(slotPrefab, itemScroll.content));
            }

            for (int index = 0; index < slots.Count; index++)
            {
                InventoryStack item = index < visibleCount && slotIndices[index] < inventory.Slots.Count
                    ? inventory.Slots[slotIndices[index]] : null;
                slots[index].Present(item);
                slots[index].gameObject.SetActive(index < visibleCount);
            }
        }

        /// <summary>
        /// 전체 또는 분류 코드가 같은 아이템을 표시합니다. 분류 없는 아이템은 기타에 표시합니다.
        /// </summary>
        private bool MatchesFilter(InventoryStack item)
        {
            if (filter.Category == null)
            {
                return true;
            }

            string code = item.Category == null ? ProjectDefine.Inventory.OtherCategory : item.Category.CodeName;
            return filter.Category.CodeName == code;
        }

        /// <summary>
        /// 필터를 바꾸고 스크롤을 처음으로 이동합니다. 모달 중에는 바꾸지 않습니다.
        /// </summary>
        private void ChangeFilter(InventoryCategoryUI category)
        {
            if (discardPopup.IsOpen)
            {
                return;
            }

            filter = category;
            Refresh();
            itemScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 스크롤하면 상세만 닫습니다. 집은 아이템은 유지하여 다른 줄로 옮길 수 있습니다.
        /// </summary>
        private void OnScrolled(Vector2 position)
        {
            HideTooltip();
        }

        /// <summary>
        /// 집기·모달 중이 아니면 작은 상세를 표시합니다.
        /// </summary>
        private void ShowTooltip(InventoryItemSlotUI slot, PointerEventData eventData)
        {
            if (carriedSlot == null && !discardPopup.IsOpen)
            {
                HideTooltip();
                hoveredSlot = slot;
                slot.SetHovered(true);
                tooltip.Present(slot.Item, eventData.position, eventData.enterEventCamera);
            }
        }

        /// <summary>
        /// 상세 표시를 닫습니다.
        /// </summary>
        private void HideTooltip()
        {
            if (hoveredSlot != null)
            {
                hoveredSlot.SetHovered(false);
                hoveredSlot = null;
            }
            tooltip.Hide();
        }

        #endregion // 목록과 상세

        #region 집기와 놓기
        /// <summary>
        /// 슬롯 클릭으로 아이템을 집거나 현재 집은 아이템을 놓습니다.
        /// </summary>
        private void Pick(InventoryItemSlotUI slot, PointerEventData eventData)
        {
            if (consumedInputFrame == Time.frameCount || discardPopup.IsOpen || !IsVisible)
            {
                return;
            }

            if (carriedSlot != null)
            {
                Drop(eventData);
                return;
            }

            BeginCarry(slot, eventData);
        }

        /// <summary>
        /// 슬롯 표시를 비우고 아이템과 수량을 커서로 옮깁니다. 보관소의 수량과 배치는 아직 변경하지 않습니다.
        /// </summary>
        private void BeginCarry(InventoryItemSlotUI slot, PointerEventData eventData)
        {
            if (consumedInputFrame == Time.frameCount || carriedSlot != null || slot.Item == null || discardPopup.IsOpen || !IsVisible)
            {
                return;
            }

            StartCarry(slot, eventData.position, eventData.pressEventCamera);
        }

        /// <summary>
        /// 현재 슬롯을 비우고 포인터 표시를 구성합니다. 목록 갱신 후에도 같은 입력 상태를 복원할 수 있습니다.
        /// </summary>
        private void StartCarry(InventoryItemSlotUI slot, Vector2 position, Camera camera)
        {
            HideTooltip();
            carriedSlot = slot;
            carriedItem = slot.Item;
            eventCamera = camera;
            carriedImage.sprite = carriedItem.Icon;
            carriedImage.enabled = carriedItem.Icon != null;
            carriedText.text = carriedImage.enabled ? string.Empty : carriedItem.DisplayName;
            carriedCountText.text = carriedItem.Count.ToString("N0");
            carriedRect.sizeDelta = ((RectTransform)slot.transform).rect.size;
            slot.Present(null);
            carriedRect.gameObject.SetActive(true);
            carriedRect.SetAsLastSibling();
            PlaceCarriedItem(position);
        }

        /// <summary>
        /// 필드를 가리는 투명 UI 없이도 클릭 집기의 놓기를 처리합니다. 실제 UI 이벤트와 같은 프레임에 중복 실행하지 않습니다.
        /// </summary>
        private void OnPointerReleased(InputAction.CallbackContext context)
        {
            if (carriedSlot == null || discardPopup.IsOpen || !IsVisible || EventSystem.current == null)
            {
                return;
            }
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = pointerPosition.ReadValue<Vector2>(),
                button = PointerEventData.InputButton.Left
            };
            pointerHits.Clear();
            EventSystem.current.RaycastAll(pointer, pointerHits);
            if (pointerHits.Count > 0)
            {
                pointer.pointerCurrentRaycast = pointerHits[0];
            }
            Drop(pointer);
        }

        /// <summary>
        /// 포인터 위치가 바뀔 때만 집은 아이템 표시를 이동합니다.
        /// </summary>
        private void MovePointer(InputAction.CallbackContext context)
        {
            if (carriedSlot != null && !discardPopup.IsOpen)
            {
                PlaceCarriedItem(context.ReadValue<Vector2>());
            }
        }

        /// <summary>
        /// 화면 좌표를 부모 평면으로 변환해 아이콘 중심을 커서에 맞춥니다. 팝업용 여백과 화면 가장자리 보정은 적용하지 않습니다.
        /// </summary>
        private void PlaceCarriedItem(Vector2 position)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)carriedRect.parent, position, eventCamera, out Vector3 worldPosition))
            {
                carriedRect.position = worldPosition;
            }
        }

        /// <summary>
        /// 왼쪽 바탕 클릭을 놓기로 처리합니다. 모달 뒤 입력은 무시합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Drop(eventData);
            }
        }

        /// <summary>
        /// 버리기 위치는 확인을 열고, 유효한 슬롯은 이동·교환하며 나머지 위치는 원래 슬롯으로 돌려놓습니다.
        /// </summary>
        private void Drop(PointerEventData eventData)
        {
            if (carriedSlot == null || discardPopup.IsOpen || !IsVisible)
            {
                return;
            }

            string identifier = carriedItem.Identifier;
            consumedInputFrame = Time.frameCount;
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            CharacterEquipmentSlotUI equipmentSlot = hit != null ? hit.GetComponentInParent<CharacterEquipmentSlotUI>() : null;
            if (equipmentSlot != null)
            {
                CancelCarry();
                equipmentSlot.TryEquip(identifier);
                return;
            }
            bool outside = !RectTransformUtility.RectangleContainsScreenPoint(panelRect, eventData.position, eventData.pressEventCamera);
            bool discard = discardSlotRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(discardSlotRect, eventData.position, eventData.pressEventCamera);
            if ((outside || discard) && carriedItem.Definition != null && carriedItem.Definition.IsBlueprint)
            {
                CancelCarry();
                ShowNotice("설계도는 버릴 수 없습니다. 오른쪽 클릭으로 사용하세요.");
                return;
            }

            if (outside || discard)
            {
                carriedRect.gameObject.SetActive(false);
                discardPopup.Present(inventory, identifier);
                if (!discardPopup.IsOpen)
                {
                    CancelCarry();
                }
                return;
            }

            int targetSlot = FindDropSlot(eventData);
            CancelCarry();
            if (targetSlot >= 0 && !inventory.TryMove(identifier, targetSlot, out string reason))
            {
                ShowNotice(reason);
            }
        }

        /// <summary>
        /// 오른쪽 클릭한 설계도를 사용해 제작법을 해금합니다. 성공하면 설계도가 사라지는 것으로 충분하므로 실패 사유만 표시합니다. 설계도가 아닌 아이템은 아직 사용 기능이 없어 무시합니다.
        /// </summary>
        private void Use(InventoryItemSlotUI slot)
        {
            if (consumedInputFrame == Time.frameCount || carriedSlot != null || discardPopup.IsOpen || !IsVisible
                || slot.Item?.Definition == null || !slot.Item.Definition.IsBlueprint)
            {
                return;
            }

            consumedInputFrame = Time.frameCount;
            if (!inventory.TryLearn(slot.Item.Identifier, out string reason))
            {
                ShowNotice(reason);
            }
        }

        /// <summary>
        /// 목록 아래 안내 영역에 결과를 표시합니다. 다음 목록 갱신 때 사라집니다.
        /// </summary>
        private void ShowNotice(string message)
        {
            emptyText.text = message;
            emptyText.gameObject.SetActive(true);
        }

        /// <summary>
        /// 실제 포인터가 맞은 현재 인벤토리 슬롯만 반환합니다. 마스크 밖·다른 버튼·슬롯 사이 빈 공간은 제외합니다.
        /// </summary>
        private int FindDropSlot(PointerEventData eventData)
        {
            GameObject hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit == null || !RectTransformUtility.RectangleContainsScreenPoint(
                itemScroll.viewport, eventData.position, eventData.pressEventCamera))
            {
                return -1;
            }

            InventoryItemSlotUI target = hit.GetComponentInParent<InventoryItemSlotUI>();
            int index = slots.IndexOf(target);
            return index >= 0 && index < slotIndices.Count ? slotIndices[index] : -1;
        }

        /// <summary>
        /// 취소 키는 모달·집기 순서로 취소하고, 남은 선택이 없을 때 Escape로 인벤토리를 닫습니다.
        /// </summary>
        private void CancelInput(InputAction.CallbackContext context)
        {
            bool consumed = dragHandle.IsDragging || discardPopup.IsOpen || carriedSlot != null;
            if (consumed || context.control.device is Keyboard)
            {
                consumedInputFrame = Time.frameCount;
            }
            rightClickConsumed = consumed && context.control.device is Mouse;
            if (dragHandle.IsDragging)
            {
                dragHandle.CancelDrag();
            }
            else if (discardPopup.IsOpen)
            {
                discardPopup.Hide();
            }
            else if (carriedSlot != null)
            {
                CancelCarry();
            }
            else if (context.control.device is Keyboard)
            {
                Hide();
            }
        }

        /// <summary>
        /// 취소에 사용한 우클릭을 뗀 프레임도 소비된 입력으로 표시해, 떼는 순간 처리되는 장비 해제로 이어지지 않게 합니다.
        /// </summary>
        private void OnCancelReleased(InputAction.CallbackContext context)
        {
            if (!rightClickConsumed)
            {
                return;
            }

            rightClickConsumed = false;
            consumedInputFrame = Time.frameCount;
        }

        /// <summary>
        /// 임시 선택·상세·포인터 표시를 비웁니다. 저장된 보유 수량은 변경하지 않습니다.
        /// </summary>
        private void CancelCarry()
        {
            if (carriedSlot != null)
            {
                carriedSlot.Present(inventory.Find(carriedItem.Identifier));
                carriedSlot = null;
                carriedItem = null;
            }

            carriedRect.gameObject.SetActive(false);
            HideTooltip();
        }

        #endregion // 집기와 놓기
    }
}
