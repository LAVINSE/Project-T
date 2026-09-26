using System.Linq;
using System.Text;
using UnityEngine;

using TMPro;

using SW.SkillTree;

namespace ProjectT.UI
{
    /// <summary>
    /// 선택한 연구의 효과·선행 연구·소울 비용·상태를 한글로 표시합니다. 구매와 환불 버튼 동작은 SWSkillTreeView가 연결합니다.
    /// </summary>
    public sealed class ResearchDetailsView : SWSkillTreeDetailsView
    {
        #region 필드
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text effectsText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private TMP_Text costsText;
        [SerializeField] private TMP_Text messageText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 기본 표시로 버튼 상태를 정한 뒤 모든 문구를 한글로 다시 씁니다. 라이브러리의 영문 처리 결과 문구는 사용하지 않고 현재 상태로 안내합니다.
        /// </summary>
        public override void Render(SWSkillTreeSystem system, SWSkillTreeNode node, string feedback)
        {
            base.Render(system, node, feedback);
            if (system == null || node == null)
            {
                titleText.text = "연구를 선택하세요";
                descriptionText.text = "트리에서 연구를 누르면 효과와 비용을 볼 수 있습니다.";
                effectsText.text = requirementsText.text = costsText.text = messageText.text = string.Empty;
                return;
            }

            bool learned = system.GetLevel(node.Identifier) >= node.Skill.MaximumLevel;
            titleText.text = node.Skill.DisplayName;
            descriptionText.text = node.Skill.Description;
            effectsText.text = "효과\n" + string.Join("\n", node.Skill.Effects.Select(effect => effect.Describe(1)));
            requirementsText.text = DescribeRequirements(system, node);
            costsText.text = learned ? string.Empty : DescribeCost(system, node);
            messageText.text = DescribeState(system, node, learned);
        }

        /// <summary>
        /// 선행 연구 목록과 완료 여부입니다.
        /// </summary>
        private static string DescribeRequirements(SWSkillTreeSystem system, SWSkillTreeNode node)
        {
            if (node.Requirements.Count == 0)
            {
                return "선행 연구 없음";
            }

            var text = new StringBuilder(node.RequirementMode == SWSkillTreeRequirementMode.All
                ? "선행 연구 (모두 필요)" : "선행 연구 (하나 이상)");
            foreach (SWSkillTreeRequirement requirement in node.Requirements)
            {
                if (system.TryGetNode(requirement.NodeIdentifier, out SWSkillTreeNode parent))
                {
                    text.Append('\n').Append(parent.Skill.DisplayName)
                        .Append(system.GetLevel(parent.Identifier) >= requirement.RequiredLevel ? " · 완료" : " · 미완료");
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// 소울 비용과 현재 잔액입니다.
        /// </summary>
        private static string DescribeCost(SWSkillTreeSystem system, SWSkillTreeNode node)
        {
            double cost = node.Skill.Costs.Sum(entry => entry.Evaluate(0).value);
            double balance = system.Wallet.GetBalance(ProjectDefine.Research.SoulCurrency);
            return "비용: 소울 " + cost.ToString("N0") + "\n보유: 소울 " + balance.ToString("N0");
        }

        /// <summary>
        /// 연구 가능 여부와 이유를 현재 상태로 판단해 안내합니다.
        /// </summary>
        private static string DescribeState(SWSkillTreeSystem system, SWSkillTreeNode node, bool learned)
        {
            if (learned)
            {
                return HasDependentLearned(system, node)
                    ? "연구 완료. 이어진 연구를 먼저 환불해야 환불할 수 있습니다."
                    : "연구 완료. 환불하면 사용한 소울을 돌려받습니다.";
            }

            if (system.PreviewPurchase(node.Identifier).Success)
            {
                return "연구할 수 있습니다.";
            }

            return RequirementsMet(system, node, null) ? "소울이 부족합니다." : "선행 연구를 먼저 완료하세요.";
        }

        /// <summary>
        /// 이 노드를 환불하면 선행 조건이 깨지는 습득 연구가 있는지 확인합니다.
        /// </summary>
        private static bool HasDependentLearned(SWSkillTreeSystem system, SWSkillTreeNode node)
        {
            foreach (SWSkillTreeNode other in system.Definition.Nodes)
            {
                if (system.GetLevel(other.Identifier) > 0
                    && other.Requirements.Any(requirement => requirement.NodeIdentifier == node.Identifier)
                    && !RequirementsMet(system, other, node.Identifier))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 선행 조건 충족 여부입니다. 제외할 노드를 주면 그 노드가 미습득인 것으로 가정합니다.
        /// </summary>
        private static bool RequirementsMet(SWSkillTreeSystem system, SWSkillTreeNode node, string excluded)
        {
            if (node.Requirements.Count == 0)
            {
                return true;
            }

            bool Satisfied(SWSkillTreeRequirement requirement)
            {
                return requirement.NodeIdentifier != excluded
                    && system.GetLevel(requirement.NodeIdentifier) >= requirement.RequiredLevel;
            }

            return node.RequirementMode == SWSkillTreeRequirementMode.All
                ? node.Requirements.All(Satisfied)
                : node.Requirements.Any(Satisfied);
        }

        #endregion // 표시
    }
}
