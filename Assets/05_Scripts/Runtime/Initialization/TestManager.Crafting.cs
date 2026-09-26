using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using SW.Attributes;
using SW.Util;

using ProjectT.Crafting;
using ProjectT.Data;
using ProjectT.Rewards;

namespace ProjectT.Initialization
{
    /// <summary>
    /// 제작 화면 전의 설계도 해금·제작 확인용 수동 테스트입니다. 실제 인벤토리·소울 저장을 사용합니다.
    /// </summary>
    public sealed partial class TestManager
    {
        #region 제작 테스트 필드
        [SWGroup("제작 테스트")]
        [SerializeField] private ItemData[] craftingTestBlueprints = Array.Empty<ItemData>();
        [SerializeField, SWReadOnly, TextArea(4, 10)]
        private string craftingTestStatus = "Play 후 설계도를 지급하고 인벤토리에서 오른쪽 클릭으로 배운 뒤 제작합니다. 실제 저장에 반영됩니다.";

        #endregion // 제작 테스트 필드

        #region 제작 테스트 버튼
        /// <summary>
        /// 테스트 설계도를 1개씩 지급합니다. 이미 배웠거나 보유 중인 설계도는 규칙에 따라 지급되지 않습니다.
        /// </summary>
        [SWButton("제작: 테스트 설계도 지급")]
        public void TestGrantBlueprints()
        {
            if (!PrepareBattle() || !HasCraftingBlueprints())
            {
                return;
            }

            var rewards = new List<RewardAmount>();
            foreach (ItemData blueprint in craftingTestBlueprints)
            {
                rewards.Add(new RewardAmount(blueprint, 1d));
            }

            bool success = battle.Inventory.TryGrant(Guid.NewGuid().ToString("N"), rewards, out string reason);
            SetCraftingTestStatus(success
                ? "설계도 지급 요청 완료. 배웠거나 보유 중인 설계도는 제외됩니다. 인벤토리에서 오른쪽 클릭으로 사용하세요."
                : "설계도 지급 실패: " + reason);
        }

        /// <summary>
        /// 모든 테스트 설계도의 제작 1회분 재료와 소울을 지급합니다.
        /// </summary>
        [SWButton("제작: 1회분 재료·소울 지급")]
        public void TestGrantCraftingCosts()
        {
            CraftingService service = PrepareCrafting();
            if (service == null)
            {
                return;
            }

            var materials = new Dictionary<ItemData, double>();
            double souls = 0d;
            foreach (ItemData blueprint in craftingTestBlueprints)
            {
                souls += service.GetSoulCost(blueprint);
                foreach (CraftingMaterial material in service.GetMaterials(blueprint))
                {
                    if (material?.Item != null && material.Count > 0)
                    {
                        materials.TryGetValue(material.Item, out double previous);
                        materials[material.Item] = previous + material.Count;
                    }
                }
            }

            var rewards = new List<RewardAmount>();
            foreach (var material in materials)
            {
                rewards.Add(new RewardAmount(material.Key, material.Value));
            }

            string reason = string.Empty;
            bool success = (rewards.Count == 0 || battle.Inventory.TryGrant(Guid.NewGuid().ToString("N"), rewards, out reason))
                && battle.Souls.TryCredit(Guid.NewGuid().ToString("N"), souls, out reason);
            SetCraftingTestStatus(success
                ? "재료 " + rewards.Count + "종과 소울 " + souls.ToString("N0") + " 지급 완료."
                : "재료·소울 지급 실패: " + reason);
        }

        /// <summary>
        /// 배운 첫 번째 테스트 설계도로 장비 1개를 제작합니다.
        /// </summary>
        [SWButton("제작: 배운 설계도로 1개 제작")]
        public void TestCraft()
        {
            CraftingService service = PrepareCrafting();
            if (service == null)
            {
                return;
            }

            foreach (ItemData blueprint in craftingTestBlueprints)
            {
                if (!battle.Inventory.IsLearned(blueprint))
                {
                    continue;
                }

                bool success = service.TryCraft(blueprint, out ItemData crafted, out string reason);
                SetCraftingTestStatus(success
                    ? blueprint.DisplayName + " → " + crafted.DisplayName + " 제작 완료. 소울 잔액 " + battle.Souls.Balance.ToString("N0")
                    : blueprint.DisplayName + " 제작 실패: " + reason);
                return;
            }

            SetCraftingTestStatus("배운 테스트 설계도가 없습니다. 설계도를 지급하고 인벤토리에서 오른쪽 클릭으로 사용하세요.");
        }

        /// <summary>
        /// 테스트 설계도마다 해금·보유 여부와 적용 비용, 현재 제작 가능 여부를 표시합니다.
        /// </summary>
        [SWButton("제작: 해금·비용 상태 표시")]
        public void TestShowCraftingState()
        {
            CraftingService service = PrepareCrafting();
            if (service == null)
            {
                return;
            }

            var text = new StringBuilder("소울 잔액 " + battle.Souls.Balance.ToString("N0"));
            foreach (ItemData blueprint in craftingTestBlueprints)
            {
                text.Append('\n').Append(blueprint.DisplayName)
                    .Append(battle.Inventory.IsLearned(blueprint) ? " [배움]" : " [미해금]")
                    .Append(" · 소울 ").Append(service.GetSoulCost(blueprint).ToString("N0"));
                foreach (CraftingMaterial material in service.GetMaterials(blueprint))
                {
                    if (material?.Item != null && material.Count > 0)
                    {
                        text.Append(" · ").Append(material.Item.DisplayName).Append(' ')
                            .Append(battle.Inventory.CountAvailable(material.Item).ToString("N0")).Append('/').Append(material.Count.ToString("N0"));
                    }
                }

                text.Append(service.CanCraft(blueprint, out string reason) ? " → 제작 가능" : " → " + reason);
            }

            SetCraftingTestStatus(text.ToString());
        }

        #endregion // 제작 테스트 버튼

        #region 제작 테스트 준비
        /// <summary>
        /// 전투와 테스트 설계도를 확인하고 제작 서비스를 만듭니다. 준비할 수 없으면 null입니다.
        /// </summary>
        private CraftingService PrepareCrafting()
        {
            if (!PrepareBattle() || !HasCraftingBlueprints())
            {
                return null;
            }

            CraftingService service = CraftingService.Create(
                battle.Inventory,
                battle.Souls,
                DataManager.Instance.CraftingCost,
                () => UnityEngine.Random.value);
            if (service == null)
            {
                SetCraftingTestStatus("제작 서비스를 만들 수 없습니다. 콘솔 경고를 확인하세요.");
            }

            return service;
        }

        /// <summary>
        /// 테스트 목록에 설계도만 연결되어 있는지 확인합니다.
        /// </summary>
        private bool HasCraftingBlueprints()
        {
            foreach (ItemData blueprint in craftingTestBlueprints)
            {
                if (blueprint == null || !blueprint.IsBlueprint)
                {
                    SetCraftingTestStatus("테스트 설계도 목록의 빈 칸 또는 제작법이 없는 아이템을 확인하세요.");
                    return false;
                }
            }

            if (craftingTestBlueprints.Length == 0)
            {
                SetCraftingTestStatus("TestManager의 테스트 설계도 목록을 연결하세요.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 마지막 제작 테스트 결과를 인스펙터와 로그에 표시합니다.
        /// </summary>
        private void SetCraftingTestStatus(string message)
        {
            craftingTestStatus = message;
            SWLog.Log("[TestManager] " + message);
        }

        #endregion // 제작 테스트 준비
    }
}
