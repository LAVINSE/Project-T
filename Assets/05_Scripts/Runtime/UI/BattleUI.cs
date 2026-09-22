using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.UI
{
    /// <summary>
    /// 공통 전투 화면을 같은 장면의 전투에 연결합니다. 전투·선택·안내 알림이 올 때만 화면을 갱신합니다.
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
        [SWGroup("실행 상태와 테스트")]
        [SerializeField, SWReadOnly, TextArea] private string testStatus;
        private BattleManager battle;
        private BattleInput input;
        private CharacterUnit displayedUnit;
        private IDisposable manualPause;
        private int speedMultiplier = 1;

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
                testStatus = "같은 장면에서 준비된 전투를 찾지 못했습니다.";
                SWLog.LogWarning("[BattleUI] 화면 연결 중단: " + testStatus);
                enabled = false;
                return;
            }

            input = battle.GetComponent<BattleInput>();
            bottomHUD.Initialize(DataManager.Instance.SpriteData);
            characterSelected.Initialize(battle, input);
            inventory.Initialize(battle.TimeController);
            battle.StateChanged += Refresh;
            input.Selection.Changed += OnSelectionChanged;
            input.MessageChanged += RefreshTestStatus;
            battle.Workshop.Health.Changed += RefreshTestStatus;
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
                null);
            inventory.PresentCoin(battle.Wallet.Balance);
            characterSelected.RefreshAvailability();
            bottomHUD.Present(manualPause != null, speedMultiplier, !battle.IsFinished);
            RefreshTestStatus();
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

        /// <summary>
        /// 인스펙터 확인용 단계·처치·공방 체력·입력 안내를 갱신합니다.
        /// </summary>
        private void RefreshTestStatus()
        {
            testStatus = battle.Stage.DisplayName + " · " + GetPhaseText(battle.Phase)
                + "\n처치 " + battle.KilledCount + " / " + battle.Stage.TotalEnemyCount
                + " · 공방 체력 " + Mathf.CeilToInt(battle.Workshop.Health.Current) + " / " + battle.Workshop.Health.Maximum.ToString("0")
                + "\n" + input.Message;
        }

        /// <summary>
        /// 전투 단계를 한글 안내로 변환합니다.
        /// </summary>
        private static string GetPhaseText(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Preparation:
                    return "전투 준비";
                case BattlePhase.Fighting:
                    return "전투 진행";
                case BattlePhase.RoundBreak:
                    return "라운드 휴식";
                case BattlePhase.FinalRest:
                    return "방어 성공 · 최종 휴식";
                case BattlePhase.Victory:
                    return "승리";
                case BattlePhase.Defeat:
                    return "패배";
                default:
                    return string.Empty;
            }
        }

        #endregion // 표시

        #region 사용자 조작
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

        #region 인스펙터 테스트
        /// <summary>
        /// 준비 또는 휴식에서 라운드를 시작합니다. 다른 단계의 요청은 전투가 거절합니다.
        /// </summary>
        [SWButton("테스트: 전투 시작 / 다음 라운드")]
        public void TestAdvanceRound()
        {
            if (Application.isPlaying && battle != null)
            {
                battle.StartRound();
            }
        }

        /// <summary>
        /// 최종 휴식의 승리를 확정하고 현재 결과를 로그에 표시합니다.
        /// </summary>
        [SWButton("테스트: 결과 확인")]
        public void TestInspectResult()
        {
            if (!Application.isPlaying || battle == null)
            {
                return;
            }

            battle.CompleteBattle();
            RefreshTestStatus();
            SWLog.Log("[BattleUI] " + testStatus);
        }

        /// <summary>
        /// 현재 스테이지를 새 전투로 다시 시작합니다. 편집 모드에서는 처리하지 않습니다.
        /// </summary>
        [SWButton("테스트: 스테이지 재시작")]
        public void TestRestartBattle()
        {
            if (Application.isPlaying && battle != null)
            {
                SceneManager.LoadScene(gameObject.scene.path);
            }
        }

        #endregion // 인스펙터 테스트

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
            input.Selection.Changed -= OnSelectionChanged;
            input.MessageChanged -= RefreshTestStatus;
            battle.Workshop.Health.Changed -= RefreshTestStatus;
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
