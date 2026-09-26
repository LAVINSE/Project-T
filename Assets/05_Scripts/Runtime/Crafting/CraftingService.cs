using System;
using System.Collections.Generic;

using SW.Util;

using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Progression;
using ProjectT.Rewards;

namespace ProjectT.Crafting
{
    /// <summary>
    /// 해금한 설계도로 장비를 제작합니다. 소울 차감 → 재료 소비·결과 지급 순서로 저장하며 뒤 단계가 실패하면 소울을 돌려줍니다.
    /// </summary>
    public sealed class CraftingService
    {
        #region 필드
        private readonly InventoryStore inventory;
        private readonly SoulWallet souls;
        private readonly CraftingCostData defaults;
        private readonly Func<double> nextRandom;
        private bool crafting;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 검증된 보관소·지갑·기본 비용·난수 공급자를 연결합니다.
        /// </summary>
        private CraftingService(InventoryStore inventory, SoulWallet souls, CraftingCostData defaults, Func<double> nextRandom)
        {
            this.inventory = inventory;
            this.souls = souls;
            this.defaults = defaults;
            this.nextRandom = nextRandom;
        }

        /// <summary>
        /// 제작에 필요한 연결을 검사해 서비스를 만듭니다. 기본 비용 자산이 없으면 모든 제작법이 덮어쓴 비용만 사용합니다.
        /// </summary>
        public static CraftingService Create(
            InventoryStore inventory,
            SoulWallet souls,
            CraftingCostData defaults,
            Func<double> nextRandom)
        {
            if (inventory == null || souls == null || nextRandom == null)
            {
                SWLog.LogWarning("[CraftingService] 생성 실패: 인벤토리·소울 지갑·난수 공급자를 확인하세요.");
                return null;
            }

            if (defaults != null && !defaults.IsValid)
            {
                SWLog.LogWarning("[CraftingService] 생성 실패: 제작 기본 비용 데이터의 검사 오류를 확인하세요.");
                return null;
            }

            return new CraftingService(inventory, souls, defaults, nextRandom);
        }

        #endregion // 초기화

        #region 제작
        /// <summary>
        /// 적용할 소울 비용입니다. 0이면 필요 없습니다.
        /// </summary>
        public double GetSoulCost(ItemData blueprint)
        {
            return blueprint?.Recipe != null ? blueprint.Recipe.GetSoulCost(defaults) : 0d;
        }

        /// <summary>
        /// 적용할 재료 목록입니다. 수량 0인 재료는 필요 없습니다.
        /// </summary>
        public IReadOnlyList<CraftingMaterial> GetMaterials(ItemData blueprint)
        {
            return blueprint?.Recipe != null ? blueprint.Recipe.GetMaterials(defaults) : Array.Empty<CraftingMaterial>();
        }

        /// <summary>
        /// 저장 없이 제작 가능 여부를 확인합니다. 미해금·재료나 소울 부족·잘못된 제작법이면 false와 사유입니다.
        /// </summary>
        public bool CanCraft(ItemData blueprint, out string reason)
        {
            return TryPrepare(blueprint, out _, out _, out reason)
                && inventory.CanExchange(CollectCosts(blueprint), AnyResult(blueprint), out reason);
        }

        /// <summary>
        /// 장비 1개를 제작합니다. 성능 등급은 장비 가중치로 추첨합니다. 실패하면 소울·재료·결과를 모두 바꾸지 않습니다.
        /// </summary>
        public bool TryCraft(ItemData blueprint, out ItemData crafted, out string reason)
        {
            crafted = null;
            if (crafting)
            {
                reason = "다른 제작을 처리 중입니다.";
                return false;
            }

            if (!TryPrepare(blueprint, out EquipmentDropResolver resolver, out double soulCost, out reason))
            {
                return false;
            }

            if (!resolver.TryResolve(nextRandom(), out ItemData result))
            {
                reason = "장비 성능 등급의 난수 또는 아이템 연결을 확인하세요.";
                return false;
            }

            IReadOnlyDictionary<ItemData, long> costs = CollectCosts(blueprint);
            if (!inventory.CanExchange(costs, result, out reason))
            {
                return false;
            }

            crafting = true;
            try
            {
                if (!souls.TrySpend(soulCost, out reason))
                {
                    return false;
                }

                if (!inventory.TryExchange(costs, result, out reason))
                {
                    if (!souls.TryRefund(soulCost, out string refundReason))
                    {
                        SWLog.LogError("[CraftingService] 제작 실패 후 소울 반환 실패: " + soulCost + " · " + refundReason);
                        reason += " 사용한 소울을 돌려주지 못했습니다.";
                    }

                    return false;
                }

                crafted = result;
                return true;
            }
            finally
            {
                crafting = false;
            }
        }

        /// <summary>
        /// 설계도·해금·결과 장비·소울 잔액을 확인하고 등급 추첨기를 준비합니다.
        /// </summary>
        private bool TryPrepare(
            ItemData blueprint,
            out EquipmentDropResolver resolver,
            out double soulCost,
            out string reason)
        {
            resolver = null;
            soulCost = 0d;
            if (blueprint == null || !blueprint.IsBlueprint || !blueprint.Recipe.IsValid)
            {
                reason = "설계도와 제작법 설정을 확인하세요.";
                return false;
            }

            if (!inventory.IsLearned(blueprint))
            {
                reason = "아직 배우지 않은 설계도입니다. 인벤토리에서 오른쪽 클릭으로 사용하세요.";
                return false;
            }

            soulCost = GetSoulCost(blueprint);
            if (souls.Balance < soulCost)
            {
                reason = "소울이 부족합니다: " + souls.Balance.ToString("N0") + " / " + soulCost.ToString("N0");
                return false;
            }

            resolver = EquipmentDropResolver.Create(blueprint.Recipe.Result, inventory.ItemDefinitions, out reason);
            return resolver != null;
        }

        /// <summary>
        /// 적용할 재료를 아이템별 수량으로 모읍니다. 수량 0은 제외합니다.
        /// </summary>
        private IReadOnlyDictionary<ItemData, long> CollectCosts(ItemData blueprint)
        {
            var costs = new Dictionary<ItemData, long>();
            foreach (CraftingMaterial material in GetMaterials(blueprint))
            {
                if (material != null && material.Count > 0)
                {
                    costs.TryGetValue(material.Item, out long previous);
                    costs[material.Item] = previous + material.Count;
                }
            }

            return costs;
        }

        /// <summary>
        /// 가능 여부 확인용으로 추첨 가능한 첫 결과 아이템을 찾습니다. 없으면 null입니다.
        /// </summary>
        private ItemData AnyResult(ItemData blueprint)
        {
            foreach (ItemData item in inventory.ItemDefinitions)
            {
                if (item != null && item.Equipment == blueprint.Recipe.Result)
                {
                    return item;
                }
            }

            return null;
        }

        #endregion // 제작
    }
}
