using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;

namespace ProjectT.Data
{
    /// <summary>
    /// 장비 이름·희귀도·등급 효과와 추첨 가중치를 정의합니다. 보유 수량은 인벤토리가 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/데이터/장비", fileName = "EquipmentData")]
    public sealed class EquipmentData : ProjectData
    {
        #region 필드
        [SerializeField] private string displayName = "새 장비";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private double sellPrice;
        [SerializeField] private CurrencyData sellCurrency;
        [SerializeField] private SWCategory rarity;
        [SerializeField] private EquipmentPerformanceGrade[] performanceGrades = Array.Empty<EquipmentPerformanceGrade>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 장비 종류의 표시 이름입니다. 빈 이름은 검사를 통과하지 못합니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 연결한 장비 아이템이 공통으로 사용하는 그림입니다. 미등록이면 빈 아이콘입니다.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 이 장비에 연결된 아이템이 사용하는 한 개의 판매가입니다. 기본값은 0입니다.
        /// </summary>
        public double SellPrice => sellPrice;

        /// <summary>
        /// 이 장비의 판매 대금으로 받는 재화입니다. 미지정이면 판매가를 표시하지 않습니다.
        /// </summary>
        public CurrencyData SellCurrency => sellCurrency;

        /// <summary>
        /// 일반·희귀처럼 사용자가 정의하는 희귀도입니다. 성능 등급이 있는 장비는 반드시 연결합니다.
        /// </summary>
        public SWCategory Rarity => rarity;

        /// <summary>
        /// 사용자 지정 순서의 성능 등급 목록입니다. 빈 목록은 제작 중인 설정이며 아이템에 연결할 수 없습니다.
        /// </summary>
        public IReadOnlyList<EquipmentPerformanceGrade> PerformanceGrades => performanceGrades;

        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 유효한 가중치의 합입니다. 음수·비유한 값이나 목록 누락이면 0입니다.
        /// </summary>
        public double GetTotalSelectionWeight()
        {
            double total = 0d;
            if (performanceGrades == null)
            {
                return 0d;
            }

            foreach (EquipmentPerformanceGrade grade in performanceGrades)
            {
                if (grade == null || !grade.SelectionWeight.ExIsNonNegative())
                {
                    return 0d;
                }

                total += grade.SelectionWeight;
            }

            return total;
        }

        /// <summary>
        /// 0부터 1까지의 난수로 가중치에 따라 한 등급을 고릅니다. 잘못된 설정·난수에는 false입니다.
        /// </summary>
        public bool TrySelectPerformanceGrade(double sample, out EquipmentPerformanceGrade result)
        {
            result = null;
            double total = GetTotalSelectionWeight();
            if (!sample.ExIsFinite() || sample < 0d || sample > 1d || total <= 0d)
            {
                return false;
            }

            double target = sample * total;
            double cumulative = 0d;
            foreach (EquipmentPerformanceGrade grade in performanceGrades)
            {
                if (grade.SelectionWeight <= 0f)
                {
                    continue;
                }

                result = grade;
                cumulative += grade.SelectionWeight;
                if (target < cumulative)
                {
                    return true;
                }
            }

            return result != null;
        }

        /// <summary>
        /// 코드명이 일치하는 성능 등급을 찾습니다. 미연결·빈 코드명·없는 등급이면 false와 null입니다.
        /// </summary>
        public bool TryGetPerformanceGrade(SWCategory grade, out EquipmentPerformanceGrade result)
        {
            result = null;
            if (grade == null || string.IsNullOrWhiteSpace(grade.CodeName) || performanceGrades == null)
            {
                return false;
            }

            foreach (EquipmentPerformanceGrade entry in performanceGrades)
            {
                if (entry != null && entry.PerformanceGrade != null
                    && string.Equals(entry.PerformanceGrade.CodeName, grade.CodeName, StringComparison.Ordinal))
                {
                    result = entry;
                    return true;
                }
            }

            return false;
        }

        #endregion // 조회

        #region 검사
        /// <summary>
        /// 희귀도·등급 코드명·효과와 중복 등급을 검사합니다. 빈 제작 양식은 허용하고 불완전한 등급 행은 거부합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckName(displayName, nameof(displayName), issues);
            valid &= CheckNonNegative(sellPrice, nameof(sellPrice), issues);
            valid &= Check(sellCurrency == null || sellCurrency.IsValid,
                nameof(sellCurrency), "판매 재화의 검사 오류를 확인하세요.", issues);
            valid &= Check(performanceGrades != null, nameof(performanceGrades), "성능 등급 목록이 없습니다.", issues);
            valid &= Check(rarity == null || !string.IsNullOrWhiteSpace(rarity.CodeName), nameof(rarity), "희귀도 분류에 코드명이 필요합니다.", issues);
            if (performanceGrades == null)
            {
                return false;
            }

            valid &= Check(performanceGrades.Length == 0 || rarity != null, nameof(rarity), "성능 등급을 설정한 장비는 희귀도를 연결하세요.", issues);
            var codes = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < performanceGrades.Length; index++)
            {
                EquipmentPerformanceGrade entry = performanceGrades[index];
                string path = nameof(performanceGrades) + ".Array.data[" + index + "]";
                if (entry == null)
                {
                    valid &= Check(false, path, "성능 등급 항목이 없습니다.", issues);
                    continue;
                }

                valid &= Check(entry.PerformanceGrade != null
                    && !string.IsNullOrWhiteSpace(entry.PerformanceGrade.CodeName)
                    && codes.Add(entry.PerformanceGrade.CodeName)
                    && names.Add(entry.PerformanceGrade.DisplayName), path + ".performanceGrade", "성능 등급 분류의 참조·코드명·이름 중복을 확인하세요.", issues);
                valid &= Check(entry.Effect != null && entry.Effect.IsValid, path + ".effect", "유효한 장비 효과를 연결하세요.", issues);
                valid &= CheckNonNegative(entry.SelectionWeight, path + ".selectionWeight", issues);
                valid &= Check(entry.ValidateStatOverrides(), path + ".statOverrides",
                    "공유 효과에 있는 스탯만 중복 없이 재정의하세요. 효과를 교체했다면 남은 설정을 정리하세요.", issues);
                var effects = new HashSet<EquipmentEffectData>();
                effects.Add(entry.Effect);
                if (entry.AdditionalEffects != null)
                {
                    for (int effectIndex = 0; effectIndex < entry.AdditionalEffects.Count; effectIndex++)
                    {
                        EquipmentEffectData additional = entry.AdditionalEffects[effectIndex];
                        valid &= Check(additional != null && additional.IsValid && effects.Add(additional),
                            path + ".additionalEffects.Array.data[" + effectIndex + "]", "중복되지 않는 유효한 추가 효과를 연결하세요.", issues);
                    }
                }
            }

            valid &= Check(performanceGrades.Length == 0 || GetTotalSelectionWeight() > 0d,
                nameof(performanceGrades), "한 등급 이상의 추첨 가중치를 0보다 크게 설정하세요.", issues);
            return valid;
        }

        #endregion // 검사
    }
}
