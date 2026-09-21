using UnityEngine;

using TMPro;

using SW.Popup;

using ProjectT.Timing;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 인벤토리의 열기·닫기·정지를 연결합니다. 아이템과 필터 동작은 후속 기능입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT", "ProjectT.Runtime", "InventoryUI")]
    public sealed class InventoryUI : SWPopupBase
    {
        #region 필드
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private BattlePopupPause popupPause;
        private bool initialized;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 표시와 정지 연결의 필수 참조입니다.
        /// </summary>
        public bool HasRequiredReferences => closeButton != null && coinLabel != null && popupPause != null;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 닫기 버튼을 한 번 연결하고 초기에는 인벤토리를 닫아 둡니다.
        /// </summary>
        public void Initialize(BattlePauseController controller)
        {
            Hide();
            popupPause.Configure(controller);
            if (!initialized)
            {
                closeButton.onClick.AddListener(Hide);
                initialized = true;
            }
        }

        /// <summary>
        /// 외부 화면과 동일한 잔액을 표시합니다. 미연결 상태는 대시입니다.
        /// </summary>
        public void PresentCoin(double? balance)
        {
            coinLabel.text = CurrencyPresentation.Format(balance);
        }

        /// <summary>
        /// 닫기 버튼의 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
        }

        #endregion // 초기화
    }
}
