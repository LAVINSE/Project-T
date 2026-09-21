using System;
using System.Collections.Generic;

using ProjectT.Data;
using ProjectT.Economy;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 독립 확률 계산 결과에서 현재 전투의 배치 재화만 지갑에 연결합니다. 다른 보상은 계산 결과로 반환합니다.
    /// </summary>
    public static class BattleRewardService
    {
        #region 지급
        /// <summary>
        /// 전투 재화 합계를 검증하고 한 번에 지급합니다. 실패하면 지갑을 보존하고 빈 결과를 반환합니다.
        /// 아이템과 다른 재화는 영구 보관하지 않으며 반환값은 획득 확정 장부가 아닙니다.
        /// </summary>
        public static bool TryProcess(
            IReadOnlyList<RewardEntry> entries,
            CurrencyDefinition deploymentCurrency,
            BattleDeploymentWallet wallet,
            Func<double> nextRandom,
            out IReadOnlyList<RewardAmount> rewards,
            out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            reason = string.Empty;
            if (deploymentCurrency == null || wallet == null)
            {
                reason = "전투 배치 재화와 지갑을 연결하세요.";
                return false;
            }

            if (!RewardCalculator.TryCalculate(entries, nextRandom, out var calculated, out reason))
            {
                return false;
            }

            double total = 0d;
            foreach (var reward in calculated)
            {
                if (reward.Definition != deploymentCurrency)
                {
                    continue;
                }

                double next = total + reward.Amount;
                if (double.IsInfinity(next) || (reward.Amount > 0d && next <= total))
                {
                    reason = "합산한 전투 보상 수량이 계산 가능한 범위를 벗어났습니다.";
                    return false;
                }

                total = next;
            }

            if (!wallet.TryCredit(total))
            {
                reason = "전투 보상 지급 후 잔액이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            rewards = calculated;
            return true;
        }
        #endregion // 지급
    }
}