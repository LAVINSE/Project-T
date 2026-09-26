using System;
using System.Collections.Generic;

using SW.Util;

using ProjectT.Data;
using ProjectT.Rewards;

namespace ProjectT.Inventory
{
    /// <summary>
    /// 영구 아이템의 저장·배치와 전투 중 점유를 관리합니다. 화면에는 장착 점유를 제외한 수량을 제공합니다.
    /// </summary>
    public sealed class InventoryStore
    {
        #region 필드
        private readonly InventorySaveStore store;
        private readonly Dictionary<string, ItemData> definitions = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        private readonly Dictionary<ItemData, string> identifiers = new Dictionary<ItemData, string>();
        private readonly HashSet<string> grantedRewards;
        private readonly HashSet<string> learnedBlueprints;
        private readonly Dictionary<object, string> reservations = new Dictionary<object, string>();
        private InventorySaveData data;
        private bool changing;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 보유 여부와 무관하게 카탈로그에 등록된 아이템 정의입니다. 장비 추첨의 저장 대상을 찾을 때 사용합니다.
        /// </summary>
        public IEnumerable<ItemData> ItemDefinitions => definitions.Values;

        /// <summary>
        /// 장착 중인 수량을 제외한 사용 가능한 아이템 목록입니다. 슬롯 순서를 따르며 빈칸은 제외합니다.
        /// </summary>
        public IReadOnlyList<InventoryStack> Items { get; private set; }

        /// <summary>
        /// 저장된 배치입니다. 빈 슬롯은 null이며 변경 시 새 목록으로 교체합니다.
        /// </summary>
        public IReadOnlyList<InventoryStack> Slots { get; private set; }

        /// <summary>
        /// 저장과 수량 반영 후 화면에 전달하는 알림입니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 특별 전리품을 새로 저장·지급한 경우 보상 요청당 한 번 전달합니다. 복원·파괴·중복 요청에는 발생하지 않습니다.
        /// </summary>
        public event Action SpecialLootAcquired;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 목록과 저장 상태를 연결합니다.
        /// </summary>
        private InventoryStore(ItemCatalogData catalog, InventorySaveStore store, InventorySaveData data)
        {
            this.store = store;
            this.data = data;
            foreach (ItemDefinitionReference item in catalog.Items)
            {
                definitions.Add(item.Identifier, item.Definition);
                identifiers.Add(item.Definition, item.Identifier);
            }

            grantedRewards = new HashSet<string>(data.GrantedRewards, StringComparer.Ordinal);
            learnedBlueprints = new HashSet<string>(data.LearnedBlueprints, StringComparer.Ordinal);
            RebuildItems();
        }

        /// <summary>
        /// 아이템 목록과 저장을 검증하여 보관소를 만듭니다. 실패하면 null과 원인을 반환하며 저장을 초기화하지 않습니다.
        /// </summary>
        public static InventoryStore Create(ItemCatalogData catalog, InventorySaveStore store, out string reason)
        {
            reason = "아이템 목록 또는 저장소가 올바르게 연결되지 않았습니다.";
            if (catalog == null || !catalog.IsValid || store == null)
            {
                SWLog.LogWarning("[InventoryStore] 생성 실패: " + reason);
                return null;
            }

            if (!store.TryLoad(out InventorySaveData data, out reason))
            {
                SWLog.LogWarning("[InventoryStore] 생성 실패: " + reason);
                return null;
            }

            return new InventoryStore(catalog, store, data);
        }

        #endregion // 초기화

        #region 보상
        /// <summary>
        /// 파일이나 수량 변경 없이 아이템 지급 가능 여부를 확인합니다. 잘못된 정의·수량·계산 범위는 false입니다.
        /// </summary>
        public bool CanGrant(string rewardIdentifier, IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            return TryPrepareGrant(rewardIdentifier, rewards, out _, out reason);
        }

        /// <summary>
        /// 처치 식별자마다 아이템을 한 번 저장·지급합니다. 아이템을 파괴한 뒤 같은 보상이 재요청되어도 다시 지급하지 않습니다.
        /// </summary>
        public bool TryGrant(string rewardIdentifier, IReadOnlyList<RewardAmount> rewards, out string reason)
        {
            if (!TryPrepareGrant(rewardIdentifier, rewards, out InventorySaveData candidate, out reason))
            {
                return false;
            }

            if (candidate == null)
            {
                return true;
            }

            if (!TryCommit(candidate, out reason))
            {
                return false;
            }

            NotifySpecialLootAcquired(rewards);
            return true;
        }

