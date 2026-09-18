using System;
using System.Collections.Generic;

using SW.SkillTree;
using SW.Util;

namespace ProjectT.Economy
{
    /// <summary>
    /// 전투 하나의 배치 재화를 보관합니다. 새 전투에서는 새 지갑을 지정한 시작 금액으로 생성합니다.
    /// </summary>
    public sealed class BattleDeploymentWallet : ISWSkillTreeWallet
    {
        #region 필드
        /// <summary>
        /// SWUtils 비용 정의에서 이 전투 재화를 참조하는 고정 식별자입니다.
        /// </summary>
        public const string CurrencyIdentifier = "BattleDeploymentCurrency";

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 전투에서 사용할 수 있는 배치 재화입니다.
        /// </summary>
        public double Balance { get; private set; }

        /// <summary>
        /// 잔액 변경 후 표시와 구매 조건을 갱신하는 알림입니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 0 이상의 유한한 시작 금액으로 독립된 전투 지갑을 만듭니다.
        /// </summary>
        private BattleDeploymentWallet(double startingAmount)
        {
            Balance = startingAmount;
        }

        /// <summary>
        /// 시작 금액을 검증하여 지갑을 생성하며 실패하면 null을 반환합니다.
        /// </summary>
        public static BattleDeploymentWallet Create(double startingAmount)
        {
            if (!IsFinite(startingAmount) || startingAmount < 0d)
            {
                SWLog.LogWarning("[BattleDeploymentWallet] 생성 실패: 시작 금액은 0 이상의 유한한 수여야 합니다.");
                return null;
            }

            return new BattleDeploymentWallet(startingAmount);
        }

        /// <summary>
        /// 등록한 전투 재화의 잔액을 반환합니다. 다른 재화 식별자는 지원하지 않습니다.
        /// </summary>
        public double GetBalance(string currency)
        {
            return currency == CurrencyIdentifier ? Balance : double.NaN;
        }

        /// <summary>
        /// 처치 보상 등 확정된 전투 재화를 지급합니다. 비정상 금액은 잔액을 변경하지 않습니다.
        /// </summary>
        public bool TryCredit(double amount)
        {
            return TryExchange(new[] { new SWSkillTreeAmount(CurrencyIdentifier, amount) }, true);
        }

        /// <summary>
        /// 배치 비용을 차감합니다. 잔액이 부족하면 변경하지 않습니다.
        /// </summary>
        public bool TrySpend(double amount)
        {
            return TryExchange(new[] { new SWSkillTreeAmount(CurrencyIdentifier, amount) }, false);
        }

        /// <summary>
        /// 비용 전체를 검증하고 한 번에 반영합니다. 실패하면 잔액과 알림을 모두 보존합니다.
        /// </summary>
        public bool TryExchange(IReadOnlyList<SWSkillTreeAmount> amounts, bool credit)
        {
            if (amounts == null)
            {
                return false;
            }

            double total = 0d;
            foreach (SWSkillTreeAmount amount in amounts)
            {
                if (!amount.IsValid || amount.currency != CurrencyIdentifier)
                {
                    return false;
                }

                double nextTotal = total + amount.value;
                if (!IsFinite(nextTotal) || (amount.value > 0d && nextTotal <= total))
                {
                    return false;
                }

                total = nextTotal;
            }

            if (total == 0d)
            {
                return true;
            }

            if (!credit && !SWSkillTreeAmount.CanSubtract(Balance, total))
            {
                return false;
            }

            double nextBalance = credit ? Balance + total : Balance - total;
            if (!IsFinite(nextBalance) || (credit && nextBalance <= Balance))
            {
                return false;
            }

            Balance = nextBalance;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 화면 구독자 예외로 성공한 거래가 실패하거나 다른 구독자의 갱신이 누락되지 않게 합니다.
        /// </summary>
        private void NotifyChanged()
        {
            if (Changed == null)
            {
                return;
            }

            foreach (Action listener in Changed.GetInvocationList())
            {
                try
                {
                    listener();
                }
                catch (Exception exception)
                {
                    SWLog.LogError(exception);
                }
            }
        }

        /// <summary>
        /// 재화 값이 계산 가능한 유한한 수인지 확인합니다.
        /// </summary>
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        #endregion // 함수
    }
}
