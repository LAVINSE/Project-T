using UnityEngine;

using TMPro;

using SW.SkillTree;

namespace ProjectT.UI
{
    /// <summary>
    /// 연구 트리 노드를 한글로 표시합니다. 모든 연구는 1회라 레벨 대신 상태만 보여 줍니다.
    /// </summary>
    public sealed class ResearchNodeView : SWSkillTreeNodeView
    {
        #region 필드
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text stateText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 기본 표시 뒤 상태 문구를 한글로 바꿉니다.
        /// </summary>
        public override void Render(SWSkillTreeNode node, int currentLevel, bool canPurchase, bool isSelected)
        {
            base.Render(node, currentLevel, canPurchase, isSelected);
            levelText.text = string.Empty;
            stateText.text = currentLevel >= node.Skill.MaximumLevel ? "연구 완료" : canPurchase ? "연구 가능" : "잠김";
        }

        /// <summary>
        /// 아직 공개되지 않은 노드를 한글로 표시합니다.
        /// </summary>
        public override void RenderMasked()
        {
            base.RenderMasked();
            levelText.text = string.Empty;
            stateText.text = "미발견";
        }

        #endregion // 표시
    }
}
