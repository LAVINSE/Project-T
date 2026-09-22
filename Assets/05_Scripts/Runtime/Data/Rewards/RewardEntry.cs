using System;
using UnityEngine;

using SW.Util;

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

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 지급 대상 재화 또는 아이템의 원본 자산입니다.
        /// </summary>
        public RewardData Definition => definition;

        /// <summary>
        /// 기본 수량 대신 항목의 개별 수량을 사용할지 여부입니다.
        /// </summary>
        public bool UseAmountOverride => useAmountOverride;

        /// <summary>
        /// 덮어쓰기를 켰을 때 사용할 수량입니다.
        /// </summary>
        public double OverrideAmount => overrideAmount;

        /// <summary>
        /// 다른 항목과 독립적으로 적용하는 0부터 100까지의 백분율입니다.
        /// </summary>
        public float AcquisitionProbability => acquisitionProbability;

        /// <summary>
        /// 실제 지급 수량입니다. 참조가 없고 덮어쓰기도 꺼져 있으면 계산 불가 값입니다.
        /// </summary>
        public double Amount => useAmountOverride ? overrideAmount : definition == null ? double.NaN : definition.DefaultAmount;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증할 개별 보상 설정을 보관합니다. 생성은 Create를 통해 요청합니다.
        /// </summary>
        private RewardEntry(RewardData definition, float probability, bool useOverride, double amount)
        {
            this.definition = definition;
            acquisitionProbability = probability;
            useAmountOverride = useOverride;
            overrideAmount = amount;
        }

        /// <summary>
        /// 보상 설정을 검증한 뒤 생성합니다. 잘못된 참조·수량·확률이면 이유를 기록하고 null을 반환합니다.
        /// </summary>
        public static RewardEntry Create(
            RewardData definition,
            float probability = 100f,
            bool useOverride = false,
            double amount = 1d)
        {
            var entry = new RewardEntry(definition, probability, useOverride, amount);
            if (!entry.TryValidate(out string reason))
            {
                SWLog.LogWarning("[RewardEntry] 생성 실패: " + reason);
                return null;
            }

            return entry;
        }

        #endregion // 초기화

        #region 검사
        /// <summary>
        /// 실제 적용되는 값과 자산 기본값을 검사합니다. 실패하면 이유를 반환하며 원본을 바꾸지 않습니다.
        /// </summary>
        public bool TryValidate(out string reason)
        {
            reason = string.Empty;
            if (definition == null)
            {
                reason = "재화 또는 아이템을 연결하세요.";
                return false;
            }

            if (!IsValidAmount(definition.DefaultAmount) || !IsValidAmount(Amount))
            {
                reason = "보상 수량은 0 이상의 유한한 수여야 합니다.";
                return false;
            }

            if (float.IsNaN(acquisitionProbability)
                || float.IsInfinity(acquisitionProbability)
                || acquisitionProbability < 0f
                || acquisitionProbability > 100f)
            {
                reason = "획득 확률은 0부터 100 사이여야 합니다.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 저장하거나 합산할 수 있는 음수가 아닌 수량인지 확인합니다.
        /// </summary>
        private static bool IsValidAmount(double amount)
        {
            return !double.IsNaN(amount) && !double.IsInfinity(amount) && amount >= 0d;
        }

        #endregion // 검사
    }
}
