using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 파괴 수량을 1개부터 보유 수량까지 선택하고 확인 시에만 저장을 요청하는 모달입니다.
    /// </summary>
    public sealed class ItemDiscardPopupUI : MonoBehaviour, IPointerClickHandler
    {
        #region 필드
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text issueText;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button maximumButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private InventoryStore inventory;
        private InventoryStack selected;
        private bool confirming;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 파괴 확인창이 열려 있는지 반환합니다.
        /// </summary>
        public bool IsOpen => gameObject.activeSelf;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 수량 입력과 버튼을 연결합니다.
        /// </summary>
        private void Awake()
        {
            decreaseButton.onClick.AddListener(Decrease);
            increaseButton.onClick.AddListener(Increase);
            maximumButton.onClick.AddListener(Maximize);
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Hide);
            quantityInput.onValueChanged.AddListener(ValidateQuantity);
        }

        /// <summary>
        /// 수량 입력과 버튼 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            decreaseButton.onClick.RemoveListener(Decrease);
            increaseButton.onClick.RemoveListener(Increase);
            maximumButton.onClick.RemoveListener(Maximize);
            confirmButton.onClick.RemoveListener(Confirm);
            cancelButton.onClick.RemoveListener(Hide);
            quantityInput.onValueChanged.RemoveListener(ValidateQuantity);
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 현재 보유 종류의 파괴 확인을 엽니다. 없어진 아이템이면 열지 않습니다.
        /// </summary>
        public void Present(InventoryStore store, string identifier)
        {
            selected = store?.Find(identifier);
            if (selected == null)
            {
                Hide();
                return;
            }

            inventory = store;
            gameObject.SetActive(true);
            titleText.text = "아이템을 파괴할까요?";
            messageText.text = selected.DisplayName + "\n보유 " + selected.Count.ToString("N0") + "개 · 파괴한 아이템은 복구할 수 없습니다.";
            quantityInput.SetTextWithoutNotify("1");
            ValidateQuantity(quantityInput.text);
        }

        /// <summary>
        /// 파괴 요청을 취소하고 선택을 비웁니다. 보유 수량은 변경하지 않습니다.
        /// </summary>
        public void Hide()
        {
            selected = null;
            inventory = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 모달 영역의 클릭이 인벤토리로 전달되지 않도록 소비합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
        }

        #endregion // 표시

        #region 수량과 파괴
        /// <summary>
        /// 입력한 수량이 현재 선택 범위에 있을 때만 성공합니다.
        /// </summary>
        private bool TryGetQuantity(out long quantity)
        {
            return long.TryParse(quantityInput.text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity)
                && selected != null
                && quantity >= 1
                && quantity <= selected.Count;
        }

        /// <summary>
        /// 유효한 수량일 때만 확인을 허용합니다. 잘못된 입력은 삭제 없이 안내합니다.
        /// </summary>
        private void ValidateQuantity(string value)
        {
            bool valid = TryGetQuantity(out long quantity);
            confirmButton.interactable = valid && !confirming;
            decreaseButton.interactable = valid && quantity > 1;
            increaseButton.interactable = valid && quantity < selected.Count;
            issueText.text = valid ? string.Empty : "1개부터 보유 수량까지 정수로 입력하세요.";
        }

        /// <summary>
        /// 수량을 한 개 줄입니다. 범위를 벗어나면 변경하지 않습니다.
        /// </summary>
        private void Decrease()
        {
            if (TryGetQuantity(out long quantity) && quantity > 1)
            {
                quantityInput.text = (quantity - 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 수량을 한 개 늘립니다. 보유 수량을 초과하지 않습니다.
        /// </summary>
        private void Increase()
        {
            if (TryGetQuantity(out long quantity) && quantity < selected.Count)
            {
                quantityInput.text = (quantity + 1).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 파괴할 수량을 보유 수량 전체로 선택합니다.
        /// </summary>
        private void Maximize()
        {
            if (selected != null)
            {
                quantityInput.text = selected.Count.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 확인한 수량의 저장 성공 후 창을 닫습니다. 저장 실패는 수량을 유지하고 원인을 표시합니다.
        /// </summary>
        private void Confirm()
        {
            if (confirming || inventory == null || !TryGetQuantity(out long quantity))
            {
                return;
            }

            confirming = true;
            confirmButton.interactable = false;
            bool success = inventory.TryDiscard(selected.Identifier, quantity, out string reason);
            confirming = false;
            if (success)
            {
                Hide();
                return;
            }

            ValidateQuantity(quantityInput.text);
            issueText.text = reason;
        }

        #endregion // 수량과 파괴
    }
}
