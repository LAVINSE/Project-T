using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Popup;
using SW.SkillTree;
using SW.Util;

namespace ProjectT.UI
{
    /// <summary>
    /// 거점의 공통 연구 창입니다. 트리 표시·구매·환불은 SWSkillTreeView가 처리하고 이 창은 열기·닫기와 전체 초기화, 저장 안내를 맡습니다.
    /// </summary>
    public sealed class HubResearchUI : SWPopupBase
    {
        #region 필드
        [SerializeField] private SWSkillTreeView treeView;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text messageText;
        private SWSkillTreeSystem system;
        private Func<string> readSaveIssue;
        private bool subscribed;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 연구 트리를 화면에 연결합니다. 저장 안내는 조립 지점이 전달한 조회 함수로 읽습니다. 이미 연결했으면 다시 구독하지 않습니다.
        /// </summary>
        public void Initialize(SWSkillTreeSystem researchSystem, Func<string> saveIssue)
        {
            Hide();
            if (subscribed)
            {
                return;
            }

            system = researchSystem;
            readSaveIssue = saveIssue;
            treeView.Bind(system);
            closeButton.onClick.AddListener(Hide);
            resetButton.onClick.AddListener(ResetResearch);
            system.Changed += PresentSaveIssue;
            subscribed = true;
        }

        /// <summary>
        /// 열 때 이전 안내를 지우고 저장 문제만 다시 표시합니다.
        /// </summary>
        protected override void OnShow()
        {
            messageText.text = string.Empty;
            PresentSaveIssue();
        }

        #endregion // 초기화

        #region 입력과 안내
        /// <summary>
        /// 모든 연구를 초기화하고 사용한 소울을 돌려받습니다. 실패하면 진행과 잔액을 바꾸지 않습니다.
        /// </summary>
        private void ResetResearch()
        {
            if (system.Reset(false, true, out string reason))
            {
                messageText.text = "모든 연구를 초기화하고 사용한 소울을 돌려받았습니다.";
                return;
            }

            SWLog.LogWarning("[HubResearchUI] 연구 초기화 실패: " + reason);
            messageText.text = "연구를 초기화하지 못했습니다. 진행과 소울은 그대로입니다.";
        }

        /// <summary>
        /// 연구 저장에 실패한 상태면 안내합니다.
        /// </summary>
        private void PresentSaveIssue()
        {
            string issue = readSaveIssue?.Invoke();
            if (!string.IsNullOrEmpty(issue))
            {
                messageText.text = issue;
            }
        }

        #endregion // 입력과 안내

        #region 정리
        /// <summary>
        /// 버튼과 트리 알림 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (!subscribed)
            {
                return;
            }

            closeButton.onClick.RemoveListener(Hide);
            resetButton.onClick.RemoveListener(ResetResearch);
            system.Changed -= PresentSaveIssue;
        }

        #endregion // 정리
    }
}
