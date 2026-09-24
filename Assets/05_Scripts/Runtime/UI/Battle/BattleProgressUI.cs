using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 전투 단계별 진행 버튼을 표시합니다. 전투 상태는 직접 변경하지 않습니다.
    /// </summary>
    public sealed class BattleProgressUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Button advanceButton;
        [SerializeField] private TMP_Text advanceText;
        [SerializeField] private TMP_Text phaseText;
        private bool subscribed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 사용자가 현재 단계의 진행을 요청했을 때 발생합니다.
        /// </summary>
        public event Action AdvanceRequested;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 진행 버튼의 클릭 알림을 연결합니다. 이미 연결했으면 다시 구독하지 않습니다.
        /// </summary>
        public void Initialize()
        {
            if (subscribed)
            {
                return;
            }

            advanceButton.onClick.AddListener(RequestAdvance);
            subscribed = true;
        }

        /// <summary>
        /// 연결한 버튼의 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (subscribed)
            {
                advanceButton.onClick.RemoveListener(RequestAdvance);
            }
        }

        #endregion // 초기화

        #region 표시와 요청
        /// <summary>
        /// 단계에 맞는 이름과 버튼을 표시합니다. 정지 중에는 진행할 수 없습니다.
        /// </summary>
        public void Present(BattlePhase phase, bool canAdvance, bool paused)
        {
            phaseText.text = phase.ExToPhaseText();
            if (paused && phase != BattlePhase.Victory && phase != BattlePhase.Defeat)
            {
                phaseText.text += " · 일시정지";
            }

            bool showButton = phase == BattlePhase.Preparation || phase == BattlePhase.RoundBreak || phase == BattlePhase.FinalRest;
            advanceButton.gameObject.SetActive(showButton);
            advanceButton.interactable = canAdvance && showButton;
            advanceText.text = phase == BattlePhase.FinalRest ? "결과 보기" : "전투 시작";
        }

        /// <summary>
        /// 사용 가능한 버튼의 클릭만 상위 화면으로 전달합니다.
        /// </summary>
        private void RequestAdvance()
        {
            if (advanceButton.interactable)
            {
                AdvanceRequested?.Invoke();
            }
        }

        #endregion // 표시와 요청
    }
}
