using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Attributes;
using SW.Util;

using ProjectT.Battle;

namespace ProjectT.Initialization
{
    /// <summary>
    /// Main에서 준비되어 장면 전환에도 유지되는 수동 테스트 진입점입니다. 준비된 현재 전투가 없으면 조작하지 않습니다.
    /// </summary>
    public sealed class TestManager : SWSingleton<TestManager>
    {
        #region 필드
        [SWGroup("전투 테스트 상태")]
        [SerializeField, SWReadOnly, TextArea(4, 8)]
        private string testStatus = "Play 후 현재 전투를 연결합니다.";
        private BattleManager battle;
        private BattleInput input;
        private Scene battleScene;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// Main의 관리자 초기화 후 현재 장면을 연결하고 장면 변경을 구독합니다.
        /// </summary>
        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            ConnectCurrentBattle();
        }

        /// <summary>
        /// 현재 활성 장면의 로드가 끝나면 새 전투를 연결합니다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene == SceneManager.GetActiveScene())
            {
                ConnectCurrentBattle();
            }
        }

        /// <summary>
        /// 활성 장면이 바뀌면 이전 전투 구독을 해제하고 현재 전투를 연결합니다.
        /// </summary>
        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            ConnectCurrentBattle();
        }

        /// <summary>
        /// 연결한 장면이 사라지면 파괴된 전투를 테스트하지 않도록 연결을 해제합니다.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            if (scene == battleScene)
            {
                DisconnectBattle();
                testStatus = "현재 장면의 전투 연결을 기다립니다.";
            }
        }

        /// <summary>
        /// 활성 장면에 준비된 전투가 정확히 하나일 때 연결합니다. 없거나 여러 개이면 조작을 막습니다.
        /// </summary>
        private void ConnectCurrentBattle()
        {
            DisconnectBattle();
            Scene current = SceneManager.GetActiveScene();
            if (!current.IsValid() || !current.isLoaded)
            {
                testStatus = "현재 장면의 전투 연결을 기다립니다.";
                return;
            }

            BattleManager selected = null;
            foreach (GameObject root in current.GetRootGameObjects())
            {
                foreach (BattleManager candidate in root.GetComponentsInChildren<BattleManager>(true))
                {
                    if (selected != null)
                    {
                        testStatus = "현재 장면에 전투 진행자가 여러 개 있어 연결하지 않았습니다.";
                        return;
                    }

                    selected = candidate;
                }
            }

            if (selected == null || !selected.IsReady || !selected.isActiveAndEnabled)
            {
                testStatus = "현재 장면에 준비된 전투가 없습니다.";
                return;
            }

            battle = selected;
            battleScene = current;
            input = battle.GetComponent<BattleInput>();
            battle.StateChanged += RefreshStatus;
            battle.Workshop.Health.Changed += RefreshStatus;
            if (input != null)
            {
                input.MessageChanged += RefreshStatus;
            }

            RefreshStatus();
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 전투 단계·처치·공방 체력·입력 안내를 상태 변경 때만 갱신합니다.
        /// </summary>
        private void RefreshStatus()
        {
            if (battle == null || !battle.IsReady)
            {
                return;
            }

            testStatus = battle.Stage.DisplayName + " · " + GetPhaseText(battle.Phase)
                + "\n처치 " + battle.KilledCount + " / " + battle.Stage.TotalEnemyCount
                + " · 공방 체력 " + Mathf.CeilToInt(battle.Workshop.Health.Current)
                + " / " + battle.Workshop.Health.Maximum.ToString("0")
                + "\n" + (input != null ? input.Message : "전투 입력이 연결되지 않았습니다.");
        }

        /// <summary>
        /// 전투 단계를 한글로 표시합니다. 알 수 없는 단계는 빈 문자열입니다.
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

        #region 전투 테스트
        /// <summary>
        /// 실행 중 현재 전투를 사용할 수 있는지 확인합니다. 편집 모드나 연결 실패에서는 false입니다.
        /// </summary>
        private bool PrepareBattle()
        {
            if (!Application.isPlaying)
            {
                SWLog.LogWarning("[TestManager] 실행 실패: Play 후 전투 테스트를 사용하세요.");
                return false;
            }

            if (battle == null || battle.gameObject.scene != SceneManager.GetActiveScene())
            {
                ConnectCurrentBattle();
            }

            if (battle == null || !battle.IsReady || !battle.isActiveAndEnabled)
            {
                SWLog.LogWarning("[TestManager] 실행 실패: 현재 장면에 준비된 전투가 없습니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 준비 또는 휴식에서 라운드를 시작합니다. 다른 단계의 요청은 기존 전투 규칙으로 거절합니다.
        /// </summary>
        [SWButton("테스트: 전투 시작 / 다음 라운드")]
        public void TestAdvanceRound()
        {
            if (PrepareBattle())
            {
                battle.StartRound();
            }
        }

        /// <summary>
        /// 최종 휴식에서 승리를 확정하고 결과를 표시합니다. 진행 중 전투를 강제 승리시키지 않습니다.
        /// </summary>
        [SWButton("테스트: 결과 확인")]
        public void TestInspectResult()
        {
            if (PrepareBattle())
            {
                battle.CompleteBattle();
                RefreshStatus();
                SWLog.Log("[TestManager] " + testStatus);
            }
        }

        /// <summary>
        /// 현재 스테이지를 새 전투로 다시 엽니다. 전투 연결이 없으면 실행하지 않습니다.
        /// </summary>
        [SWButton("테스트: 스테이지 재시작")]
        public void TestRestartBattle()
        {
            if (!PrepareBattle())
            {
                return;
            }

            string path = battle.gameObject.scene.path;
            DisconnectBattle();
            SceneManager.LoadSceneAsync(path);
        }

        #endregion // 전투 테스트

        #region 정리
        /// <summary>
        /// 기존 전투의 상태·체력·입력 알림을 해제하고 참조를 비웁니다.
        /// </summary>
        private void DisconnectBattle()
        {
            if (battle != null)
            {
                battle.StateChanged -= RefreshStatus;
                if (battle.IsReady)
                {
                    battle.Workshop.Health.Changed -= RefreshStatus;
                }
            }

            if (input != null)
            {
                input.MessageChanged -= RefreshStatus;
            }

            battle = null;
            input = null;
            battleScene = default;
        }

        /// <summary>
        /// 장면과 전투 구독을 모두 해제하고 SWUtils 싱글톤 참조를 정리합니다.
        /// </summary>
        public override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            DisconnectBattle();
            base.OnDestroy();
        }

        #endregion // 정리
    }
}
