using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TMPro;

using SW.Attributes;
using SW.Base;
using SW.Popup;
using SW.Util;

using ProjectT.Crafting;
using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Progression;
using ProjectT.Research;

namespace ProjectT.UI
{
    /// <summary>
    /// 거점 화면의 조립 지점입니다. 공통 관리자에서 소울·인벤토리·연구를 읽어 출전·연구·제작·인벤토리 화면에 전달하고 출전 장면 전환을 요청합니다.
    /// </summary>
    public sealed class HubUI : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("출전")]
        [SerializeField] private StageData[] stages = Array.Empty<StageData>();
        [SerializeField] private RectTransform stageContent;
        [SerializeField] private HubStageButtonUI stageButtonPrefab;

        [SWGroup("거점 화면")]
        [SerializeField] private Image soulIconImage;
        [SerializeField] private TMP_Text soulText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button researchButton;
        [SerializeField] private Button craftingButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private HubResearchUI research;
        [SerializeField] private HubCraftingUI crafting;
        [SerializeField] private InventoryUI inventory;

        private readonly List<HubStageButtonUI> stageButtons = new List<HubStageButtonUI>();
        private SoulWallet souls;
        private bool departing;
        private bool subscribed;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// Bootstrap의 관리자가 준비한 소울·인벤토리로 화면을 연결합니다. 준비되지 않았으면 출전만 막고 원인을 표시합니다.
        /// </summary>
        private void Start()
        {
            InventoryStore store = InventoryManager.HasInstance ? InventoryManager.Instance.Store : null;
            souls = SoulManager.HasInstance ? SoulManager.Instance.Wallet : null;
            if (!DataManager.HasInstance || store == null || souls == null)
            {
                SWLog.LogWarning("[HubUI] 연결 중단: Bootstrap의 DataManager·InventoryManager·SoulManager를 확인하세요.");
                messageText.text = "공통 데이터를 불러오지 못했습니다. Bootstrap 장면부터 실행하세요.";
                return;
            }

            DataManager data = DataManager.Instance;
            soulIconImage.sprite = data.SoulCurrency != null ? data.SoulCurrency.Icon : null;
            soulIconImage.enabled = soulIconImage.sprite != null;
            inventory.Initialize(store, data.ColorData, data.SpriteData);
            inventory.PresentCoin(null);
            PresentStages();
            souls.Changed += PresentSouls;
            researchButton.onClick.AddListener(ToggleResearch);
            craftingButton.onClick.AddListener(ToggleCrafting);
            inventoryButton.onClick.AddListener(ToggleInventory);
            subscribed = true;
            PresentSouls();
            messageText.text = string.Empty;
            ConnectCrafting(store, data);
            ConnectResearch();
        }

        /// <summary>
        /// 제작 서비스를 만들어 제작 창에 전달합니다. 기본 비용 데이터가 잘못되었으면 제작 버튼을 막습니다.
        /// </summary>
        private void ConnectCrafting(InventoryStore store, DataManager data)
        {
            CraftingService service = CraftingService.Create(store, souls, data.CraftingCost, () => UnityEngine.Random.value);
            if (service == null)
            {
                craftingButton.interactable = false;
                messageText.text = "제작 기본 비용 데이터를 확인하세요. 제작을 사용할 수 없습니다.";
                crafting.Hide();
                return;
            }

            crafting.Initialize(service, store, souls);
        }

        /// <summary>
        /// 준비된 연구 트리를 연구 창에 전달합니다. 연구를 준비하지 못했으면 연구 버튼을 막고 원인을 표시합니다.
        /// </summary>
        private void ConnectResearch()
        {
            ResearchManager manager = ResearchManager.HasInstance ? ResearchManager.Instance : null;
            if (manager == null || manager.System == null)
            {
                researchButton.interactable = false;
                messageText.text = manager != null ? manager.InitializationError : "Bootstrap의 ResearchManager가 없습니다.";
                research.Hide();
                return;
            }

            research.Initialize(manager.System, () => manager.SaveIssue);
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 등록한 스테이지마다 출전 버튼을 만듭니다. 검사에 실패한 스테이지는 누를 수 없습니다.
        /// </summary>
        private void PresentStages()
        {
            foreach (StageData stage in stages)
            {
                if (stage == null)
                {
                    continue;
                }

                HubStageButtonUI button = Instantiate(stageButtonPrefab, stageContent);
                button.Present(stage, stage.IsValid, Depart);
                stageButtons.Add(button);
            }
        }

        /// <summary>
        /// 저장된 소울 잔액을 표시합니다.
        /// </summary>
        private void PresentSouls()
        {
            soulText.text = souls.Balance.ToString("N0");
        }

        #endregion // 표시

        #region 입력
        /// <summary>
        /// 선택한 스테이지 장면으로 출전합니다. 전환 중에는 다른 버튼을 막습니다.
        /// </summary>
        private void Depart(StageData stage)
        {
            if (departing || stage == null || !stage.IsValid)
            {
                return;
            }

            departing = true;
            foreach (HubStageButtonUI button in stageButtons)
            {
                button.SetInteractable(false);
            }

            research.Hide();
            crafting.Hide();
            inventory.Hide();
            messageText.text = stage.DisplayName + "(으)로 출전합니다.";
            SceneManager.LoadSceneAsync(stage.ScenePath);
        }

        /// <summary>
        /// 연구 창을 열거나 닫습니다.
        /// </summary>
        private void ToggleResearch()
        {
            Toggle(research);
        }

        /// <summary>
        /// 제작 창을 열거나 닫습니다.
        /// </summary>
        private void ToggleCrafting()
        {
            Toggle(crafting);
        }

        /// <summary>
        /// 인벤토리를 열거나 닫습니다.
        /// </summary>
        private void ToggleInventory()
        {
            Toggle(inventory);
        }

        /// <summary>
        /// 대상 창이 열려 있으면 닫고, 아니면 다른 창을 모두 닫은 뒤 엽니다. 거점 창은 한 번에 하나만 엽니다.
        /// </summary>
        private void Toggle(SWPopupBase target)
        {
            if (target.IsVisible)
            {
                target.Hide();
                return;
            }

            research.Hide();
            crafting.Hide();
            inventory.Hide();
            target.Show();
        }

        #endregion // 입력

        #region 정리
        /// <summary>
        /// 버튼과 소울 알림 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (!subscribed)
            {
                return;
            }

            souls.Changed -= PresentSouls;
            researchButton.onClick.RemoveListener(ToggleResearch);
            craftingButton.onClick.RemoveListener(ToggleCrafting);
            inventoryButton.onClick.RemoveListener(ToggleInventory);
        }

        #endregion // 정리
    }
}
