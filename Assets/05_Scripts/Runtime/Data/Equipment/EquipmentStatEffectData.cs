using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using SW.Stat;

namespace ProjectT.Data
{
    /// <summary>
    /// 선택한 SWStat별 고정 증가량을 정의합니다. 원본 스탯이나 실행 중 능력치는 변경하지 않습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/데이터/장비 스탯 효과", fileName = "EquipmentStatEffectData")]
    public sealed class EquipmentStatEffectData : EquipmentEffectData
    {
        #region 필드
        [SerializeField] private string displayName = "새 장비 효과";
        [SerializeField] private EquipmentStatBonus[] statBonuses = Array.Empty<EquipmentStatBonus>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 효과 설정의 표시 이름입니다. 빈 이름은 검사를 통과하지 못합니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 스탯별 증가량입니다. 빈 목록은 아무 스탯도 증가시키지 않습니다.
        /// </summary>
        public IReadOnlyList<EquipmentStatBonus> StatBonuses => statBonuses;

        /// <summary>
        /// 연결된 스탯과 증가량을 표시합니다. 미완성 항목은 안내로 표시하며 능력치를 적용하지 않습니다.
        /// </summary>
        public override string EffectDescription
        {
            get
            {
                if (statBonuses == null || statBonuses.Length == 0)
                {
                    return "등록된 스탯 효과가 없습니다.";
                }

                var description = new StringBuilder();
                foreach (EquipmentStatBonus bonus in statBonuses)
                {
                    if (description.Length > 0)
                    {
                        description.Append(" · ");
                    }
                    if (bonus == null || bonus.Stat == null)
                    {
                        description.Append("스탯 미연결");
                        continue;
                    }

                    description.Append(bonus.Stat.DisplayName).Append(" +");
                    description.Append((bonus.Stat.IsPercentType ? bonus.Amount * 100f : bonus.Amount).ToString("0.###"));
                    if (bonus.Stat.IsPercentType)
                    {
                        description.Append("%p");
                    }
                }

                return description.ToString();
            }
        }

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 이름·스탯 참조·중복·증가량을 검사합니다. 없는 참조와 유한하지 않은 값은 false입니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckName(displayName, nameof(displayName), issues);
            if (!Check(statBonuses != null, nameof(statBonuses), "스탯 증가량 목록이 없습니다.", issues))
            {
                return false;
            }

            var definitions = new HashSet<SWStat>();
            var identifiers = new HashSet<int>();
            for (int index = 0; index < statBonuses.Length; index++)
            {
                EquipmentStatBonus bonus = statBonuses[index];
                string path = nameof(statBonuses) + ".Array.data[" + index + "]";
                if (bonus == null)
                {
                    valid &= Check(false, path, "스탯 항목이 없습니다.", issues);
                    continue;
                }

                valid &= Check(bonus.Stat != null && definitions.Add(bonus.Stat)
                    && (bonus.Stat.ID == 0 || identifiers.Add(bonus.Stat.ID)), path + ".stat", "중복되지 않는 스탯 정의를 연결하세요.", issues);
                valid &= CheckNonNegative(bonus.Amount, path + ".amount", issues);
            }

            return valid;
        }

        #endregion // 검사
    }
}
