using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Stat;
using SW.Util;

namespace ProjectT.Data
{
    /// <summary>
    /// 장비의 성능 등급과 해당 등급의 고정 효과 설정을 연결합니다. 목록 순서가 표시 순서입니다.
    /// </summary>
    [Serializable]
    public sealed class EquipmentPerformanceGrade
    {
        #region 필드
        [SerializeField] private SWCategory performanceGrade;
        [SerializeField] private EquipmentEffectData effect;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField] private EquipmentGradeStatOverride[] statOverrides = Array.Empty<EquipmentGradeStatOverride>();
        [SerializeField] private EquipmentEffectData[] additionalEffects = Array.Empty<EquipmentEffectData>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 체크와 증가량 재정의를 반영한 설명입니다. 잘못된 설정은 안내 문구로 표시합니다.
        /// </summary>
        public string EffectDescription
        {
            get
            {
                if (!ValidateStatOverrides())
                {
                    return "등급 효과 설정을 확인하세요.";
                }

                TryResolveStatBonuses(out IReadOnlyList<EquipmentStatBonus> bonuses);
                var descriptions = new List<string>();
                if (!(effect is EquipmentStatEffectData))
                {
                    descriptions.Add(effect.EffectDescription);
                }
                foreach (EquipmentStatBonus bonus in bonuses)
                {
                    float value = bonus.Stat.IsPercentType ? bonus.Amount * 100f : bonus.Amount;
                    descriptions.Add(bonus.Stat.DisplayName + " +" + value.ToString("0.###")
                        + (bonus.Stat.IsPercentType ? "%p" : string.Empty));
                }

                if (additionalEffects != null)
                {
                    foreach (EquipmentEffectData additional in additionalEffects)
                    {
                        if (additional != null)
                        {
                            descriptions.Add(additional.EffectDescription);
                        }
                    }
                }

                return descriptions.Count > 0 ? string.Join(" · ", descriptions) : "포함된 스탯 없음";
            }
        }

        /// <summary>
        /// 등급 추첨 가중치입니다. 0이면 추첨에서 제외하며 음수는 유효하지 않습니다.
        /// </summary>
        public float SelectionWeight => selectionWeight;

        /// <summary>
        /// 공유 스탯의 등급별 포함·수치 설정입니다. 설정이 없는 스탯은 공유값으로 포함합니다.
        /// </summary>
        public IReadOnlyList<EquipmentGradeStatOverride> StatOverrides => statOverrides;

        /// <summary>
        /// 사용자가 추가하는 성능 등급 분류입니다. 미연결이면 장비 검사를 통과하지 못합니다.
        /// </summary>
        public SWCategory PerformanceGrade => performanceGrade;

        /// <summary>
        /// 이 성능 등급의 효과 정의입니다. 미연결이면 장비 검사를 통과하지 못합니다.
        /// </summary>
        public EquipmentEffectData Effect => effect;

        /// <summary>
        /// 기본 효과와 함께 사용할 추가 능력 모듈입니다. 빈 목록은 추가 효과 없음이며 구체적인 능력 동작은 별도 구현합니다.
        /// </summary>
        public IReadOnlyList<EquipmentEffectData> AdditionalEffects => additionalEffects;

        #endregion // 프로퍼티

        #region 스탯 결정
        /// <summary>
        /// 체크된 모든 스탯의 최종 증가량을 반환합니다. 별도 추첨은 없으며 잘못된 설정은 false입니다.
        /// </summary>
        public bool TryResolveStatBonuses(out IReadOnlyList<EquipmentStatBonus> bonuses)
        {
            bonuses = Array.Empty<EquipmentStatBonus>();
            if (!ValidateStatOverrides())
            {
                SWLog.LogWarning("[EquipmentPerformanceGrade] 효과 계산 실패: 공유 스탯과 등급별 설정을 확인하세요.");
                return false;
            }

            if (!effect.TryGetStatBonuses(out IReadOnlyList<EquipmentStatBonus> templateBonuses))
            {
                return false;
            }

            var result = new List<EquipmentStatBonus>();
            foreach (EquipmentStatBonus bonus in templateBonuses)
            {
                EquipmentGradeStatOverride setting = FindStatOverride(bonus.Stat);
                if (setting != null && !setting.Enabled)
                {
                    continue;
                }

                float amount = setting != null && setting.UseAmountOverride ? setting.Amount : bonus.Amount;
                result.Add(EquipmentStatBonus.Create(bonus.Stat, amount));
            }

            bonuses = result.AsReadOnly();
            return true;
        }

        /// <summary>
        /// 공유 효과에 포함된 스탯만 중복 없이 재정의하는지 검사합니다. 원본은 변경하지 않습니다.
        /// </summary>
        public bool ValidateStatOverrides()
        {
            if (effect == null || !effect.IsValid || statOverrides == null)
            {
                return false;
            }

            var definitions = new HashSet<SWStat>();
            if (effect is EquipmentStatEffectData template)
            {
                foreach (EquipmentStatBonus bonus in template.StatBonuses)
                {
                    definitions.Add(bonus.Stat);
                }
            }

            var seen = new HashSet<SWStat>();
            foreach (EquipmentGradeStatOverride setting in statOverrides)
            {
                if (setting?.Stat == null || !definitions.Contains(setting.Stat) || !seen.Add(setting.Stat)
                    || (setting.Enabled && setting.UseAmountOverride && !setting.Amount.ExIsNonNegative()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 지정 스탯의 등급별 설정입니다. 없으면 공유값 사용을 뜻하는 null입니다.
        /// </summary>
        public EquipmentGradeStatOverride FindStatOverride(SWStat stat)
        {
            if (statOverrides != null)
            {
                foreach (EquipmentGradeStatOverride setting in statOverrides)
                {
                    if (setting != null && setting.Stat == stat)
                    {
                        return setting;
                    }
                }
            }

            return null;
        }

        #endregion // 스탯 결정
    }
}
