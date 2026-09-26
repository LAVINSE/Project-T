using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;
using SW.Util;

using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Rewards;
using ProjectT.UI;

namespace ProjectT.Initialization
{
    /// <summary>
    /// 실제 인벤토리 저장 경로를 사용하는 수동 테스트 버튼입니다. 버튼을 누르기 전에는 지급하지 않습니다.
    /// </summary>
    public sealed partial class TestManager
    {
        #region 인벤토리 테스트 필드
        [SWGroup("인벤토리 테스트")]
        [SerializeField] private ItemData[] inventorySampleItems = Array.Empty<ItemData>();
        [SerializeField] private ItemData[] inventoryScrollItems = Array.Empty<ItemData>();
        [SerializeField, SWReadOnly, TextArea(3, 6)]
        private string inventoryTestStatus = "Play 후 버튼으로 지급합니다. 실제 저장에 반영되며 테스트 목록 아이템만 제거할 수 있습니다.";
        private string lastInventoryRequest;
        private IReadOnlyList<RewardAmount> lastInventoryRewards;

        #endregion // 인벤토리 테스트 필드

        #region 인벤토리 테스트 버튼
        /// <summary>
        /// 필터·수량·등급·스탯·판매가 확인용 기본 세트를 기본 지급량만큼 저장합니다.
        /// </summary>
        [SWButton("인벤토리: 기본 더미 세트 지급")]
        public void TestGrantInventorySamples()
        {
            GrantInventoryItems(inventorySampleItems);
        }

        /// <summary>
        /// 기본 세트와 스크롤용 아이템을 합쳐 지급합니다. 다시 누르면 새 획득으로 수량이 합산됩니다.
        /// </summary>
        [SWButton("인벤토리: 전체 더미 37종 지급 (스크롤)")]
        public void TestGrantInventoryScrollItems()
        {
            var items = new List<ItemData>(inventorySampleItems);
            items.AddRange(inventoryScrollItems);
            GrantInventoryItems(items);
        }

        /// <summary>
        /// 마지막 지급과 같은 식별자로 재요청합니다. 성공한 요청이면 수량과 획득 연출이 반복되지 않습니다.
        /// </summary>
        [SWButton("인벤토리: 마지막 지급 재요청 (중복 확인)")]
        public void TestRetryInventoryGrant()
        {
            if (!PrepareBattle())
            {
                return;
            }

            if (lastInventoryRewards == null)
            {
                SetInventoryTestStatus("먼저 기본 세트 또는 전체 더미 지급 버튼을 누르세요.");
                return;
            }

            CommitInventoryGrant();
        }

        /// <summary>
        /// 테스트 목록에 연결한 종류의 현재 보유량만 한 번 저장하여 제거합니다. 다른 아이템은 유지합니다.
        /// </summary>
        [SWButton("인벤토리: 테스트 목록 아이템 모두 제거")]
        public void TestRemoveInventoryItems()
        {
            if (!PrepareBattle())
            {
                return;
            }

            var definitions = new HashSet<ItemData>(inventorySampleItems);
            definitions.UnionWith(inventoryScrollItems);
            var quantities = new List<InventoryQuantity>();
            foreach (InventoryStack item in battle.Inventory.Items)
            {
                if (item.Definition != null && definitions.Contains(item.Definition))
                {
                    quantities.Add(new InventoryQuantity(item.Identifier, item.Count));
                }
            }

            bool success = battle.Inventory.TryDiscardBatch(quantities, out string reason);
            SetInventoryTestStatus(success
                ? "테스트 목록 " + quantities.Count + "종 제거 완료. 다른 아이템과 지급 기록은 유지합니다."
                : "제거 실패: " + reason);
        }

        /// <summary>
        /// 현재 전투의 인벤토리를 엽니다. 찾지 못하면 상태 안내만 표시합니다.
        /// </summary>
        [SWButton("인벤토리: 화면 열기")]
        public void TestOpenInventory()
        {
            if (!PrepareBattle())
            {
                return;
            }

            foreach (GameObject root in battleScene.GetRootGameObjects())
            {
                InventoryUI inventory = root.GetComponentInChildren<InventoryUI>(true);
                if (inventory != null)
                {
                    inventory.Show();
                    return;
                }
            }

            SetInventoryTestStatus("현재 장면에서 인벤토리 화면을 찾지 못했습니다.");
        }

        #endregion // 인벤토리 테스트 버튼

        #region 지급과 안내
        /// <summary>
        /// 설정된 기본 수량으로 새 지급 요청을 만듭니다. 정의 누락은 지급 전에 중단합니다.
        /// </summary>
        private void GrantInventoryItems(IReadOnlyList<ItemData> items)
        {
            if (!PrepareBattle())
            {
                return;
            }

            var rewards = new List<RewardAmount>();
            var definitions = new HashSet<ItemData>();
            foreach (ItemData item in items)
            {
                if (item == null || !definitions.Add(item))
                {
                    SetInventoryTestStatus("지급 중단: 테스트 목록의 누락·중복 아이템을 확인하세요.");
                    return;
                }

                rewards.Add(new RewardAmount(item, item.DefaultAmount));
            }

            if (rewards.Count == 0)
            {
                SetInventoryTestStatus("지급할 테스트 아이템이 없습니다.");
                return;
            }

            lastInventoryRequest = Guid.NewGuid().ToString("N");
            lastInventoryRewards = rewards.AsReadOnly();
            CommitInventoryGrant();
        }

        /// <summary>
        /// 실제 저장·중복 방지·특별 획득 알림 경로로 지급하고 결과를 안내합니다.
        /// </summary>
        private void CommitInventoryGrant()
        {
            bool success = battle.Inventory.TryGrant(lastInventoryRequest, lastInventoryRewards, out string reason);
            SetInventoryTestStatus(success
                ? "지급 요청 처리 완료. 같은 요청은 중복 지급되지 않습니다. 현재 보유 " + battle.Inventory.Items.Count + "종."
                : "지급 실패: " + reason);
        }

        /// <summary>
        /// 마지막 수동 조작 결과를 인스펙터와 로그에 표시합니다.
        /// </summary>
        private void SetInventoryTestStatus(string message)
        {
            inventoryTestStatus = message;
            SWLog.Log("[TestManager] " + message);
        }

        #endregion // 지급과 안내
    }
}
