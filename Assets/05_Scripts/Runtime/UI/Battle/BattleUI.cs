using System;
using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.UI
{
    /// <summary>
    /// 공통 전투 화면을 같은 장면의 전투에 연결합니다. 전투·선택 알림이 올 때만 화면을 갱신합니다.
    /// </summary>
    public sealed class BattleUI : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("공통 화면")]
        [SerializeField] private CharacterHUDUI characterHUD;
        [SerializeField] private CharacterSelectedUI characterSelected;
        [SerializeField] private EconomyHUDUI economyHUD;
        [SerializeField] private InventoryUI inventory;
        [SerializeField] private BottomHUDUI bottomHUD;
        [SerializeField] private BattleProgressUI progress;
        [SerializeField] private BattleMessageUI message;
        [SerializeField] private BattleResultPopupUI statisticsPopup;

        private BattleManager battle;
        private BattleInput input;
        private CharacterUnit displayedUnit;
        private IDisposable manualPause;
        private int speedMultiplier = 1;
        private BattlePhase? displayedPhase;
        private bool displayedPause;
        private string displayedRewardIssue;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 같은 장면의 전투 하나를 찾아 화면과 알림을 연결합니다. 전투가 준비되지 않았으면 연결하지 않습니다.
        /// </summary>
        private void Start()
        {
            battle = FindBattle();
            if (battle == null || !battle.IsReady)
            {
                SWLog.LogWarning("[BattleUI] 화면 연결 중단: 같은 장면에서 준비된 전투를 찾지 못했습니다.");
                enabled = false;
                return;
            }

            input = battle.GetComponent<BattleInput>();
            bottomHUD.Initialize(DataManager.Instance.SpriteData, battle.Inventory);
            economyHUD.Initialize(DataManager.Instance.SoulCurrency);
            characterSelected.Initialize(battle, input);
            inventory.Initialize(battle.Inventory,
                DataManager.Instance.ColorData, DataManager.Instance.SpriteData);
            input.SetFieldInputBlocker(() => inventory.BlocksFieldInput);
            characterHUD.Initialize(battle.Equipment, DataManager.Instance.SpriteData, EquipSelected);
            battle.Equipment.Changed += RefreshCharacter;
            progress.Initialize();
            statisticsPopup.Initialize(DataManager.Instance.SpriteData);
            progress.AdvanceRequested += AdvanceBattle;
            input.MessageChanged += RefreshInputMessage;
            battle.StateChanged += Refresh;
            input.Selection.Changed += OnSelectionChanged;

            bottomHUD.InventoryRequested += ToggleInventory;
            bottomHUD.PauseRequested += TogglePause;
            bottomHUD.SpeedRequested += CycleSpeed;
            battle.TimeController.TrySetTimeScale(speedMultiplier);
            OnSelectionChanged();
            Refresh();
        }

        /// <summary>
        /// 이 화면이 속한 장면의 전투를 찾습니다. 다른 장면의 전투는 참조하지 않습니다.
        /// </summary>
        private BattleManager FindBattle()
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<BattleManager>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 전투 진행·재화·조작 상태를 각 화면에 전달합니다. 재화를 지급하거나 보관하지 않습니다.
        /// </summary>
        private void Refresh()
        {
            economyHUD.Present(
                battle.RoundNumber,
                battle.Stage.RoundCount,
                battle.KilledCount,
                battle.Stage.TotalEnemyCount,
                battle.Wallet.Balance,
                null,
                battle.Souls.Balance);
            inventory.PresentCoin(battle.Wallet.Balance);
            characterSelected.RefreshAvailability();
            bottomHUD.Present(manualPause != null, speedMultiplier, !battle.IsFinished);
            progress.Present(battle.Phase, battle.CanCommand, battle.TimeController.IsPaused);
            if (displayedPhase != battle.Phase
                || displayedPause != battle.TimeController.IsPaused
                || displayedRewardIssue != battle.RewardIssue)
            {
                displayedPhase = battle.Phase;
                displayedPause = battle.TimeController.IsPaused;
                displayedRewardIssue = battle.RewardIssue;
                PresentPhaseMessage();
            }

            if (battle.IsFinished)
            {
                if (inventory.IsVisible)
                {
                    inventory.Hide();
                }

                statisticsPopup.Present(battle.Statistics);
            }
        }

        /// <summary>
        /// 현재 단계와 정지 상태에 맞는 안내를 표시합니다.
        /// </summary>
        private void PresentPhaseMessage()
        {
            if (!string.IsNullOrEmpty(battle.RewardIssue))
            {
                message.PresentMessage(battle.RewardIssue);
                return;
            }

            if (battle.IsFinished)
            {
                message.PresentMessage("전투 통계에서 캐릭터를 선택하면 상세 정보를 볼 수 있습니다.");
                return;
            }

            if (battle.TimeController.IsPaused)
            {
                message.PresentMessage("일시 정지 · 캐릭터 선택, 이동 예약과 장비 변경이 가능합니다.");
                return;
            }

            switch (battle.Phase)
            {
                case BattlePhase.Preparation:
                    message.PresentMessage("캐릭터를 배치한 뒤 전투 시작을 눌러주세요.");
                    break;
                case BattlePhase.RoundBreak:
                    message.PresentMessage("정비를 마치면 전투 시작을 눌러 다음 라운드를 진행하세요.");
                    break;
                case BattlePhase.FinalRest:
                    message.PresentMessage("정비를 마치면 결과 보기를 눌러주세요.");
                    break;
                default:
                    message.PresentMessage(input.Message);
                    break;
            }
        }

        /// <summary>
        /// 배치·이동·장비의 최신 안내를 정지 중에도 전달합니다.
        /// </summary>
        private void RefreshInputMessage()
        {
            if (battle.CanInteract && string.IsNullOrEmpty(battle.RewardIssue))
            {
                message.PresentMessage(input.Message);
            }
            else
            {
                PresentPhaseMessage();
            }
        }

        /// <summary>
        /// 선택한 아군이 바뀌면 체력·부활 알림을 다시 구독하고 표시합니다.
        /// </summary>
        private void OnSelectionChanged()
        {
            if (displayedUnit != null)
            {
                displayedUnit.HealthChanged -= RefreshCharacter;
                displayedUnit.RevivalChanged -= RefreshCharacter;
            }

            displayedUnit = input.Selection.SelectedUnit;
            if (displayedUnit != null)
            {
                displayedUnit.HealthChanged += RefreshCharacter;
                displayedUnit.RevivalChanged += RefreshCharacter;
            }

            RefreshCharacter();
        }

        /// <summary>
        /// 선택한 아군의 체력과 부활 상태를 표시합니다.
        /// </summary>
        private void RefreshCharacter()
        {
            characterHUD.Present(displayedUnit);
        }

        #endregion // 표시

        #region 사용자 조작
        /// <summary>
        /// 선택 캐릭터의 장비를 변경하고 실패 사유를 기존 안내 영역에 표시합니다.
        /// </summary>
        private bool EquipSelected(int index, string identifier)
        {
            if (identifier == null && inventory.BlocksFieldInput)
            {
                return false;
            }
            if (!battle.TryEquip(input.Selection.SelectedUnit, index, identifier, out string reason))
            {
                input.ShowMessage(reason);
                return false;
            }
            input.ShowMessage(identifier == null ? "장비를 해제했습니다." : "장비를 장착했습니다.");
            return true;
        }

        /// <summary>
        /// 진행 요청을 현재 전투 단계의 기존 기능에 전달합니다. 중복 클릭과 정지 중 요청은 무시합니다.
        /// </summary>
        private void AdvanceBattle()
        {
            if (!battle.CanCommand)
            {
                return;
            }

            if (battle.Phase == BattlePhase.FinalRest)
            {
                battle.CompleteBattle();
            }
            else if (battle.Phase == BattlePhase.Preparation || battle.Phase == BattlePhase.RoundBreak)
            {
                battle.StartRound();
            }
        }

        /// <summary>
        /// 인벤토리를 열거나 닫습니다.
        /// </summary>
        public void ToggleInventory()
        {
            if (inventory.IsVisible)
            {
                inventory.Hide();
            }
            else
            {
                inventory.Show();
            }
        }

        /// <summary>
        /// 수동 정지만 전환하며 팝업과 결과의 정지는 유지합니다. 결과 확정 후에는 무시합니다.
        /// </summary>
        public void TogglePause()
        {
            if (battle.IsFinished)
            {
                return;
            }

            if (manualPause == null)
            {
                manualPause = battle.TimeController.Pause();
            }
            else
            {
                manualPause.Dispose();
                manualPause = null;
            }

            Refresh();
        }

        /// <summary>
        /// 1배부터 최대 배속까지 순환합니다. 정지 중에는 재개할 배속만 바꿉니다.
        /// </summary>
        public void CycleSpeed()
        {
            if (battle.IsFinished)
            {
                return;
            }

            int nextSpeed = speedMultiplier % ProjectDefine.Battle.MaximumSpeedMultiplier + 1;
            if (battle.TimeController.TrySetTimeScale(nextSpeed))
            {
                speedMultiplier = nextSpeed;
                Refresh();
            }
        }

        #endregion // 사용자 조작

        #region 정리
        /// <summary>
        /// 이 화면의 정지 소유권과 모든 알림 구독을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            manualPause?.Dispose();
            manualPause = null;
            if (battle == null || input == null)
            {
                return;
            }

            battle.StateChanged -= Refresh;
            battle.Equipment.Changed -= RefreshCharacter;
            input.SetFieldInputBlocker(null);
            input.Selection.Changed -= OnSelectionChanged;
            input.MessageChanged -= RefreshInputMessage;
            progress.AdvanceRequested -= AdvanceBattle;
            bottomHUD.InventoryRequested -= ToggleInventory;
            bottomHUD.PauseRequested -= TogglePause;
            bottomHUD.SpeedRequested -= CycleSpeed;
            if (displayedUnit != null)
            {
                displayedUnit.HealthChanged -= RefreshCharacter;
                displayedUnit.RevivalChanged -= RefreshCharacter;
            }
        }

        #endregion // 정리
    }
}