        /// <summary>
        /// 확정된 보상에서 정수 아이템 수량을 모아 저장 후보를 만듭니다. 중복 또는 아이템 없는 보상은 후보 없이 성공합니다.
        /// </summary>
        private bool TryPrepareGrant(
            string rewardIdentifier,
            IReadOnlyList<RewardAmount> rewards,
            out InventorySaveData candidate,
            out string reason)
        {
            candidate = null;
            reason = string.Empty;
            if (changing || !Guid.TryParseExact(rewardIdentifier, "N", out _) || rewards == null)
            {
                reason = "아이템 지급 정보가 잘못되었거나 저장 중입니다.";
                return false;
            }

            if (grantedRewards.Contains(rewardIdentifier))
            {
                return true;
            }

            var additions = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (RewardAmount reward in rewards)
            {
                if (!(reward.Definition is ItemData item))
                {
                    continue;
                }

                if (!item.IsValid || !reward.Amount.ExIsItemCount() || !identifiers.TryGetValue(item, out string identifier))
                {
                    reason = "아이템 정의·목록 연결·정수 보상 수량을 확인하세요.";
                    return false;
                }

                long amount = (long)reward.Amount;
                if (item.IsBlueprint)
                {
                    amount = IsLearned(item) || HasStored(identifier) || additions.ContainsKey(identifier) ? 0 : Math.Min(amount, 1);
                }

                if (amount == 0)
                {
                    continue;
                }

                additions.TryGetValue(identifier, out long previous);
                if (previous > long.MaxValue - amount)
                {
                    reason = "아이템 보상 합계가 계산 가능한 범위를 벗어났습니다.";
                    return false;
                }

                additions[identifier] = previous + amount;
            }

            if (additions.Count == 0)
            {
                return true;
            }

            candidate = data.CreateGrant(rewardIdentifier, additions);
            if (candidate == null)
            {
                reason = "지급 후 아이템 수량이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            return true;
        }

        #endregion // 보상

        #region 파괴와 조회
        /// <summary>
        /// 전투 장착용 한 개의 점유를 교체합니다. 총보유량은 저장에 남겨 비정상 종료 시에도 장비를 잃지 않습니다.
        /// </summary>
        internal bool TrySetReservation(object owner, string identifier, Func<bool> apply, out string reason)
        {
            reason = string.Empty;
            if (changing || owner == null)
            {
                reason = "아이템을 변경 중입니다. 잠시 후 다시 시도하세요.";
                return false;
            }

            reservations.TryGetValue(owner, out string previous);
            if (identifier != null && identifier != previous && Find(identifier) == null)
            {
                reason = "장착 가능한 장비 수량이 없습니다.";
                return false;
            }

            changing = true;
            try
            {
                if (apply != null && !apply())
                {
                    reason = "장비 효과를 적용할 수 없습니다.";
                    return false;
                }
                if (identifier == null)
                {
                    reservations.Remove(owner);
                }
                else
                {
                    reservations[owner] = identifier;
                }
                RebuildItems();
                NotifyChanged();
                return true;
            }
            finally
            {
                changing = false;
            }
        }

        /// <summary>
        /// 아이템을 빈 슬롯으로 이동하거나 기존 아이템과 교환합니다. 저장에 실패하면 원래 배치를 유지합니다.
        /// </summary>
        public bool TryMove(string identifier, int targetSlot, out string reason)
        {
            reason = string.Empty;
            if (changing || Find(identifier) == null)
            {
                reason = "아이템 저장 중이거나 이동할 아이템이 없습니다.";
                return false;
            }

            if (targetSlot >= 0 && targetSlot < Slots.Count && Slots[targetSlot]?.Identifier == identifier)
            {
                return true;
            }

            InventorySaveData candidate = data.CreateMove(identifier, targetSlot);
            if (candidate == null)
            {
                reason = "아이템을 놓을 수 없는 슬롯입니다.";
                return false;
            }
            return TryCommit(candidate, out reason);
        }

        /// <summary>
        /// 사용자가 확인한 종류와 수량만 파괴합니다. 저장 실패·수량 부족·잘못된 요청이면 기존 보유 수량을 유지합니다.
        /// </summary>
        public bool TryDiscard(string identifier, long count, out string reason)
        {
            reason = string.Empty;
            if (changing)
            {
                reason = "아이템 저장 중입니다. 잠시 후 다시 시도하세요.";
                return false;
            }

            InventoryStack available = Find(identifier);
            if (available?.Definition != null && available.Definition.IsBlueprint)
            {
                reason = "설계도는 버릴 수 없습니다. 오른쪽 클릭으로 사용하세요.";
                return false;
            }

            if (available == null || count <= 0 || count > available.Count)
            {
                reason = "장착 중인 수량을 제외한 보유량만 버릴 수 있습니다.";
                return false;
            }
            InventorySaveData candidate = data.CreateDiscard(identifier, count);
            if (candidate == null)
            {
                reason = "파괴할 수량은 1개부터 현재 보유 수량 사이여야 합니다.";
                return false;
            }

            return TryCommit(candidate, out reason);
        }

        /// <summary>
        /// 지정한 여러 종류의 수량을 한 번 저장하여 제거합니다. 어느 항목이든 잘못되면 모두 유지합니다.
        /// </summary>
        internal bool TryDiscardBatch(IReadOnlyList<InventoryQuantity> quantities, out string reason)
        {
            reason = string.Empty;
            if (changing || quantities == null)
            {
                reason = "아이템 저장 중이거나 제거 목록이 없습니다.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            InventorySaveData candidate = data;
            foreach (InventoryQuantity quantity in quantities)
            {
                if (quantity == null || !seen.Add(quantity.Identifier)
                    || quantity.Count <= 0 || quantity.Count > (Find(quantity.Identifier)?.Count ?? 0))
                {
                    reason = "제거할 아이템 참조 또는 중복 항목을 확인하세요.";
                    return false;
                }

                candidate = candidate.CreateDiscard(quantity.Identifier, quantity.Count);
                if (candidate == null)
                {
                    reason = "제거할 수량은 현재 보유 수량 이내여야 합니다.";
                    return false;
                }
            }

            return quantities.Count == 0 || TryCommit(candidate, out reason);
        }

        /// <summary>
        /// 장착 점유를 제외한 사용 가능 수량입니다. 목록에 없거나 보유하지 않은 아이템은 0입니다.
        /// </summary>
        public long CountAvailable(ItemData item)
        {
            return item != null && identifiers.TryGetValue(item, out string identifier) ? Find(identifier)?.Count ?? 0 : 0;
        }

        /// <summary>
        /// 저장 파일에 한 개 이상 보관 중인지 확인합니다. 장착 점유와 무관한 전체 보유 기준입니다.
        /// </summary>
        private bool HasStored(string identifier)
        {
            foreach (InventoryQuantity quantity in data.Quantities)
            {
                if (quantity.Identifier == identifier)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 보유 중인 종류를 반환합니다. 전부 파괴했거나 없는 종류이면 null입니다.
        /// </summary>
        public InventoryStack Find(string identifier)
        {
            foreach (InventoryStack item in Items)
            {
                if (item.Identifier == identifier)
                {
                    return item;
                }
            }

            return null;
        }

        #endregion // 파괴와 조회

        #region 설계도와 제작
        /// <summary>
        /// 설계도를 사용해 해금했는지 반환합니다. 설계도가 아니거나 목록에 없는 아이템이면 false입니다.
        /// </summary>
        public bool IsLearned(ItemData blueprint)
        {
            return blueprint != null && identifiers.TryGetValue(blueprint, out string identifier)
                && learnedBlueprints.Contains(identifier);
        }

        /// <summary>
        /// 보유한 설계도 1개를 소모해 제작법을 해금합니다. 배운 설계도는 다시 획득하지 않으므로 별도 안내 없이 저장 후보 단계에서 거절합니다.
        /// 설계도가 아니거나 저장에 실패하면 변경하지 않습니다.
        /// </summary>
        public bool TryLearn(string identifier, out string reason)
        {
            reason = string.Empty;
            InventoryStack item = Find(identifier);
            if (changing || item?.Definition == null || !item.Definition.IsBlueprint)
            {
                reason = "사용할 수 있는 설계도가 아닙니다.";
                return false;
            }

            InventorySaveData candidate = data.CreateLearn(identifier);
            if (candidate == null)
            {
                reason = "설계도를 사용할 수 없습니다. 보유 수량을 확인하세요.";
                return false;
            }

            return TryCommit(candidate, out reason);
        }

        /// <summary>
        /// 재료를 소비하고 결과 아이템을 추가할 수 있는지 저장 없이 확인합니다. 수량 부족·잘못된 정의이면 false와 사유입니다.
        /// </summary>
        internal bool CanExchange(IReadOnlyDictionary<ItemData, long> costs, ItemData result, out string reason)
        {
            return TryPrepareExchange(costs, result, out _, out reason);
        }

        /// <summary>
        /// 재료 소비와 결과 추가를 한 번의 저장으로 처리합니다. 실패하면 수량과 배치를 바꾸지 않습니다.
        /// </summary>
        internal bool TryExchange(IReadOnlyDictionary<ItemData, long> costs, ItemData result, out string reason)
        {
            return TryPrepareExchange(costs, result, out InventorySaveData candidate, out reason)
                && TryCommit(candidate, out reason);
        }

        /// <summary>
        /// 장착 점유를 제외한 사용 가능 수량으로 재료를 검사하고 저장 후보를 만듭니다.
        /// </summary>
        private bool TryPrepareExchange(
            IReadOnlyDictionary<ItemData, long> costs,
            ItemData result,
            out InventorySaveData candidate,
            out string reason)
        {
            candidate = null;
            reason = string.Empty;
            if (changing || costs == null || result == null || !identifiers.TryGetValue(result, out string resultIdentifier))
            {
                reason = "제작 결과가 아이템 목록에 없거나 저장 중입니다.";
                return false;
            }

            var removals = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var cost in costs)
            {
                if (cost.Value == 0)
                {
                    continue;
                }

                if (cost.Key == null || cost.Value < 0 || !identifiers.TryGetValue(cost.Key, out string identifier))
                {
                    reason = "재료 아이템과 수량을 확인하세요.";
                    return false;
                }

                long available = CountAvailable(cost.Key);
                if (available < cost.Value)
                {
                    reason = "재료가 부족합니다: " + cost.Key.DisplayName + " " + available.ToString("N0") + " / " + cost.Value.ToString("N0");
                    return false;
                }

                removals.Add(identifier, cost.Value);
            }

            candidate = data.CreateExchange(removals, new Dictionary<string, long>(StringComparer.Ordinal) { { resultIdentifier, 1 } });
            if (candidate == null)
            {
                reason = "제작 후 아이템 수량이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            return true;
        }

        #endregion // 설계도와 제작

        #region 상태 반영
        /// <summary>
        /// 실제 지급한 양수 수량의 특별 전리품이 있으면 한 번 알립니다. 구독자 예외는 저장 성공을 취소하지 않습니다.
        /// </summary>
        private void NotifySpecialLootAcquired(IReadOnlyList<RewardAmount> rewards)
        {
            bool hasSpecialLoot = false;
            foreach (RewardAmount reward in rewards)
            {
                if (reward.Amount > 0d && reward.Definition is ItemData item && item.IsSpecialLoot)
                {
                    hasSpecialLoot = true;
                    break;
                }
            }

            if (!hasSpecialLoot || SpecialLootAcquired == null)
            {
                return;
            }

            foreach (Action subscriber in SpecialLootAcquired.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    SWLog.LogError("[InventoryStore] 특별 전리품 획득 알림 실패: " + exception);
                }
            }
        }

        /// <summary>
        /// 파일 저장에 성공했을 때만 수량·지급 기록·화면을 함께 갱신합니다.
        /// </summary>
        private bool TryCommit(InventorySaveData candidate, out string reason)
        {
            reason = string.Empty;
            changing = true;
            try
            {
                if (!store.TrySave(candidate))
                {
                    reason = "아이템 저장에 실패했습니다. 보유 수량은 변경되지 않았습니다.";
                    return false;
                }

                data = candidate;
                grantedRewards.Clear();
                grantedRewards.UnionWith(data.GrantedRewards);
                learnedBlueprints.Clear();
                learnedBlueprints.UnionWith(data.LearnedBlueprints);
                RebuildItems();
                NotifyChanged();
                return true;
            }
            finally
            {
                changing = false;
            }
        }

        /// <summary>
        /// 저장된 식별자를 현재 정의에 연결합니다. 누락된 정의의 수량을 버리거나 다른 종류에 연결하지 않습니다.
        /// </summary>
        private void RebuildItems()
        {
            var quantities = new Dictionary<string, InventoryQuantity>(StringComparer.Ordinal);
            foreach (InventoryQuantity quantity in data.Quantities)
            {
                quantities.Add(quantity.Identifier, quantity);
            }

            var items = new List<InventoryStack>(quantities.Count);
            var slots = new List<InventoryStack>();
            var reserved = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string identifier in reservations.Values)
            {
                reserved.TryGetValue(identifier, out long count);
                reserved[identifier] = count + 1;
            }
            foreach (string identifier in data.CopySlots())
            {
                if (string.IsNullOrEmpty(identifier))
                {
                    slots.Add(null);
                    continue;
                }

                reserved.TryGetValue(identifier, out long count);
                long available = quantities[identifier].Count - count;
                if (available <= 0)
                {
                    slots.Add(null);
                    continue;
                }
                definitions.TryGetValue(identifier, out ItemData definition);
                var item = new InventoryStack(new InventoryQuantity(identifier, available), definition);
                slots.Add(item);
                items.Add(item);
            }
            Items = items.AsReadOnly();
            Slots = slots.AsReadOnly();
        }

        /// <summary>
        /// 구독자 하나의 표시 실패가 저장 성공 결과와 나머지 알림을 바꾸지 않도록 분리합니다.
        /// </summary>
        private void NotifyChanged()
        {
            if (Changed == null)
            {
                return;
            }

            foreach (Action subscriber in Changed.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    SWLog.LogError("[InventoryStore] 보유 수량 알림 실패: " + exception);
                }
            }
        }

        #endregion // 상태 반영
    }
}
