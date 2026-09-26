using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using ProjectT.Data;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 보유량에 맞는 단일·복수 화면에서 버리기를 확인하고 저장 성공 때만 닫습니다.
    /// </summary>
    public sealed class InventoryDiscardPopup : MonoBehaviour, IPointerClickHandler
    {
        #region 필드
        [SerializeField] private GameObject singlePanel;
        [SerializeField] private GameObject multiplePanel;
        [SerializeField] private InventoryItemHeader singleHeader;
        [SerializeField] private InventoryItemHeader multipleHeader;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text[] messageTexts;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button maximumButton;
        [SerializeField] private Button[] confirmButtons;
        [SerializeField] private Button[] cancelButtons;
        private InventoryStore inventory;
        private InventoryStack selected;
        private ColorData colors;
        private SpriteData sprites;
        private bool confirming;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 버리기 확인창의 표시 여부입니다.
        /// </summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>
        /// 보유량 갱신 중에도 유지할 사용자의 수량 입력입니다.
        /// </summary>
        public string RequestedQuantity => quantityInput.text;

        /// <summary>
        /// 확인창이 닫히면 임시 집기를 종료하고 남은 아이템 표시를 복원하도록 알립니다.
        /// </summary>
        public event Action Closed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 공통 등급 표시 데이터를 전달받습니다.
        /// </summary>
        public void Initialize(ColorData colorData, SpriteData spriteData)
        {
            colors = colorData;
            sprites = spriteData;
            Hide();
        }

        /// <summary>
        /// 두 화면의 입력을 같은 버리기 처리에 연결합니다.
        /// </summary>
        private void Awake()
        {
            decreaseButton.onClick.AddListener(Decrease);
            increaseButton.onClick.AddListener(Increase);
            maximumButton.onClick.AddListener(Maximize);
            foreach (Button button in confirmButtons)
            {
                button.onClick.AddListener(Confirm);
            }
            foreach (Button button in cancelButtons)
            {
                button.onClick.AddListener(Hide);
            }
            quantityInput.onValueChanged.AddListener(ValidateQuantity);
        }

        /// <summary>
        /// 모든 버튼과 수량 입력의 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            decreaseButton.onClick.RemoveListener(Decrease);
            increaseButton.onClick.RemoveListener(Increase);
            maximumButton.onClick.RemoveListener(Maximize);
            foreach (Button button in confirmButtons)
            {
                button.onClick.RemoveListener(Confirm);
            }
            foreach (Button button in cancelButtons)
            {
                button.onClick.RemoveListener(Hide);
            }
            quantityInput.onValueChanged.RemoveListener(ValidateQuantity);
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 보유량이 1개이면 단일 화면, 그 이상이면 수량 선택 화면을 표시합니다.
        /// </summary>
        public void Present(InventoryStore store, string identifier, string quantity = "1")
        {
            selected = store?.Find(identifier);
            if (selected == null)
            {
                Hide();
                return;
            }

            inventory = store;
            singleHeader.Present(selected, colors, sprites);
            multipleHeader.Present(selected, colors, sprites);
            countText.text = "보유 " + selected.Count.ToString("N0") + "개";
            singlePanel.SetActive(selected.Count == 1);
            multiplePanel.SetActive(selected.Count > 1);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            quantityInput.SetTextWithoutNotify(quantity);
            ValidateQuantity(quantityInput.text);
        }

        /// <summary>
        /// 수량을 변경하지 않고 요청을 취소합니다.
        /// </summary>
        public void Hide()
        {
            selected = null;
            inventory = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 취소·확인·외부 비활성화에서 모두 임시 선택을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            selected = null;
            inventory = null;
            Closed?.Invoke();
        }

        /// <summary>
        /// 모달 배경의 클릭을 소비합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 활성 화면에 경고 또는 저장 실패 이유를 표시합니다.
        /// </summary>
        private void ShowMessage(string message)
        {
            foreach (TMP_Text text in messageTexts)
            {
                text.text = message;
            }
        }

        #endregion // 표시

        #region 수량과 버리기
        /// <summary>
        /// 선택 수량을 검사합니다. 보유량을 벗어난 입력은 거절합니다.
        /// </summary>
        private bool TryGetQuantity(out long quantity)
        {
            return long.TryParse(quantityInput.text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity)
                && selected != null && quantity >= 1 && quantity <= selected.Count;
        }

        /// <summary>
        /// 유효한 수량일 때만 버리기를 허용합니다.
        /// </summary>
        private void ValidateQuantity(string value)
        {
            bool valid = TryGetQuantity(out long quantity);
            foreach (Button button in confirmButtons)
            {
                button.interactable = valid && !confirming;
            }
            decreaseButton.interactable = valid && quantity > 1;
            increaseButton.interactable = valid && quantity < selected.Count;
            maximumButton.interactable = selected != null && !confirming;
            ShowMessage(valid ? "버린 아이템은 되돌릴 수 없습니다." : "1개부터 보유 수량까지 입력하세요.");
        }

        /// <summary>
        /// 선택 수량을 한 개 줄입니다.
        /// </summary>
        private void Decrease()
        {
            if (TryGetQuantity(out long quantity) && quantity > 1)
            {
                quantityInput.text = (quantity - 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 선택 수량을 보유량 이내에서 한 개 늘립니다.
        /// </summary>
        private void Increase()
        {
            if (TryGetQuantity(out long quantity) && quantity < selected.Count)
            {
                quantityInput.text = (quantity + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 최대 보유량을 선택합니다.
        /// </summary>
        private void Maximize()
        {
            if (selected != null)
            {
                quantityInput.text = selected.Count.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 저장 성공 후 닫으며 실패하면 보유량을 유지하고 이유를 표시합니다.
        /// </summary>
        private void Confirm()
        {
            if (confirming || inventory == null || !TryGetQuantity(out long quantity))
            {
                return;
            }

            confirming = true;
            ValidateQuantity(quantityInput.text);
            bool success = inventory.TryDiscard(selected.Identifier, quantity, out string reason);
            confirming = false;
            if (success)
            {
                Hide();
                return;
            }

            ValidateQuantity(quantityInput.text);
            ShowMessage(reason);
        }

        #endregion // 수량과 버리기
    }
}
