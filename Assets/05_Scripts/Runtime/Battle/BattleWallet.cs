using System;

using SW.Util;

namespace ProjectT.Battle
{
    /// <summary>
    /// 전투 하나의 배치 재화를 보관합니다. 새 전투에서는 새 지갑을 지정한 시작 금액으로 생성합니다.
    /// </summary>
    public sealed class BattleWallet
    {
        #region 프로퍼티
        /// <summary>
        /// 현재 전투에서 사용할 수 있는 배치 재화입니다.
        /// </summary>
        public double Balance { get; private set; }

        /// <summary>
        /// 잔액이 바뀌었을 때 표시와 구매 조건을 갱신하는 알림입니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 시작 금액으로 지갑을 만듭니다.
        /// </summary>
        private BattleWallet(double startingAmount)
        {
            Balance = startingAmount;
        }

        /// <summary>
        /// 시작 금액을 검증하여 지갑을 생성하며 실패하면 null을 반환합니다.
        /// </summary>
        public static BattleWallet Create(double startingAmount)
        {
            if (!startingAmount.ExIsNonNegative())
            {
                SWLog.LogWarning("[BattleWallet] 생성 실패: 시작 금액은 0 이상의 유한한 수여야 합니다.");
                return null;
            }

            return new BattleWallet(startingAmount);
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 처치 보상 등 확정된 재화를 지급합니다. 비정상 금액이나 계산 범위 초과는 잔액을 바꾸지 않습니다.
        /// </summary>
        public bool TryCredit(double amount)
        {
            if (!amount.ExIsNonNegative() || !(Balance + amount).ExIsFinite())
            {
                return false;
            }

            return Apply(Balance + amount);
        }

        /// <summary>
        /// 배치 비용을 차감합니다. 잔액이 부족하면 바꾸지 않습니다.
        /// </summary>
        public bool TrySpend(double amount)
        {
            if (!amount.ExIsNonNegative() || Balance < amount)
            {
                return false;
            }

            return Apply(Balance - amount);
        }

        /// <summary>
        /// 새 잔액을 반영하고 실제 변경이 있을 때만 알립니다.
        /// </summary>
        private bool Apply(double nextBalance)
        {
            if (nextBalance == Balance)
            {
                return true;
            }

            Balance = nextBalance;
            Changed?.Invoke();
            return true;
        }

        #endregion // 함수
    }
}
