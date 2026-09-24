using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    /// <summary>
    /// 보상 자산의 기본 수량을 필요할 때 덮어쓰고 독립 획득 확률을 지정합니다. 원본은 변경하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class RewardEntry
    {
        #region 필드
        [SerializeField] private RewardData definition;
        [SerializeField] private bool useAmountOverride;
        [SerializeField, Min(0)] private double overrideAmount = 1d;
        [SerializeField, Range(0, 100)] private float acquisitionProbability = 100f;
        [SerializeField] private bool randomizeEquipmentGrade = true;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 장비 보상은 개별 수량마다 등급을 추첨할지 반환합니다. 일반 아이템·재화에는 적용하지 않습니다.
        /// </summary>
        public bool RandomizeEquipmentGrade => randomizeEquipmentGrade && definition is ItemData item && item.Equipment != null;

        /// <summary>
        /// 지급 대상 재화 또는 아이템의 원본 자산입니다.
        /// </summary>
        public RewardData Definition => definition;

        /// <summary>
        /// 다른 항목과 독립적으로 적용하는 0부터 100까지의 백분율입니다.
        /// </summary>
        public float AcquisitionProbability => acquisitionProbability;

        /// <summary>
        /// 실제 지급 수량입니다. 참조가 없고 덮어쓰기도 꺼져 있으면 계산 불가 값입니다.
        /// </summary>
        public double Amount
        {
            get
            {
                if (useAmountOverride)
                {
                    return overrideAmount;
                }

                return definition != null ? definition.DefaultAmount : double.NaN;
            }
        }

        /// <summary>
        /// 참조·수량·확률이 모두 올바른지 반환합니다.
        /// </summary>
        public bool IsValid => Validate(null, string.Empty, null);

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 실제 적용되는 값과 참조를 검사합니다. 목록이 있으면 소유 데이터 기준 경로로 문제를 추가합니다.
        /// </summary>
        public bool Validate(ProjectData owner, string propertyPath, List<DataIssue> issues)
        {
            bool valid = true;
            if (definition == null)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(definition), "재화 또는 아이템을 연결하세요."));
                valid = false;
            }
            else if (!Amount.ExIsNonNegative())
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(overrideAmount), "보상 수량은 0 이상의 유한한 수여야 합니다."));
                valid = false;
            }

            if (definition is ItemData && (!Amount.ExIsItemCount() || !definition.IsValid))
            {
                issues?.Add(new DataIssue(owner, propertyPath, "아이템 보상은 유효한 정의와 0 이상의 정수 수량이어야 합니다."));
                valid = false;
            }

            if (!acquisitionProbability.ExIsFinite() || acquisitionProbability < 0f || acquisitionProbability > 100f)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(acquisitionProbability), "획득 확률을 0부터 100 사이로 입력하세요."));
                valid = false;
            }

            if (RandomizeEquipmentGrade && acquisitionProbability > 0f && Amount > ProjectDefine.Inventory.MaximumEquipmentRollCount)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(overrideAmount),
                    "장비 개별 추첨은 한 요청당 " + ProjectDefine.Inventory.MaximumEquipmentRollCount + "개까지 처리합니다. 지급을 나누어 요청하세요."));
                valid = false;
            }

            return valid;
        }

        #endregion // 검사
    }
}
