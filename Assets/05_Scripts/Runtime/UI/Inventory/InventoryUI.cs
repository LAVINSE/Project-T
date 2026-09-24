using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using TMPro;

using SW.Popup;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 보관소와 필터·스크롤·상세·파괴 화면을 연결하고 열린 동안 전투 정지를 유지합니다.
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
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private ItemTooltipUI tooltip;
        [SerializeField] private ItemDiscardPopupUI discardPopup;
        [SerializeField] private RectTransform carriedRect;
        [SerializeField] private Image carriedImage;
        [SerializeField] private TMP_Text carriedText;
        private readonly List<InventoryItemSlotUI> slots = new List<InventoryItemSlotUI>();
        private BattleTimeController timeController;
        private InventoryStore inventory;
        private InventoryCategoryUI filter;
        private InventoryItemSlotUI carriedSlot;
        private IDisposable pauseLease;
        private InputAction pointerPosition;
        private InputAction cancel;
        private Camera eventCamera;
        private bool subscribed;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 보관소와 정지 관리자를 연결합니다. 보관소가 준비되지 않으면 기능을 켜지 않습니다.
        /// </summary>
        public void Initialize(BattleTimeController controller, InventoryStore store)
        {
            Hide();
            if (subscribed)
            {
                return;
            }

            if (controller == null || store == null)
            {
                SWLog.LogWarning("[InventoryUI] 연결 중단: 보관소와 전투 시간 관리자가 준비되지 않았습니다.");
                return;
            }

            timeController = controller;
            inventory = store;
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
            pointerPosition.performed += MovePointer;
            cancel.performed += CancelInput;
            closeButton.onClick.AddListener(Hide);
            itemScroll.onValueChanged.AddListener(OnScrolled);
            inventory.Changed += Refresh;
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
            slot.Dropped += Drop;
        }

        /// <summary>
        /// 전투 정지 요청과 포인터 입력을 시작합니다.
        /// </summary>
        private void OnEnable()
        {
            if (!subscribed)
            {
                return;
            }

            pauseLease = timeController.Pause();
            pointerPosition.Enable();
            cancel.Enable();
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
            }
        }

        /// <summary>
        /// 비활성화 시 임시 선택을 취소하고 이 화면의 정지 요청만 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            OnHide();
            pauseLease?.Dispose();
            pauseLease = null;
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

            inventory.Changed -= Refresh;
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
                    slot.Dropped -= Drop;
                }
            }

            pointerPosition.performed -= MovePointer;
            cancel.performed -= CancelInput;
            pointerPosition.Dispose();
            cancel.Dispose();
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
        /// 외부 화면과 동일한 전투 코인 잔액을 표시합니다.
        /// </summary>
        public void PresentCoin(double? balance)
        {
            coinText.text = balance.ExToCurrencyText();
        }

        /// <summary>
        /// 현재 분류의 아이템과 빈칸을 합쳐 기본 36칸 이상 표시합니다. 종류가 더 많으면 슬롯을 추가하며 보관 한도는 두지 않습니다.
        /// </summary>
        private void Refresh()
        {
            CancelCarry();
            discardPopup.Hide();
            int displayed = 0;
            foreach (InventoryStack item in inventory.Items)
            {
                if (!MatchesFilter(item))
                {
                    continue;
                }

                if (displayed == slots.Count)
                {
                    ConnectSlot(Instantiate(slotPrefab, itemScroll.content));
                }

                slots[displayed].Present(item);
                displayed++;
            }

            int visibleCount = Mathf.Max(ProjectDefine.Inventory.MinimumVisibleSlots, displayed);
            while (slots.Count < visibleCount)
            {
                ConnectSlot(Instantiate(slotPrefab, itemScroll.content));
            }

            for (int index = displayed; index < slots.Count; index++)
            {
                slots[index].Present(null);
                slots[index].gameObject.SetActive(index < visibleCount);
            }

            emptyText.gameObject.SetActive(false);
            foreach (InventoryCategoryUI category in categories)
            {
                category.Present(category == filter);
            }

            LayoutRebuilder.MarkLayoutForRebuild(itemScroll.content);
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
        /// 스크롤로 다른 아이템과 상세가 겹치지 않도록 임시 표시를 취소합니다.
        /// </summary>
        private void OnScrolled(Vector2 position)
        {
            CancelCarry();
        }

        /// <summary>
        /// 집기·모달 중이 아니면 작은 상세를 표시합니다.
        /// </summary>
        private void ShowTooltip(InventoryItemSlotUI slot, PointerEventData eventData)
        {
            if (carriedSlot == null && !discardPopup.IsOpen)
            {
                tooltip.Present(slot.Item, eventData.position, eventData.enterEventCamera);
            }
        }

        /// <summary>
        /// 상세 표시를 닫습니다.
        /// </summary>
        private void HideTooltip()
        {
            tooltip.Hide();
        }

        #endregion // 목록과 상세

        #region 집기와 놓기
        /// <summary>
        /// 아이템을 집어 포인터에 표시합니다. 이미 집은 상태에서 클릭하면 원래 슬롯으로 돌려놓습니다.
        /// </summary>
        private void Pick(InventoryItemSlotUI slot, PointerEventData eventData)
        {
            if (discardPopup.IsOpen || !IsVisible)
            {
                return;
            }

            if (carriedSlot != null)
            {
                CancelCarry();
                return;
            }

            HideTooltip();
            carriedSlot = slot;
            slot.Select(true);
            eventCamera = eventData.pressEventCamera;
            carriedImage.sprite = slot.Item.Icon;
            carriedImage.enabled = slot.Item.Icon != null;
            carriedText.text = carriedImage.enabled ? string.Empty : slot.Item.DisplayName;
            carriedRect.gameObject.SetActive(true);
            carriedRect.ExPlaceInside(eventData.position, eventCamera, new Vector2(16f, -16f));
            instructionText.text = "인벤토리 밖에 놓으면 파괴 확인 · 우클릭으로 취소";
        }

        /// <summary>
        /// 포인터 위치가 바뀔 때만 집은 아이템 표시를 이동합니다.
        /// </summary>
        private void MovePointer(InputAction.CallbackContext context)
        {
            if (carriedSlot != null)
            {
                carriedRect.ExPlaceInside(context.ReadValue<Vector2>(), eventCamera, new Vector2(16f, -16f));
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
        /// 인벤토리 밖에 놓으면 수량 확인을 열고, 안에 놓으면 보유 상태로 되돌립니다.
        /// </summary>
        private void Drop(PointerEventData eventData)
        {
            if (carriedSlot == null || discardPopup.IsOpen || !IsVisible)
            {
                return;
            }

            string identifier = carriedSlot.Item.Identifier;
            bool outside = !RectTransformUtility.RectangleContainsScreenPoint(panelRect, eventData.position, eventData.pressEventCamera);
            CancelCarry();
            if (outside)
            {
                discardPopup.Present(inventory, identifier);
            }
        }

        /// <summary>
        /// 취소 키는 모달·집기 순서로 취소하고, 남은 선택이 없을 때 Escape로 인벤토리를 닫습니다.
        /// </summary>
        private void CancelInput(InputAction.CallbackContext context)
        {
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
        /// 임시 선택·상세·포인터 표시를 비웁니다. 저장된 보유 수량은 변경하지 않습니다.
        /// </summary>
        private void CancelCarry()
        {
            if (carriedSlot != null)
            {
                carriedSlot.Select(false);
                carriedSlot = null;
            }

            carriedRect.gameObject.SetActive(false);
            HideTooltip();
            instructionText.text = "마우스 올림: 상세 · 클릭 후 밖에 놓기: 파괴";
        }

        #endregion // 집기와 놓기
    }
}
