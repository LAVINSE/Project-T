using System;
using System.Collections.Generic;

using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Progression;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 처치마다 한 번 계산한 전투 코인·영구 소울·아이템을 지급합니다. 저장 재시도 시 이미 지급한 영구 보상은 중복 지급하지 않습니다.
    /// </summary>
    public sealed class RewardService
    {
        #region 필드
        private readonly CurrencyData deploymentCurrency;
        private readonly CurrencyData soulCurrency;
        private readonly BattleWallet battleWallet;
        private readonly SoulWallet soulWallet;
        private readonly InventoryStore inventory;
        private readonly Func<double> nextRandom;
        private readonly Dictionary<string, IReadOnlyList<RewardAmount>> calculatedRewards = new Dictionary<string, IReadOnlyList<RewardAmount>>();
        private readonly HashSet<string> completedRewards = new HashSet<string>();
        private bool granting;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 검증한 보상 정의·지갑·난수 공급자를 연결합니다.
        /// </summary>
        private RewardService(
            CurrencyData deploymentCurrency,
            CurrencyData soulCurrency,
            BattleWallet battleWallet,
            SoulWallet soulWallet,
            InventoryStore inventory,
            Func<double> nextRandom)
        {
            this.deploymentCurrency = deploymentCurrency;
            this.soulCurrency = soulCurrency;
            this.battleWallet = battleWallet;
            this.soulWallet = soulWallet;
            this.inventory = inventory;
            this.nextRandom = nextRandom;
        }

        /// <summary>
        /// 재화와 지갑을 검사해 보상 서비스를 만듭니다. 미연결 또는 두 재화가 같은 경우 null입니다.
        /// </summary>
        public static RewardService Create(
            CurrencyData deploymentCurrency,
            CurrencyData soulCurrency,
            BattleWallet battleWallet,
            SoulWallet soulWallet,
            InventoryStore inventory,
            Func<double> nextRandom)
        {
            if (deploymentCurrency == null
                || soulCurrency == null
                || !deploymentCurrency.IsValid
                || !soulCurrency.IsValid
                || deploymentCurrency == soulCurrency
                || battleWallet == null
                || soulWallet == null
                || inventory == null
                || nextRandom == null)
            {
                SWLog.LogWarning("[RewardService] 생성 실패: 전투 재화·소울 정의, 지갑, 인벤토리, 난수 공급자를 확인하세요.");
                return null;
            }

            return new RewardService(deploymentCurrency, soulCurrency, battleWallet, soulWallet, inventory, nextRandom);
        }

        #endregion // 초기화

        #region 지급
        /// <summary>
        /// 처치 식별자마다 한 번 계산하고 지급합니다. 실패한 지급을 다시 요청해도 확률을 다시 판정하지 않습니다.
        /// </summary>
        public bool TryGrant(
            string identifier,
            IReadOnlyList<RewardEntry> entries,
            out IReadOnlyList<RewardAmount> rewards,
            out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            reason = string.Empty;
            if (granting || !Guid.TryParseExact(identifier, "N", out _))
            {
                reason = "처치 보상 식별자가 잘못되었거나 다른 보상을 지급 중입니다.";
                return false;
            }

            if (!calculatedRewards.TryGetValue(identifier, out IReadOnlyList<RewardAmount> calculated))
            {
                if (!TryCalculate(entries, out calculated, out reason))
                {
                    return false;
                }

                calculatedRewards.Add(identifier, calculated);
            }

            if (!TryApply(identifier, calculated, out reason))
            {
                return false;
            }

            rewards = calculated;
            return true;
        }

        /// <summary>
        /// 현재 전투에서 보류한 계산 결과만 다시 지급합니다. 성공한 보상은 건너뛰며 첫 실패에서 중단합니다.
        /// </summary>
        public bool TryGrantPending(out string reason)
        {
            reason = string.Empty;
            foreach (var pair in calculatedRewards)
            {
                if (!TryApply(pair.Key, pair.Value, out reason))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 잔액과 아이템 수량을 검사하고 영구 보상 저장 후 코인을 반영합니다. 부분 저장 실패는 지급 기록을 유지해 재시도합니다.
        /// </summary>
        private bool TryApply(string identifier, IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            reason = string.Empty;
            if (completedRewards.Contains(identifier))
            {
                return true;
            }

            if (granting)
            {
                reason = "보상 지급이 진행 중입니다.";
                return false;
            }

            double coins = 0d;
            double souls = 0d;
            foreach (RewardAmount reward in rewards)
            {
                if (reward.Definition == deploymentCurrency)
                {
                    coins += reward.Amount;
                }
                else if (reward.Definition == soulCurrency)
                {
                    souls += reward.Amount;
                }
            }

            if (!battleWallet.CanCredit(coins) || !souls.ExIsNonNegative())
            {
                reason = "보상 합계 또는 지급 후 잔액이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            granting = true;
            try
            {
                if (!inventory.CanGrant(identifier, rewards, out reason))
                {
                    return false;
                }

                if (!soulWallet.TryCredit(identifier, souls, out reason))
                {
                    return false;
                }

                if (!inventory.TryGrant(identifier, rewards, out reason))
                {
                    return false;
                }

                if (!battleWallet.TryCredit(coins))
                {
                    reason = "전투 재화 지급에 실패했습니다. 지급 기록을 유지하여 다시 처리합니다.";
                    return false;
                }

                completedRewards.Add(identifier);
                return true;
            }
            finally
            {
                granting = false;
            }
        }

        /// <summary>
        /// 전체 입력을 검증한 뒤 항목마다 한 번 판정합니다. 0%와 100% 항목은 난수를 소비하지 않습니다.
        /// </summary>
        private bool TryCalculate(IReadOnlyList<RewardEntry> entries, out IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            rewards = Array.Empty<RewardAmount>();
            reason = string.Empty;
            if (entries == null)
            {
                reason = "보상 목록이 없습니다.";
                return false;
            }

            var equipmentResolvers = new Dictionary<EquipmentData, EquipmentDropResolver>();
            double equipmentCount = 0d;
            foreach (var entry in entries)
            {
                if (entry == null || !entry.IsValid)
                {
                    reason = "보상 목록의 참조·수량·확률을 확인해 주세요.";
                    return false;
                }

                if (entry.RandomizeEquipmentGrade && entry.AcquisitionProbability > 0f && entry.Amount > 0d)
                {
                    equipmentCount += entry.Amount;
                    if (equipmentCount > ProjectDefine.Inventory.MaximumEquipmentRollCount)
                    {
                        reason = "한 요청의 장비 개별 추첨 수량이 안전 한도를 초과했습니다. 지급을 나누어 요청하세요.";
                        return false;
                    }

                    var item = (ItemData)entry.Definition;
                    if (!equipmentResolvers.ContainsKey(item.Equipment))
                    {
                        EquipmentDropResolver resolver = EquipmentDropResolver.Create(item.Equipment, inventory.ItemDefinitions, out reason);
                        if (resolver == null)
                        {
                            return false;
                        }

                        equipmentResolvers.Add(item.Equipment, resolver);
                    }
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

                if (entry.RandomizeEquipmentGrade)
                {
                    var item = (ItemData)entry.Definition;
                    if (!TryCalculateEquipment(equipmentResolvers[item.Equipment], (long)entry.Amount, calculated, out reason))
                    {
                        return false;
                    }
                }
                else
                {
                    calculated.Add(new RewardAmount(entry.Definition, entry.Amount));
                }
            }

            rewards = calculated.AsReadOnly();
            return true;
        }

        /// <summary>
        /// 장비 한 개마다 독립적으로 등급을 결정해 같은 아이템끼리 합산합니다. 잘못된 난수는 지급 전에 false입니다.
        /// </summary>
        private bool TryCalculateEquipment(EquipmentDropResolver resolver, long count, List<RewardAmount> rewards, out string reason)
        {
            reason = string.Empty;
            var counts = new Dictionary<ItemData, long>();
            for (long index = 0; index < count; index++)
            {
                if (!resolver.TryResolve(nextRandom(), out ItemData item))
                {
                    reason = "장비 성능 등급의 난수 또는 아이템 연결을 확인하세요.";
                    return false;
                }

                counts.TryGetValue(item, out long previous);
                counts[item] = previous + 1;
            }

            foreach (var pair in counts)
            {
                rewards.Add(new RewardAmount(pair.Key, pair.Value));
            }

            return true;
        }

        #endregion // 지급
    }
}
