using System;

using NUnit.Framework;

using SW.SkillTree;

using ProjectT.Economy;

namespace ProjectT.Tests
{
    /// <summary>
    /// 배치 재화의 손실 없는 실패와 새 전투의 독립된 시작 금액을 검증합니다.
    /// </summary>
    public sealed class BattleDeploymentWalletTests
    {
        #region 함수
        /// <summary>
        /// 여러 비용의 합이 잔액을 넘으면 일부 비용도 차감하지 않습니다.
        /// </summary>
        [Test]
        public void CombinedCostsFailWithoutPartialSpendingOrNotification()
        {
            var wallet = BattleDeploymentWallet.Create(100d);
            int notifications = 0;
            wallet.Changed += () => notifications++;
            var costs = new[]
            {
                new SWSkillTreeAmount(BattleDeploymentWallet.CurrencyIdentifier, 60d),
                new SWSkillTreeAmount(BattleDeploymentWallet.CurrencyIdentifier, 50d)
            };
            Assert.That(wallet.TryExchange(costs, false), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100d));
            Assert.That(notifications, Is.Zero);
            Assert.That(wallet.TrySpend(100d), Is.True);
            Assert.That(wallet.Balance, Is.Zero);
            Assert.That(notifications, Is.EqualTo(1));
        }

        /// <summary>
        /// 이전 전투에서 얻은 재화가 다음 전투의 시작 금액에 누적되지 않습니다.
        /// </summary>
        [Test]
        public void NewBattleStartsWithConfiguredAmount()
        {
            var previous = BattleDeploymentWallet.Create(100d);
            previous.TryCredit(40d);
            previous.TrySpend(30d);
            var current = BattleDeploymentWallet.Create(100d);
            Assert.That(previous.Balance, Is.EqualTo(110d));
            Assert.That(current.Balance, Is.EqualTo(100d));
        }

        /// <summary>
        /// 잘못된 재화나 비정상 수량을 섞어도 유효한 비용까지 지급하지 않습니다.
        /// </summary>
        [Test]
        public void InvalidCurrencyAndAmountsPreserveBalance()
        {
            var wallet = BattleDeploymentWallet.Create(20d);
            Assert.That(
                wallet.TryExchange(
                new[] { new SWSkillTreeAmount(BattleDeploymentWallet.CurrencyIdentifier, 10d), new SWSkillTreeAmount("UnknownCurrency", 1d) },
                true),
                Is.False);
            Assert.That(wallet.TryCredit(double.NaN), Is.False);
            Assert.That(wallet.TryCredit(-1d), Is.False);
            Assert.That(wallet.TrySpend(double.PositiveInfinity), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(20d));
            Assert.That(BattleDeploymentWallet.Create(-1d), Is.Null);
        }

        /// <summary>
        /// 표현 범위를 넘는 금액이나 정밀도 손실로 잔액이 바뀌지 않는 거래는 성공으로 처리하지 않습니다.
        /// </summary>
        [Test]
        public void OverflowAndUnrepresentableTransactionsAreRejected()
        {
            var wallet = BattleDeploymentWallet.Create(double.MaxValue);
            Assert.That(wallet.TryCredit(double.MaxValue), Is.False);
            Assert.That(wallet.TryCredit(1d), Is.False);
            Assert.That(wallet.TrySpend(1d), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(double.MaxValue));
        }

        #endregion // 함수
    }
}
