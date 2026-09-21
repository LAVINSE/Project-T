using System;
using System.Collections.Generic;

using ProjectT.Data;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 보상마다 독립 확률을 판정합니다. 재화 지급·아이템 보관·원본 변경은 수행하지 않습니다.
    /// </summary>
    public static class RewardCalculator
    {
        #region 계산
        /// <summary>
        /// 전체 입력을 검증한 뒤 항목마다 한 번 판정합니다. 실패하면 부분 결과 대신 빈 목록을 반환합니다.
        /// 난수 공급자는 0부터 1까지 값을 반환하며 0%와 100% 항목은 난수를 소비하지 않습니다.
        /// </summary>
        public static bool TryCalculate(
            IReadOnlyList<RewardEntry> entries,
            Func<double> nextRandom,
            out IReadOnlyList<RewardAmount> rewards,
            out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            reason = string.Empty;
            if (entries == null || nextRandom == null)
            {
                reason = "보상 목록과 확률 공급자가 필요합니다.";
                return false;
            }

            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    reason = "보상 목록에 빈 항목이 있습니다.";
                    return false;
                }

                if (!entry.TryValidate(out reason))
                {
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
        #endregion // 계산
    }
}