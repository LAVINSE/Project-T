using UnityEngine;

using SW.SkillTree;
using SW.Stat;

using ProjectT.Research;

namespace ProjectT.Data
{
    /// <summary>
    /// 연구 노드가 모든 아군에게 주는 능력치 효과입니다. 노드마다 고정값 또는 클래스 기본값 기준 비율을 고릅니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ResearchStatEffectData", menuName = "Project T/데이터/연구 능력치 효과")]
    public sealed class ResearchStatEffectData : SWSkillTreeEffect
    {
        #region 필드
        [SerializeField] private SWStat stat;
        [SerializeField] private ResearchValueMode mode;
        [SerializeField] private float amount;

        #endregion // 필드

        #region 효과
        /// <summary>
        /// 습득 레벨만큼의 증가량을 연구 모음에 기록합니다. 레벨 0이면 이 출처를 제거합니다. 연구 모음이 아닌 문맥은 무시합니다.
        /// </summary>
        public override void Apply(object context, object source, int level)
        {
            if (context is ResearchBonuses bonuses)
            {
                bonuses.Set(source, stat, mode, amount * level);
            }
        }

        /// <summary>
        /// 습득 레벨의 효과를 한글로 설명합니다. 비율은 백분율, 백분율 표시 스탯의 고정값은 %p로 표시합니다.
        /// </summary>
        public override string Describe(int level)
        {
            string name = stat != null ? stat.DisplayName : "스탯 미연결";
            float value = amount * Mathf.Max(1, level);
            if (mode == ResearchValueMode.Percent)
            {
                return "모든 아군 " + name + " +" + (value * 100f).ToString("0.##") + "% (클래스 기본값 기준)";
            }

            return stat != null && stat.IsPercentType
                ? "모든 아군 " + name + " +" + (value * 100f).ToString("0.##") + "%p"
                : "모든 아군 " + name + " +" + value.ToString("0.##");
        }

        #endregion // 효과
    }
}
