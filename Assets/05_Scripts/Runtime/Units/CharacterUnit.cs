using System;
using UnityEngine;

using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Units
{
    /// <summary>
    /// 구매한 아군 한 명의 수명과 명령을 관리합니다. 같은 클래스도 서로 독립된 상태를 가집니다.
    /// </summary>
    public sealed class CharacterUnit : UnitBase
    {
        #region 필드
        private readonly SWTimer revivalTimer = new SWTimer(0f, false, false, SWTimer.TimeMode.Manual);
        private WalkableBattlefield battlefield;
        private int displayedRevivalSeconds;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 런타임 동시 저지 수입니다. 초기화 전에는 0입니다.
        /// </summary>
        public int BlockCapacity
        {
            get
            {
                float value = Stats?.GetValue(Definition.BlockCapacityStat) ?? 0f;
                return value >= int.MaxValue ? int.MaxValue : Mathf.Max(0, Mathf.FloorToInt(value));
            }
        }

        /// <summary>
        /// 런타임 부활 대기시간입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float RevivalSeconds => Stats?.GetValue(Definition.RevivalSecondsStat) ?? 0f;

        /// <summary>
        /// 이 개체가 사용하는 클래스입니다.
        /// </summary>
        public UnitClassData Definition { get; private set; }

        /// <inheritdoc/>
        public override UnitData Data => Definition;

        /// <summary>
        /// 목적지 명령과 현재 이동 상태입니다.
        /// </summary>
        public CharacterMovement Movement { get; private set; }

        /// <summary>
        /// 부활까지 남은 게임 시간입니다.
        /// </summary>
        public float RevivalRemaining => revivalTimer.IsRunning ? revivalTimer.Remaining : 0f;

        /// <summary>
        /// 생존하고 목적지에 도착한 아군만 교전할 수 있습니다.
        /// </summary>
        public bool CanFight => Health != null && Health.IsAlive && !Movement.IsMoving;

        /// <inheritdoc/>
        public override bool IsMoving => Health != null && Health.IsAlive && Movement.IsMoving;

        /// <summary>
        /// 이 개체의 마지막 이동 목적지입니다. 부활 대기 중에도 유지합니다.
        /// </summary>
        public Vector2 RequestedDestination { get; private set; }

        /// <summary>
        /// 현재 준비하거나 진행 중인 공격의 적 대상입니다.
        /// </summary>
        internal EnemyUnit AttackTarget { get; set; }

        /// <summary>
        /// 이동·사망·부활로 교전 가능 상태가 바뀌었을 때 전투에 알립니다.
        /// </summary>
        public event Action<CharacterUnit> AvailabilityChanged;

        /// <summary>
        /// 화면에 표시하는 부활 남은 초가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action RevivalChanged;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 생성 위치에서 독립된 체력과 클래스별 부활 상태로 초기화합니다.
        /// </summary>
        public bool Initialize(UnitClassData definition, WalkableBattlefield terrain, Vector2 spawn)
        {
            if (definition == null || !definition.IsValid)
            {
                SWLog.LogWarning("[CharacterUnit] 초기화 실패: 클래스 설정이 올바르지 않습니다.");
                return false;
            }

            RuntimeStatCollection nextStats = RuntimeStatCollection.Create(definition.GetStatSettings());
            if (nextStats == null)
            {
                return false;
            }

            Health nextHealth = Health.Create(nextStats.GetValue(definition.MaximumHealthStat));
            var nextMovement = CharacterMovement.Create(transform, terrain, spawn, definition.MoveSpeed);
            if (nextHealth == null || nextMovement == null)
            {
                nextStats.Dispose();
                return false;
            }

            if (Movement != null)
            {
                Movement.MovementChanged -= OnMovementChanged;
            }

            Definition = definition;
            battlefield = terrain;
            RequestedDestination = spawn;
            revivalTimer.Stop();
            AttackTarget = null;
            Movement = nextMovement;
            Movement.MovementChanged += OnMovementChanged;
            SetHealth(nextHealth);
            SetStats(nextStats);
            NotifyInitialized();
            return true;
        }

        #endregion // 초기화

        #region 함수
        /// <inheritdoc/>
        protected override void OnStatsChanged()
        {
            base.OnStatsChanged();
            Movement?.SetMoveSpeed(MoveSpeed);
        }

        /// <summary>
        /// 생존 중에는 즉시 이동하고 부활 중에는 부활 후 이동할 목적지를 갱신합니다.
        /// </summary>
        public bool TryMove(Vector2 destination)
        {
            if (Health == null)
            {
                return false;
            }

            if (Health.IsAlive)
            {
                if (!Movement.TryMove(destination))
                {
                    return false;
                }
            }
            else if (!battlefield.TryFindPath(transform.position, destination, out _))
            {
                return false;
            }

            RequestedDestination = destination;
            NotifyMovingChanged();
            return true;
        }

        /// <inheritdoc/>
        public override void Tick(float deltaTime)
        {
            if (Health == null)
            {
                return;
            }

            if (Health.IsAlive)
            {
                if (Movement.Advance(deltaTime))
                {
                    NotifyMoved();
                }

                return;
            }

            AdvanceRevival(deltaTime);
        }

        /// <summary>
        /// 게임 시간으로 부활을 진행합니다. 전투가 정지하면 부활도 정지하며, 부활 위치를 준비하지 못하면 다음 갱신에 다시 시도합니다.
        /// </summary>
        private void AdvanceRevival(float deltaTime)
        {
            if (revivalTimer.IsRunning && !revivalTimer.Tick(deltaTime))
            {
                int seconds = Mathf.CeilToInt(RevivalRemaining);
                if (seconds != displayedRevivalSeconds)
                {
                    displayedRevivalSeconds = seconds;
                    RevivalChanged?.Invoke();
                }

                return;
            }

            if (!Movement.Reset(transform.position))
            {
                return;
            }

            AttackTarget = null;
            Health.Revive();
            Attack.Reset();
            Movement.TryMove(RequestedDestination);
            AvailabilityChanged?.Invoke(this);
            RevivalChanged?.Invoke();
        }

        /// <summary>
        /// 공격과 이동을 중단하고 무료 부활 대기를 시작합니다.
        /// </summary>
        protected override void OnDied()
        {
            Attack.Cancel();
            revivalTimer.SetDuration(RevivalSeconds);
            revivalTimer.Start();
            displayedRevivalSeconds = Mathf.CeilToInt(RevivalRemaining);
            Movement.Stop();
            AvailabilityChanged?.Invoke(this);
            RevivalChanged?.Invoke();
        }

        /// <summary>
        /// 이동 시작 시 공격을 취소하고 교전·이동 상태의 변화를 알립니다.
        /// </summary>
        private void OnMovementChanged()
        {
            if (Movement.IsMoving)
            {
                Attack.Cancel();
            }

            AvailabilityChanged?.Invoke(this);
            NotifyMovingChanged();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Movement != null)
            {
                Movement.MovementChanged -= OnMovementChanged;
            }
        }

        #endregion // 함수
    }
}
