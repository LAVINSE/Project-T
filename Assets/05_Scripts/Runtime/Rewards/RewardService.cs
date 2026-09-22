using System;
using System.Collections.Generic;

using ProjectT.Data;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 처치 보상을 항목마다 독립 확률로 계산하고, 전투 배치 재화와 같은 보상만 합산해 한 번에 지급합니다.
    /// 아이템·다른 재화는 계산 결과로만 반환하며 보관하지 않습니다.
    /// </summary>
    public sealed class RewardService
    {
        #region 필드
        private readonly CurrencyData deploymentCurrency;
        private readonly Func<double, bool> creditCurrency;
        private readonly Func<double> nextRandom;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 지급 대상 재화, 재화 지급 함수, 0부터 1까지의 난수 공급자를 연결합니다. 지갑 구현에 의존하지 않습니다.
        /// </summary>
        public RewardService(CurrencyData deploymentCurrency, Func<double, bool> creditCurrency, Func<double> nextRandom)
        {
            this.deploymentCurrency = deploymentCurrency;
            this.creditCurrency = creditCurrency;
            this.nextRandom = nextRandom;
        }

        #endregion // 초기화

        #region 지급
        /// <summary>
        /// 보상을 계산해 배치 재화를 지급합니다. 입력이 잘못되었거나 지급이 실패하면 지갑을 바꾸지 않고 빈 결과를 반환합니다.
        /// </summary>
        public bool TryGrant(IReadOnlyList<RewardEntry> entries, out IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            if (!TryCalculate(entries, out var calculated, out reason))
            {
                return false;
            }

            double total = 0d;
            foreach (RewardAmount reward in calculated)
            {
                if (reward.Definition == deploymentCurrency)
                {
                    total += reward.Amount;
                }
            }

            if (!creditCurrency(total))
            {
                reason = "지급 후 잔액이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            rewards = calculated;
            return true;
        }

        /// <summary>
        /// 전체 입력을 검증한 뒤 항목마다 한 번 판정합니다. 0%와 100% 항목은 난수를 소비하지 않습니다.
        /// </summary>
        private bool TryCalculate(IReadOnlyList<RewardEntry> entries, out IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            reason = string.Empty;
            foreach (var entry in entries)
            {
                if (entry == null || !entry.IsValid)
                {
                    reason = "보상 목록의 참조·수량·확률을 확인해 주세요.";
                    return false;
                }
            }

            var calculated = new List<RewardAmount>();
            foreach (var entry in entries)
            {
                if (entry.AcquisitionProbability == 0f || entry.Amount == 0d)
                {
                    continue;
                }

                if (entry.AcquisitionProbability < 100f)
                {
                    double sample = nextRandom();
                    if (double.IsNaN(sample) || sample < 0d || sample > 1d)
                    {
                        reason = "확률 공급자의 값은 0부터 1 사이여야 합니다.";
                        return false;
                    }

                    if (sample >= entry.AcquisitionProbability / 100d)
                    {
                        continue;
                    }
                }

                calculated.Add(new RewardAmount(entry.Definition, entry.Amount));
            }

            rewards = calculated.AsReadOnly();
            return true;
        }

        #endregion // 지급
    }
}
