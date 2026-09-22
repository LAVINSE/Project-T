using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Pooling;
using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Rewards;
using ProjectT.Units;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 전투 하나의 준비·라운드·결과 단계를 진행하고 배치·적 생성·교전·보상 모듈을 연결합니다.
    /// 전투의 유일한 프레임 갱신으로 모듈을 정해진 순서대로 진행합니다.
    /// </summary>
    [RequireComponent(typeof(BattleTimeController))]
    public sealed class BattleManager : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private StageData stage;
        [SerializeField] private WalkableBattlefield battlefield;
        [SerializeField] private Transform unitParent;
        [SerializeField] private Transform workshopPoint;
        [SerializeField] private WorkshopView workshopView;
        private readonly List<CharacterUnit> allies = new List<CharacterUnit>();
        private DeploymentService deployment;
        private EnemySpawner spawner;
        private RewardService rewards;
        private CombatSystem combat;
        private IDisposable resultPause;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 전투가 사용하는 스테이지 정의입니다.
        /// </summary>
        public StageData Stage => stage;

        /// <summary>
        /// 배속과 정지를 관리하는 같은 객체의 컴포넌트입니다.
        /// </summary>
        public BattleTimeController TimeController { get; private set; }

        /// <summary>
        /// 전투가 시작될 때 새로 생성한 배치 지갑입니다. 초기화 실패 시 null입니다.
        /// </summary>
        public BattleWallet Wallet { get; private set; }

        /// <summary>
        /// 실제 피격되는 공방입니다. 초기화 실패 시 null입니다.
        /// </summary>
        public Workshop Workshop { get; private set; }

        /// <summary>
        /// 현재 진행 단계입니다.
        /// </summary>
        public BattlePhase Phase { get; private set; }

        /// <summary>
        /// 결과가 확정된 단계인지 반환합니다.
        /// </summary>
        public bool IsFinished => Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat;

        /// <summary>
        /// 화면에 표시할 1부터 시작하는 라운드 번호입니다.
        /// </summary>
        public int RoundNumber { get; private set; }

        /// <summary>
        /// 현재까지 처치한 적 수입니다.
        /// </summary>
        public int KilledCount { get; private set; }

        /// <summary>
        /// 마지막 처치의 확률 계산 결과입니다. 현재는 배치 재화만 지급되며 아이템의 소유·영구 저장 결과가 아닙니다.
        /// </summary>
        public IReadOnlyList<RewardAmount> LastCalculatedRewards { get; private set; } = Array.Empty<RewardAmount>();

        /// <summary>
        /// 구매한 아군 목록입니다. 부활 대기 중인 아군도 포함합니다.
        /// </summary>
        public IReadOnlyList<CharacterUnit> Allies => allies;

        /// <summary>
        /// 초기화에 성공해 전투를 진행할 수 있는지 반환합니다.
        /// </summary>
        public bool IsReady => combat != null;

        /// <summary>
        /// 플레이어가 배치와 이동 명령을 내릴 수 있는 상태입니다.
        /// </summary>
        public bool CanCommand => IsReady && enabled && !TimeController.IsPaused && !IsFinished;

        /// <summary>
        /// 단계·라운드·처치 수·잔액·정지 상태 중 하나가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// 공격 시각 효과 요청을 표시 모듈로 전달합니다.
        /// </summary>
        public event Action<Vector2, Vector2, bool> Attacked;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 스테이지 참조를 검증하고 지갑·공방·배치·적 생성·보상·교전 모듈을 준비합니다. 다른 컴포넌트는 Start부터 결과를 사용합니다.
        /// </summary>
        private void Awake()
        {
            TimeController = GetComponent<BattleTimeController>();
            if (stage == null
                || !stage.IsValid
                || !stage.Enemy.IsValid
                || battlefield == null
                || unitParent == null
                || workshopPoint == null
                || workshopView == null)
            {
                StopInitialization("스테이지·적·전장·배치 부모·공방 위치·공방 표시 참조를 확인해 주세요.");
                return;
            }

            FixedRoute route = stage.EnemyRoute.CreateRoute();
            var wallet = BattleWallet.Create(stage.StartingCurrency);
            var workshop = Workshop.Create(stage.WorkshopMaximumHealth, workshopPoint.position);
            if (route == null || wallet == null || workshop == null)
            {
                StopInitialization("경로·시작 재화·공방 체력 설정을 확인해 주세요.");
                return;
            }

            ColorData colors = DataManager.Instance.ColorData;
            Wallet = wallet;
            Workshop = workshop;
            deployment = new DeploymentService(stage, Wallet, battlefield, unitParent, colors);
            spawner = new EnemySpawner(stage.Enemy, route, unitParent, colors, stage.SpawnInterval);
            rewards = new RewardService(stage.DeploymentCurrency, Wallet.TryCredit, () => UnityEngine.Random.value);
            combat = new CombatSystem(allies, spawner.Enemies, Workshop);
            workshopView.Initialize(Workshop, colors);
            Wallet.Changed += NotifyStateChanged;
            Workshop.Health.Died += OnWorkshopDestroyed;
            TimeController.PauseChanged += NotifyStateChanged;
            spawner.EnemyResolved += OnEnemyResolved;
            combat.Attacked += OnAttacked;
            Phase = BattlePhase.Preparation;
        }

        /// <summary>
        /// 초기화 실패 이유를 기록하고 전투 갱신을 중단합니다.
        /// </summary>
        private void StopInitialization(string reason)
        {
            SWLog.LogWarning("[BattleManager] 초기화 중단: " + reason);
            enabled = false;
        }

        #endregion // 초기화

        #region 명령
        /// <summary>
        /// 명령 가능한 상태에서 클래스와 위치를 받아 아군을 배치합니다. 실패하면 비용을 차감하지 않습니다.
        /// </summary>
        public bool TryDeploy(UnitClassData unitClass, Vector2 position, out CharacterUnit unit, out string reason)
        {
            unit = null;
            if (!CanDeployAt(unitClass, position, out reason))
            {
                return false;
            }

            unit = deployment.Deploy(unitClass, position, out reason);
            if (unit == null)
            {
                return false;
            }

            allies.Add(unit);
            unit.AvailabilityChanged += combat.ReleaseUnavailableBlocker;
            return true;
        }

        /// <summary>
        /// 현재 상태·잔액·통행 영역으로 배치할 수 있는지 확인합니다. 불가하면 안내 문구를 반환합니다.
        /// </summary>
        public bool CanDeployAt(UnitClassData unitClass, Vector2 position, out string reason)
        {
            if (!CanCommand)
            {
                reason = "지금은 배치 명령을 내릴 수 없습니다.";
                return false;
            }

            return deployment.CanDeploy(unitClass, position, out reason);
        }

        /// <summary>
        /// 선택한 아군을 이동시키거나 부활 후 이동할 목적지를 예약합니다.
        /// </summary>
        public bool TryMove(CharacterUnit unit, Vector2 destination)
        {
            return CanCommand && unit != null && allies.Contains(unit) && unit.TryMove(destination);
        }

        /// <summary>
        /// 준비 또는 라운드 휴식에서 다음 라운드를 시작합니다. 휴식 시간에는 제한이 없습니다.
        /// </summary>
        public void StartRound()
        {
            if (!CanCommand
                || RoundNumber >= stage.RoundCount
                || (Phase != BattlePhase.Preparation && Phase != BattlePhase.RoundBreak))
            {
                return;
            }

            RoundNumber++;
            spawner.StartRound(stage.GetEnemyCount(RoundNumber - 1));
            SetPhase(BattlePhase.Fighting);
        }

        /// <summary>
        /// 최종 휴식 정비를 마친 플레이어의 요청으로 승리 결과를 확정합니다.
        /// </summary>
        public void CompleteBattle()
        {
            if (Phase == BattlePhase.FinalRest && CanCommand && Workshop.Health.IsAlive)
            {
                Finish(BattlePhase.Victory);
            }
        }

        #endregion // 명령

        #region 진행
        /// <summary>
        /// 적 생성·교전·라운드 종료를 판정한 뒤 모든 유닛의 이동과 부활을 진행합니다.
        /// </summary>
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!IsReady || IsFinished || TimeController.IsPaused || deltaTime <= 0f)
            {
                return;
            }

            if (Phase == BattlePhase.Fighting && !UpdateRound(deltaTime))
            {
                return;
            }

            spawner.ReleaseResolved();
            if (IsFinished)
            {
                return;
            }

            for (int index = 0; index < allies.Count; index++)
            {
                allies[index].Tick(deltaTime);
            }

            spawner.TickEnemies(deltaTime);
        }

        /// <summary>
        /// 라운드 안의 적 생성과 교전을 진행하고 모든 적이 정리되면 휴식으로 전환합니다. 생성에 실패하면 전투를 멈추고 false입니다.
        /// </summary>
        private bool UpdateRound(float deltaTime)
        {
            if (!spawner.Tick(deltaTime))
            {
                enabled = false;
                resultPause = TimeController.Pause();
                return false;
            }

            combat.Tick(Time.time);
            spawner.ReleaseResolved();
            if (!IsFinished && spawner.IsRoundCleared)
            {
                combat.CancelAttacks();
                SetPhase(RoundNumber == stage.RoundCount ? BattlePhase.FinalRest : BattlePhase.RoundBreak);
            }

            return true;
        }

        /// <summary>
        /// 결과 확정 전의 처치만 보상하고 처치 수를 올립니다.
        /// </summary>
        private void OnEnemyResolved(EnemyUnit enemy)
        {
            if (IsFinished)
            {
                return;
            }

            if (rewards.TryGrant(enemy.Definition.Rewards, out var granted, out string reason))
            {
                LastCalculatedRewards = granted;
            }
            else
            {
                LastCalculatedRewards = Array.Empty<RewardAmount>();
                SWLog.LogWarning("[BattleManager] 처치 보상 지급 실패: " + reason);
            }

            KilledCount++;
            NotifyStateChanged();
        }

        /// <summary>
        /// 단계를 바꾸고 화면에 알립니다.
        /// </summary>
        private void SetPhase(BattlePhase phase)
        {
            Phase = phase;
            NotifyStateChanged();
        }

        /// <summary>
        /// 전투 결과를 확정하고 공격과 게임 시간을 정지합니다.
        /// </summary>
        private void Finish(BattlePhase phase)
        {
            if (IsFinished)
            {
                return;
            }

            combat.CancelAttacks();
            resultPause = TimeController.Pause();
            SetPhase(phase);
        }

        /// <summary>
        /// 공방 파괴를 전투 패배로 연결합니다.
        /// </summary>
        private void OnWorkshopDestroyed()
        {
            Finish(BattlePhase.Defeat);
        }

        /// <summary>
        /// 전투의 타격 정보를 표시 모듈에 전달합니다.
        /// </summary>
        private void OnAttacked(Vector2 start, Vector2 end, bool ranged)
        {
            Attacked?.Invoke(start, end, ranged);
        }

        /// <summary>
        /// 상태 변경을 구독자에게 알립니다.
        /// </summary>
        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        #endregion // 진행

        #region 정리
        /// <summary>
        /// 장면 종료 시 정지 소유권·전투 알림·생성된 유닛을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            resultPause?.Dispose();
            if (!IsReady)
            {
                return;
            }

            Wallet.Changed -= NotifyStateChanged;
            Workshop.Health.Died -= OnWorkshopDestroyed;
            TimeController.PauseChanged -= NotifyStateChanged;
            spawner.EnemyResolved -= OnEnemyResolved;
            combat.Attacked -= OnAttacked;
            SWPool pool = SWPool.HasInstance ? SWPool.Instance : null;
            foreach (CharacterUnit ally in allies)
            {
                if (ally == null)
                {
                    continue;
                }

                ally.AvailabilityChanged -= combat.ReleaseUnavailableBlocker;
                if (pool != null && ally.gameObject.activeSelf)
                {
                    pool.Release(ally.gameObject);
                }
            }

            spawner.ReleaseAll();
        }

        #endregion // 정리
    }
}
