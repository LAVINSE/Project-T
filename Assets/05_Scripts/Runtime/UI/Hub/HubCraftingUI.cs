using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Popup;

using ProjectT.Crafting;
using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Progression;

namespace ProjectT.UI
{
    /// <summary>
    /// 배운 설계도 목록과 선택한 제작법의 비용·보유량을 표시하고 제작 요청을 전달합니다. 비용 계산과 저장은 CraftingService가 담당합니다.
    /// </summary>
    public sealed class HubCraftingUI : SWPopupBase
    {
        #region 필드
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform recipeContent;
        [SerializeField] private HubRecipeRowUI recipeRowPrefab;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private TMP_Text recipeNameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button craftButton;
        [SerializeField] private TMP_Text resultText;
        private readonly List<HubRecipeRowUI> rows = new List<HubRecipeRowUI>();
        private readonly List<ItemData> learned = new List<ItemData>();
        private CraftingService crafting;
        private InventoryStore inventory;
        private SoulWallet souls;
        private ItemData selected;
        private bool subscribed;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 제작 서비스와 표시할 보유 정보를 연결합니다. 이미 연결했으면 다시 구독하지 않습니다.
        /// </summary>
        public void Initialize(CraftingService service, InventoryStore store, SoulWallet wallet)
        {
            Hide();
            if (subscribed)
            {
                return;
            }

            crafting = service;
            inventory = store;
            souls = wallet;
            closeButton.onClick.AddListener(Hide);
            craftButton.onClick.AddListener(Craft);
            inventory.Changed += Refresh;
            souls.Changed += Refresh;
            subscribed = true;
        }

        /// <summary>
        /// 열 때 이전 결과 안내를 지우고 현재 보유량으로 다시 표시합니다.
        /// </summary>
        protected override void OnShow()
        {
            resultText.text = string.Empty;
            Refresh();
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 열려 있을 때만 배운 설계도 목록과 선택한 제작법을 갱신합니다. 선택이 사라지면 첫 설계도를 고릅니다.
        /// </summary>
        private void Refresh()
        {
            if (!subscribed || !IsVisible)
            {
                return;
            }

            CollectLearned();
            if (!learned.Contains(selected))
            {
                selected = learned.Count > 0 ? learned[0] : null;
            }

            PresentRows();
            PresentDetail();
        }

        /// <summary>
        /// 아이템 목록에서 배운 설계도만 모읍니다.
        /// </summary>
        private void CollectLearned()
        {
            learned.Clear();
            foreach (ItemData item in inventory.ItemDefinitions)
            {
                if (item != null && item.IsBlueprint && inventory.IsLearned(item))
                {
                    learned.Add(item);
                }
            }

            learned.Sort((left, right) => string.CompareOrdinal(left.DisplayName, right.DisplayName));
        }

        /// <summary>
        /// 설계도 수에 맞춰 줄을 만들고 남는 줄은 숨깁니다.
        /// </summary>
        private void PresentRows()
        {
            while (rows.Count < learned.Count)
            {
                rows.Add(Instantiate(recipeRowPrefab, recipeContent));
            }

            for (int index = 0; index < rows.Count; index++)
            {
                if (index < learned.Count)
                {
                    rows[index].Present(learned[index], learned[index] == selected, Select);
                }
                else
                {
                    rows[index].gameObject.SetActive(false);
                }
            }

            emptyText.gameObject.SetActive(learned.Count == 0);
        }

        /// <summary>
        /// 선택한 제작법의 결과·소울·재료를 보유/필요 형식으로 표시하고 제작 가능 여부를 버튼에 반영합니다.
        /// </summary>
        private void PresentDetail()
        {
            if (selected == null)
            {
                recipeNameText.text = string.Empty;
                costText.text = "배운 설계도가 없습니다. 인벤토리에서 설계도를 오른쪽 클릭해 배우세요.";
                craftButton.interactable = false;
                return;
            }

            recipeNameText.text = selected.Recipe.Result.DisplayName;
            var text = new StringBuilder();
            double soulCost = crafting.GetSoulCost(selected);
            if (soulCost > 0d)
            {
                text.Append("소울 ").Append(souls.Balance.ToString("N0")).Append(" / ").Append(soulCost.ToString("N0")).Append('\n');
            }

            foreach (CraftingMaterial material in crafting.GetMaterials(selected))
            {
                if (material?.Item != null && material.Count > 0)
                {
                    text.Append(material.Item.DisplayName).Append(' ')
                        .Append(inventory.CountAvailable(material.Item).ToString("N0")).Append(" / ")
                        .Append(material.Count.ToString("N0")).Append('\n');
                }
            }

            bool canCraft = crafting.CanCraft(selected, out string reason);
            text.Append(canCraft ? "제작할 수 있습니다." : reason);
            costText.text = text.Length > 0 ? text.ToString() : "필요한 비용이 없습니다.";
            craftButton.interactable = canCraft;
        }

        #endregion // 표시

        #region 입력
        /// <summary>
        /// 목록에서 설계도를 선택합니다.
        /// </summary>
        private void Select(ItemData blueprint)
        {
            selected = blueprint;
            resultText.text = string.Empty;
            PresentRows();
            PresentDetail();
        }

        /// <summary>
        /// 선택한 설계도로 장비 1개를 제작하고 결과를 표시합니다. 보유량 변경은 알림으로 다시 그립니다.
        /// </summary>
        private void Craft()
        {
            if (selected == null)
            {
                return;
            }

            bool success = crafting.TryCraft(selected, out ItemData result, out string reason);
            resultText.text = success ? "제작 완료: " + result.DisplayName : reason;
            PresentDetail();
        }

        #endregion // 입력

        #region 정리
        /// <summary>
        /// 버튼과 보유 알림 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (!subscribed)
            {
                return;
            }

            closeButton.onClick.RemoveListener(Hide);
            craftButton.onClick.RemoveListener(Craft);
            inventory.Changed -= Refresh;
            souls.Changed -= Refresh;
        }

        #endregion // 정리
    }
}
