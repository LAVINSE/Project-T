using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Popup;

using ProjectT.Battle;

namespace ProjectT.UI
{
    /// <summary>
    /// 인벤토리를 열고 닫으며 열려 있는 동안 전투를 정지합니다. 아이템과 필터 동작은 후속 기능입니다.
    /// </summary>
    public sealed class InventoryUI : SWPopupBase
    {
        #region 필드
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text coinLabel;
        private BattleTimeController timeController;
        private IDisposable pauseLease;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 전투 정지 관리자를 연결하고 닫힌 상태로 시작합니다.
        /// </summary>
        public void Initialize(BattleTimeController controller)
        {
            timeController = controller;
            Hide();
        }

        /// <summary>
        /// 닫기 버튼을 연결합니다.
        /// </summary>
        private void Awake()
        {
            closeButton.onClick.AddListener(Hide);
        }

        /// <summary>
        /// 팝업이 열리면 전투 정지를 요청합니다.
        /// </summary>
        private void OnEnable()
        {
            if (pauseLease == null && timeController != null)
            {
                pauseLease = timeController.Pause();
            }
        }

        /// <summary>
        /// 팝업이 닫히면 이 팝업의 정지 요청을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            pauseLease?.Dispose();
            pauseLease = null;
        }

        /// <summary>
        /// 닫기 버튼 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 외부 화면과 동일한 잔액을 표시합니다. 미연결 상태는 대시입니다.
        /// </summary>
        public void PresentCoin(double? balance)
        {
            coinLabel.text = balance.ExToCurrencyText();
        }

        #endregion // 표시
    }
}
