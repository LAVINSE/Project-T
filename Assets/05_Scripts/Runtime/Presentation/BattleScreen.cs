using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Timing;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 공통 화면을 같은 장면의 전투에 연결합니다. 필수 참조가 없으면 초기화를 중단합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattleScreen")]
    public sealed class BattleScreen : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("공통 화면")]
        [SerializeField] private CharacterHUDUI character;
        [SerializeField] private CharacterSelectedUI deployment;
        [SerializeField] private EconomyHUDUI economy;
        [SerializeField] private InventoryUI inventory;
        [SerializeField] private BottomHUDUI controls;
        [SWGroup("실행 상태와 테스트")]
        [SerializeField, SWReadOnly, TextArea] private string testStatus;
        private BattleSession session;
        private BattleMouseCommand commands;
        private BattlePauseController pauseController;
        private IDisposable manualPause;
        private int speedMultiplier = 1;
        private int totalEnemyCount;
        private bool initialized;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 공통 화면의 현재 전투 연결 여부입니다.
        /// </summary>
        public bool IsInitialized => initialized;

        /// <summary>
        /// 플레이어의 수동 정지 상태입니다. 팝업 정지는 별도로 관리합니다.
        /// </summary>
        public bool IsManuallyPaused => manualPause != null;

        /// <summary>
        /// 정지 중에도 유지하는 선택 배속입니다.
        /// </summary>
        public int SpeedMultiplier => speedMultiplier;

        /// <summary>
        /// 인스펙터에서 확인할 현재 단계와 결과입니다.
        /// </summary>
        public string TestStatus => testStatus;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 같은 장면의 전투 하나를 찾아 연결합니다. 다른 장면의 전투를 참조하지 않습니다.
        /// </summary>
        private void Start()
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (BattleSession candidate in root.GetComponentsInChildren<BattleSession>(true))
                {
                    if (session != null)
                    {
                        StopInitialization("한 장면에 전투가 여러 개 있습니다.");
                        return;
                    }

                    session = candidate;
                }
            }

            commands = session != null ? session.GetComponent<BattleMouseCommand>() : null;
            pauseController = session != null ? session.GetComponent<BattlePauseController>() : null;
            BattlePlacementCommand placement = session != null ? session.GetComponent<BattlePlacementCommand>() : null;
            if (session == null
                || session.Wallet == null
                || session.Workshop == null
                || commands == null
                || placement == null
                || pauseController == null
                || character == null
                || !character.HasRequiredReferences
                || deployment == null
                || !deployment.HasRequiredReferences
                || economy == null
                || !economy.HasRequiredReferences
                || inventory == null
                || !inventory.HasRequiredReferences
                || controls == null
                || !controls.HasRequiredReferences)
            {
                StopInitialization("전투 초기화와 공통 화면의 필수 참조를 확인해 주세요.");
                return;
            }

            SpriteData sharedSprites = DataManager.HasInstance ? DataManager.Instance.SpriteData : null;
            if (!controls.Initialize(sharedSprites))
            {
                StopInitialization("공통 아이콘과 하단 조작 화면을 준비하지 못했습니다.");
                return;
            }

            if (!deployment.Initialize(session, placement))
            {
                StopInitialization("캐릭터 구매 화면을 준비하지 못했습니다.");
                return;
            }

            for (int index = 0; index < session.Definition.RoundCount; index++)
            {
                totalEnemyCount += session.Definition.GetEnemyCount(index);
            }

            inventory.Initialize(pauseController);
            controls.InventoryRequested += ToggleInventory;
            controls.PauseRequested += TogglePause;
            controls.SpeedRequested += CycleSpeed;
            pauseController.TrySetTimeScale(speedMultiplier);
            initialized = true;
            Refresh();
        }

        /// <summary>
        /// 화면 연결 실패를 한 번 기록하고 갱신을 중단합니다.
        /// </summary>
        private void StopInitialization(string reason)
        {
            testStatus = reason;
            SWLog.LogWarning("[BattleScreen] 화면 초기화 중단: " + reason);
            enabled = false;
        }

        /// <summary>
        /// 선택·체력·재화·전투 상태를 해당 화면에 전달합니다.
        /// </summary>
        private void LateUpdate()
        {
            if (initialized)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 전투 데이터만 읽어 표시하며 미구현 파편은 미연결 상태로 전달합니다.
        /// </summary>
        private void Refresh()
        {
            character.Present(commands.SelectedUnit);
            economy.Present(
                session.RoundNumber,
                session.Definition.RoundCount,
                session.KilledCount,
                totalEnemyCount,
                session.Wallet.Balance,
                null);
            inventory.PresentCoin(session.Wallet.Balance);
            deployment.RefreshAvailability();
            bool finished = session.CurrentPhase == BattleSession.Phase.Victory || session.CurrentPhase == BattleSession.Phase.Defeat;
            controls.Present(IsManuallyPaused, speedMultiplier, !finished);
            testStatus = session.Definition.DisplayName + " · " + PhaseDescription(session.CurrentPhase) + "\n처치 " + session.KilledCount + " / " + totalEnemyCount + " · 공방 체력 " + Mathf.CeilToInt(session.Workshop.Health.Current) + " / " + session.Workshop.Health.Maximum.ToString("0") + "\n" + commands.Message;
        }

        /// <summary>
        /// 전투 단계를 인스펙터용 한글 안내로 변환합니다.
        /// </summary>
        private static string PhaseDescription(BattleSession.Phase phase)
        {
            switch (phase)
            {
                case BattleSession.Phase.Preparation:
                    return "전투 준비";
                case BattleSession.Phase.Fighting:
                    return "전투 진행";
                case BattleSession.Phase.RoundBreak:
                    return "라운드 휴식";
                case BattleSession.Phase.FinalRest:
                    return "방어 성공 · 최종 휴식";
                case BattleSession.Phase.Victory:
                    return "승리";
                case BattleSession.Phase.Defeat:
                    return "패배";
                default:
                    return string.Empty;
            }
        }

        #endregion // 초기화

        #region 사용자 조작
        /// <summary>
        /// 인벤토리를 열거나 닫습니다. 미초기화 상태에서는 처리하지 않습니다.
        /// </summary>
        public void ToggleInventory()
        {
            if (!initialized)
            {
                return;
            }

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
        /// 수동 정지만 전환하며 다른 팝업과 결과의 정지는 유지합니다.
        /// </summary>
        public void TogglePause()
        {
            if (!CanControlTime())
            {
                return;
            }

            if (manualPause == null)
            {
                manualPause = pauseController.Pause();
            }
            else
            {
                manualPause.Dispose();
                manualPause = null;
            }

            Refresh();
        }

        /// <summary>
        /// 1배·2배·3배를 순환합니다. 정지 중에는 재개할 배속만 바꿉니다.
        /// </summary>
        public void CycleSpeed()
        {
            if (!CanControlTime())
            {
                return;
            }

            int nextSpeed = speedMultiplier % 3 + 1;
            if (pauseController.TrySetTimeScale(nextSpeed))
            {
                speedMultiplier = nextSpeed;
                Refresh();
            }
        }

        /// <summary>
        /// 결과 확정 후에는 정지·배속 조작을 거절합니다.
        /// </summary>
        private bool CanControlTime()
        {
            return initialized
                && session != null
                && session.CurrentPhase != BattleSession.Phase.Victory
                && session.CurrentPhase != BattleSession.Phase.Defeat;
        }

        #endregion // 사용자 조작

        #region 인스펙터 테스트
        /// <summary>
        /// 준비 또는 휴식에서 라운드를 시작합니다. 다른 단계의 요청은 전투가 거절합니다.
        /// </summary>
        [SWButton("테스트: 전투 시작 / 다음 라운드")]
        public void TestAdvanceRound()
        {
            if (!Application.isPlaying || !initialized)
            {
                return;
            }

            session.StartBattle();
            Refresh();
        }

        /// <summary>
        /// 최종 휴식의 승리를 확정하고 현재 결과를 인스펙터와 로그에 표시합니다.
        /// </summary>
        [SWButton("테스트: 결과 확인")]
        public void TestInspectResult()
        {
            if (!Application.isPlaying || !initialized)
            {
                return;
            }

            if (session.CurrentPhase == BattleSession.Phase.FinalRest)
            {
                session.CompleteBattle();
            }

            Refresh();
            SWLog.Log("[BattleScreen] " + testStatus);
        }

        /// <summary>
        /// 현재 스테이지를 새 전투로 다시 시작합니다. 편집 모드에서는 처리하지 않습니다.
        /// </summary>
        [SWButton("테스트: 스테이지 재시작")]
        public void TestRestartBattle()
        {
            if (!Application.isPlaying || !initialized)
            {
                return;
            }

            ReleaseTimeControls();
            SceneManager.LoadScene(gameObject.scene.path);
        }

        #endregion // 인스펙터 테스트

        #region 정리
        /// <summary>
        /// 화면의 정지를 해제하고 다음 전투의 기본 배속을 복원합니다.
        /// </summary>
        private void ReleaseTimeControls()
        {
            if (inventory != null)
            {
                inventory.Hide();
            }

            manualPause?.Dispose();
            manualPause = null;
            if (pauseController != null && pauseController.isActiveAndEnabled)
            {
                pauseController.TrySetTimeScale(1f);
            }
        }

        /// <summary>
        /// 화면을 끌 때 이 화면의 정지 소유권을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (initialized)
            {
                ReleaseTimeControls();
                speedMultiplier = 1;
            }
        }

        /// <summary>
        /// 버튼 구독을 해제하여 이전 전투 참조가 남지 않도록 합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (controls != null)
            {
                controls.InventoryRequested -= ToggleInventory;
                controls.PauseRequested -= TogglePause;
                controls.SpeedRequested -= CycleSpeed;
            }
        }

        #endregion // 정리
    }
}
