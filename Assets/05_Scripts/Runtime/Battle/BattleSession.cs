using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Pooling;
using SW.Util;

using ProjectT.Data;
using ProjectT.Deployment;
using ProjectT.Economy;
using ProjectT.Navigation;
using ProjectT.Timing;
using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 전투 하나의 준비·라운드·결과를 연결하는 진입점입니다. 개별 이동과 피해 구현은 각 모듈에 위임합니다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Battle", "ProjectT.Defense.Runtime", "BattleSession")]
    public sealed class BattleSession : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 스테이지 전투의 진행 단계입니다.
        /// </summary>
        public enum Phase
        {
            Preparation,
            Fighting,
            RoundBreak,
            FinalRest,
            Victory,
            Defeat
        }

        #endregion // 데이터

        #region 필드
        [SerializeField] private StageDefinition definition;
        [SerializeField] private WalkableBattlefield battlefield;
        [SerializeField] private BattlePauseController pauseController;
        private SWPool pool;
        [SerializeField] private AllyUnit allyPrefab;
        [SerializeField] private EnemyUnit enemyPrefab;
        [SerializeField] private Transform unitParent;
        [SerializeField] private Transform workshopTarget;
        private readonly List<AllyUnit> allies = new List<AllyUnit>();
        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
        private readonly List<EnemyUnit> resolvedEnemies = new List<EnemyUnit>();
        private FixedRoute route;
        private AllyDeploymentService deployment;
        private BattleCombatSystem combat;
        private int spawnedThisRound;
        private float spawnRemaining;
        private IDisposable resultPause;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 전투가 사용하는 스테이지 정의입니다.
        /// </summary>
        public StageDefinition Definition => definition;

        /// <summary>
        /// 전투가 시작될 때 새로 생성한 배치 지갑입니다.
        /// </summary>
        public BattleDeploymentWallet Wallet { get; private set; }

        /// <summary>
        /// 현재 진행 단계입니다.
        /// </summary>
        public Phase CurrentPhase { get; private set; }

        /// <summary>
        /// 화면에 표시할 1부터 시작하는 라운드 번호입니다.
        /// </summary>
        public int RoundNumber { get; private set; }

        /// <summary>
        /// 실제 피격되는 공방입니다. 체력과 타격 위치를 전투 계산에 제공합니다.
        /// </summary>
        public WorkshopObjective Workshop { get; private set; }

        /// <summary>
        /// 현재까지 처치한 적 수입니다.
        /// </summary>
        public int KilledCount { get; private set; }

        /// <summary>
        /// 구매한 아군 목록입니다. 부활 대기 중인 아군도 포함합니다.
        /// </summary>
        public IReadOnlyList<AllyUnit> Allies => allies;

        /// <summary>
        /// 현재 전장의 적 목록입니다.
        /// </summary>
        public IReadOnlyList<EnemyUnit> Enemies => enemies;

        /// <summary>
        /// 전투와 마우스 명령이 팝업으로 정지된 상태입니다.
        /// </summary>
        public bool IsPaused => pauseController != null && pauseController.IsPaused;

        /// <summary>
        /// 플레이어가 배치와 이동 명령을 내릴 수 있는 상태입니다.
        /// </summary>
        public bool CanCommand => enabled
            && Wallet != null
            && combat != null
            && !IsPaused
            && CurrentPhase != Phase.Victory
            && CurrentPhase != Phase.Defeat;

        /// <summary>
        /// 공격 시각 효과 요청을 표시 모듈로 전달합니다.
        /// </summary>
        public event Action<Vector2, Vector2, bool> Attacked;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 스테이지 참조를 검증하고 지갑·공방·배치·전투 모듈을 초기화합니다.
        /// </summary>
        private void Start()
        {
            Application.runInBackground = true;
            if (definition == null
                || definition.EnemyRoute == null
                || definition.Enemy == null
                || battlefield == null
                || pauseController == null
                || allyPrefab == null
                || enemyPrefab == null
                || unitParent == null
                || workshopTarget == null)
            {
                StopInitialization("스테이지·경로·유닛·전장·정지 관리자·배치 부모·공방 위치 참조를 확인해 주세요.");
                return;
            }

            pool = SWPool.Instance;
            if (!definition.EnemyRoute.TryCreateRoute(out route, out string reason))
            {
                StopInitialization(reason);
                return;
            }

            var nextWallet = BattleDeploymentWallet.Create(definition.StartingCurrency);
            var nextWorkshop = WorkshopObjective.Create(definition.WorkshopMaximumHealth, workshopTarget.position);
            if (nextWallet == null || nextWorkshop == null)
            {
                StopInitialization("시작 재화 또는 공방 체력 설정이 올바르지 않습니다.");
                return;
            }

            deployment = AllyDeploymentService.Create(nextWallet, battlefield, pool, allyPrefab, unitParent);
            if (deployment == null)
            {
                StopInitialization("배치 기능을 준비하지 못했습니다.");
                return;
            }

            Wallet = nextWallet;
            Workshop = nextWorkshop;
            Workshop.Health.Died += OnWorkshopDestroyed;
            combat = new BattleCombatSystem(allies, enemies, Workshop);
            combat.Attacked += OnAttacked;
            CurrentPhase = Phase.Preparation;
        }

        /// <summary>
        /// 초기화 실패 이유를 기록하고 전투 갱신을 중단합니다.
        /// </summary>
        private void StopInitialization(string reason)
        {
            SWLog.LogWarning("[BattleSession] 초기화 중단: " + reason);
            enabled = false;
        }

        /// <summary>
        /// 클래스와 목적지를 받아 구매하고 독립된 아군 목록에 등록합니다.
        /// </summary>
        public bool TryDeploy(
            AllyClassDefinition selectedClass,
            Vector2 destination,
            out AllyUnit unit,
            out string reason)
        {
            unit = null;
            reason = "지금은 배치 명령을 내릴 수 없습니다.";
            if (!CanCommand || deployment == null)
            {
                return false;
            }

            if (!definition.Classes.ContainsClass(selectedClass))
            {
                reason = "이 스테이지에서 사용할 수 없는 클래스입니다.";
                return false;
            }

            if (!deployment.TryDeploy(selectedClass, destination, out unit, out reason))
            {
                return false;
            }

            allies.Add(unit);
            unit.AvailabilityChanged += combat.ReleaseUnavailableBlocker;
            return true;
        }

        /// <summary>
        /// 미리보기에서 현재 상태·잔액·통행 영역을 확인합니다. 실제 배치 시에는 다시 검증합니다.
        /// </summary>
        public bool CanDeployAt(AllyClassDefinition selectedClass, Vector2 position)
        {
            return CanCommand
                && Wallet != null
                && selectedClass != null
                && selectedClass.IsValid
                && definition.Classes.ContainsClass(selectedClass)
                && Wallet.Balance >= selectedClass.DeploymentCost
                && battlefield.IsWalkable(position);
        }

        /// <summary>
        /// 선택한 아군을 이동시키거나 부활 후 이동할 목적지를 예약합니다.
        /// </summary>
        public bool TryMove(AllyUnit unit, Vector2 destination)
        {
            return CanCommand && unit != null && allies.Contains(unit) && unit.TryMove(destination);
        }

        /// <summary>
        /// 준비 또는 라운드 휴식에서 다음 라운드를 한 번 시작합니다. 휴식 시간에는 제한이 없습니다.
        /// </summary>
        public void StartBattle()
        {
            if (Wallet == null || !CanCommand || RoundNumber >= definition.RoundCount)
            {
                return;
            }

            if (CurrentPhase == Phase.Preparation || CurrentPhase == Phase.RoundBreak)
            {
                BeginRound();
            }
        }

        /// <summary>
        /// 최종 휴식 정비를 마친 플레이어의 요청으로 승리 결과를 한 번 확정합니다.
        /// </summary>
        public void CompleteBattle()
        {
            if (CurrentPhase == Phase.FinalRest && CanCommand && Workshop.Health.IsAlive)
            {
                Finish(Phase.Victory);
            }
        }

        /// <summary>
        /// 다음 라운드 번호와 적 생성 대기 상태를 설정합니다.
        /// </summary>
        private void BeginRound()
        {
            RoundNumber++;
            spawnedThisRound = 0;
            spawnRemaining = 0f;
            CurrentPhase = Phase.Fighting;
        }

        /// <summary>
        /// 진행 중인 라운드의 적 생성·교전·종료 조건을 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (Wallet == null
                || IsPaused
                || Time.deltaTime <= 0f
                || CurrentPhase == Phase.Victory
                || CurrentPhase == Phase.Defeat)
            {
                return;
            }

            ReleaseResolvedEnemies();
            if (CurrentPhase != Phase.Fighting)
            {
                return;
            }

            spawnRemaining -= Time.deltaTime;
            if (spawnedThisRound < definition.GetEnemyCount(RoundNumber - 1) && spawnRemaining <= 0f)
            {
                EnemyUnit enemy = pool.Spawn<EnemyUnit>(enemyPrefab.gameObject, route.GetPoint(0), Quaternion.identity, unitParent);
                if (enemy == null || !enemy.Initialize(definition.Enemy, route))
                {
                    if (enemy != null)
                    {
                        pool.Release(enemy.gameObject);
                    }

                    SWLog.LogWarning("[BattleSession] 적 생성 실패: 현재 전투 진행을 중단합니다.");
                    enabled = false;
                    resultPause = pauseController.Pause();
                    return;
                }

                enemy.Resolved += OnEnemyResolved;
                enemies.Add(enemy);
                spawnedThisRound++;
                spawnRemaining = definition.SpawnInterval;
            }

            combat.Tick(Time.time);
            ReleaseResolvedEnemies();
            if (CurrentPhase == Phase.Defeat)
            {
                return;
            }

            if (spawnedThisRound != definition.GetEnemyCount(RoundNumber - 1) || enemies.Count > 0)
            {
                return;
            }

            combat.CancelAttacks();
            CurrentPhase = RoundNumber == definition.RoundCount ? Phase.FinalRest : Phase.RoundBreak;
        }

        /// <summary>
        /// 처치 보상을 지급하고 적을 풀 반환 대기 목록에 등록합니다.
        /// </summary>
        private void OnEnemyResolved(EnemyUnit enemy)
        {
            if (CurrentPhase != Phase.Defeat && CurrentPhase != Phase.Victory)
            {
                Wallet.TryCredit(enemy.Definition.KillReward);
                KilledCount++;
            }

            resolvedEnemies.Add(enemy);
        }

        /// <summary>
        /// 처리가 끝난 적의 이벤트를 해제하고 풀에 반환합니다.
        /// </summary>
        private void ReleaseResolvedEnemies()
        {
            foreach (EnemyUnit enemy in resolvedEnemies)
            {
                enemy.Resolved -= OnEnemyResolved;
                enemies.Remove(enemy);
                pool.Release(enemy.gameObject);
            }

            resolvedEnemies.Clear();
        }

        /// <summary>
        /// 전투 결과를 확정하고 공격과 게임 시간을 정지합니다.
        /// </summary>
        private void Finish(Phase phase)
        {
            if (CurrentPhase == Phase.Victory || CurrentPhase == Phase.Defeat)
            {
                return;
            }

            CurrentPhase = phase;
            combat?.CancelAttacks();
            resultPause = pauseController.Pause();
        }

        /// <summary>
        /// 공방 파괴를 전투 패배로 연결합니다.
        /// </summary>
        private void OnWorkshopDestroyed()
        {
            Finish(Phase.Defeat);
        }

        /// <summary>
        /// 전투의 타격 정보를 표시 모듈에 전달합니다.
        /// </summary>
        private void OnAttacked(Vector2 start, Vector2 end, bool ranged)
        {
            Attacked?.Invoke(start, end, ranged);
        }

        /// <summary>
        /// 장면 종료 시 정지 소유권·전투 이벤트·생성된 유닛을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            resultPause?.Dispose();
            if (Workshop != null)
            {
                Workshop.Health.Died -= OnWorkshopDestroyed;
            }

            if (combat != null)
            {
                combat.Attacked -= OnAttacked;
            }

            foreach (AllyUnit ally in allies)
            {
                if (ally == null)
                {
                    continue;
                }

                if (combat != null)
                {
                    ally.AvailabilityChanged -= combat.ReleaseUnavailableBlocker;
                }

                if (pool != null)
                {
                    pool.Release(ally.gameObject);
                }
            }

            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.Resolved -= OnEnemyResolved;
                if (pool != null)
                {
                    pool.Release(enemy.gameObject);
                }
            }
        }

        #endregion // 함수
    }

    /// <summary>
    /// 공개 클래스 목록을 외부 변경 없이 조회합니다.
    /// </summary>
    internal static class ClassSelectionExtensions
    {
        #region 함수
        /// <summary>
        /// 스테이지에서 제공하는 클래스 목록에 선택한 정의가 포함되는지 확인합니다.
        /// </summary>
        internal static bool ContainsClass(
            this IReadOnlyList<AllyClassDefinition> classes,
            AllyClassDefinition selected)
        {
            for (int index = 0; index < classes.Count; index++)
            {
                if (classes[index] == selected)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion // 함수
    }
}
