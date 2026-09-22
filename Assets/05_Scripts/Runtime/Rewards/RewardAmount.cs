using ProjectT.Data;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 확률 판정 후 얻은 보상 종류와 수량입니다. 소유·저장 완료를 의미하지 않습니다.
    /// </summary>
    public readonly struct RewardAmount
    {
        #region 프로퍼티
        /// <summary>
        /// 판정을 통과한 보상 자산입니다.
        /// </summary>
        public RewardData Definition { get; }

        /// <summary>
        /// 기본값 또는 항목별 덮어쓰기를 반영한 수량입니다.
        /// </summary>
        public double Amount { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 계산기가 검증한 보상 결과를 보관합니다.
        /// </summary>
        internal RewardAmount(RewardData definition, double amount)
        {
            Definition = definition;
            Amount = amount;
        }

        #endregion // 초기화
    }
}
