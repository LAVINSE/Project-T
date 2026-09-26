using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Inventory
{
    /// <summary>
    /// 영구 아이템의 정수 수량과 지급 완료 기록을 함께 저장합니다. 삭제한 아이템의 과거 지급 기록도 유지합니다.
    /// </summary>
    [Serializable]
    public sealed class InventorySaveData : IProjectSaveData
    {
        #region 필드
        [SerializeField] private int version;
        [SerializeField] private List<InventoryQuantity> quantities;
        [SerializeField] private List<string> grantedRewards;
        [SerializeField] private List<string> slotIdentifiers;
        [SerializeField] private bool hasSlotLayout;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 최초 획득 순서로 보관하는 아이템 수량입니다. 보유 수량이 0인 종류는 포함하지 않습니다.
        /// </summary>
        public IReadOnlyList<InventoryQuantity> Quantities => quantities;

        /// <summary>
        /// 수량 저장까지 완료한 처치 보상의 식별자입니다.
        /// </summary>
        public IReadOnlyList<string> GrantedRewards => grantedRewards;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 수량과 지급 기록으로 다음 저장 상태를 구성합니다.
        /// </summary>
        private InventorySaveData(List<InventoryQuantity> quantities, List<string> grantedRewards, List<string> slotIdentifiers = null)
        {
            version = ProjectDefine.Save.InventoryVersion;
            this.quantities = quantities;
            this.grantedRewards = grantedRewards;
            this.slotIdentifiers = slotIdentifiers;
            hasSlotLayout = slotIdentifiers != null;
        }

        /// <summary>
        /// 저장 파일이 없는 사용자의 빈 인벤토리 상태를 만듭니다.
        /// </summary>
        public static InventorySaveData CreateEmpty()
        {
            return new InventorySaveData(new List<InventoryQuantity>(), new List<string>());
        }

        #endregion // 초기화

        #region 검사와 변경
        /// <summary>
        /// 저장 버전·정수 수량·중복 식별자를 확인합니다. 손상된 저장은 false이며 초기화하지 않습니다.
        /// </summary>
        public bool Validate(out string reason)
        {
            reason = "아이템 저장 형식 또는 지급 기록을 읽을 수 없습니다. 기존 파일을 보존합니다.";
            if (version != ProjectDefine.Save.InventoryVersion || quantities == null || grantedRewards == null)
            {
                return false;
            }

            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (InventoryQuantity quantity in quantities)
            {
                if (quantity == null || quantity.Count <= 0
                    || !Guid.TryParseExact(quantity.Identifier, "N", out _)
                    || !identifiers.Add(quantity.Identifier))
                {
                    return false;
                }
            }

            if (!ValidateSlots(identifiers))
            {
                reason = "아이템 슬롯 배치에 중복·누락 또는 없는 아이템이 있습니다. 기존 파일을 보존합니다.";
                return false;
            }

            identifiers.Clear();
            foreach (string identifier in grantedRewards)
            {
                if (!Guid.TryParseExact(identifier, "N", out _) || !identifiers.Add(identifier))
                {
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// 위치 정보가 있는 저장에서 모든 보유 종류가 정확히 한 슬롯에 있는지 확인합니다.
        /// </summary>
        private bool ValidateSlots(HashSet<string> identifiers)
        {
            if (!hasSlotLayout)
            {
                return true;
            }

            if (slotIdentifiers == null)
            {
                return false;
            }

            var placed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string identifier in slotIdentifiers)
            {
                if (!string.IsNullOrEmpty(identifier)
                    && (!identifiers.Contains(identifier) || !placed.Add(identifier)))
                {
                    return false;
                }
            }

            return placed.Count == identifiers.Count;
        }

        /// <summary>
        /// 원본을 수정하지 않고 슬롯 배치를 복사합니다. 이전 저장은 기존 획득 순서로 읽습니다.
        /// </summary>
        internal List<string> CopySlots()
        {
            if (hasSlotLayout)
            {
                return new List<string>(slotIdentifiers);
            }

            var result = new List<string>(quantities.Count);
            foreach (InventoryQuantity quantity in quantities)
            {
                result.Add(quantity.Identifier);
            }
            return result;
        }

        /// <summary>
        /// 같은 종류를 합산한 저장 후보를 만듭니다. 정수 범위를 넘으면 기존 상태를 유지하고 null입니다.
        /// </summary>
        internal InventorySaveData CreateGrant(string rewardIdentifier, IReadOnlyDictionary<string, long> additions)
        {
            var next = new List<InventoryQuantity>(quantities.Count + additions.Count);
            List<string> nextSlots = CopySlots();
            var included = new HashSet<string>(StringComparer.Ordinal);
            foreach (InventoryQuantity quantity in quantities)
            {
                additions.TryGetValue(quantity.Identifier, out long addition);
                if (addition < 0 || quantity.Count > long.MaxValue - addition)
                {
                    return null;
                }

                next.Add(new InventoryQuantity(quantity.Identifier, quantity.Count + addition));
                included.Add(quantity.Identifier);
            }

            foreach (var addition in additions)
            {
                if (!included.Contains(addition.Key))
                {
                    next.Add(new InventoryQuantity(addition.Key, addition.Value));
                    int empty = nextSlots.FindIndex(string.IsNullOrEmpty);
                    if (empty < 0)
                    {
                        nextSlots.Add(addition.Key);
                    }
                    else
                    {
                        nextSlots[empty] = addition.Key;
                    }
                }
            }

            var nextRewards = new List<string>(grantedRewards) { rewardIdentifier };
            return new InventorySaveData(next, nextRewards, nextSlots);
        }

        /// <summary>
        /// 확인받은 수량만 제거한 저장 후보를 만듭니다. 수량 부족·없는 종류·0 이하 요청이면 null입니다.
        /// </summary>
        internal InventorySaveData CreateDiscard(string identifier, long count)
        {
            if (count <= 0)
            {
                return null;
            }

            bool found = false;
            List<string> nextSlots = CopySlots();
            var next = new List<InventoryQuantity>(quantities.Count);
            foreach (InventoryQuantity quantity in quantities)
            {
                if (quantity.Identifier != identifier)
                {
                    next.Add(quantity);
                    continue;
                }

                if (quantity.Count < count)
                {
                    return null;
                }

                found = true;
                if (quantity.Count > count)
                {
                    next.Add(new InventoryQuantity(identifier, quantity.Count - count));
                }
                else
                {
                    nextSlots[nextSlots.IndexOf(identifier)] = string.Empty;
                }
            }

            return found ? new InventorySaveData(next, new List<string>(grantedRewards), nextSlots) : null;
        }

        /// <summary>
        /// 지정한 슬롯으로 이동하거나 두 아이템을 교환한 후보를 만듭니다. 수량과 지급 기록은 보존합니다.
        /// </summary>
        internal InventorySaveData CreateMove(string identifier, int targetSlot)
        {
            List<string> nextSlots = CopySlots();
            int sourceSlot = nextSlots.IndexOf(identifier);
            if (sourceSlot < 0 || targetSlot < 0
                || targetSlot >= nextSlots.Count + ProjectDefine.Inventory.MinimumVisibleSlots)
            {
                return null;
            }

            while (nextSlots.Count <= targetSlot)
            {
                nextSlots.Add(string.Empty);
            }
            nextSlots[sourceSlot] = nextSlots[targetSlot];
            nextSlots[targetSlot] = identifier;
            return new InventorySaveData(new List<InventoryQuantity>(quantities),
                new List<string>(grantedRewards), nextSlots);
        }

        #endregion // 검사와 변경
    }

    /// <summary>
    /// 아이템 정의의 고정 식별자와 양의 정수 보유 수량을 직렬화합니다.
    /// </summary>
    [Serializable]
    public sealed class InventoryQuantity
    {
        #region 필드
        [SerializeField] private string identifier;
        [SerializeField] private long count;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 자산의 이름이나 경로를 변경해도 유지되는 저장 식별자입니다.
        /// </summary>
        public string Identifier => identifier;

        /// <summary>
        /// 해당 종류의 보유 수량입니다.
        /// </summary>
        public long Count => count;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 저장 후보에서 검사한 식별자와 수량을 복사합니다.
        /// </summary>
        internal InventoryQuantity(string identifier, long count)
        {
            this.identifier = identifier;
            this.count = count;
        }

        #endregion // 초기화
    }
}
