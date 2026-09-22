using System;

using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Units
{
    /// <summary>
    /// 적 한 명의 경로 이동·공방 도착·저지·처치를 관리합니다. 도착만으로 적을 제거하지 않습니다.
    /// </summary>
    public sealed class EnemyUnit : UnitBase
    {
        #region 필드
        private bool resolved;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 적의 수치 정의입니다.
        /// </summary>
        public UnitEnemyData Definition { get; private set; }

        /// <inheritdoc/>
        public override UnitData Data => Definition;

        /// <summary>
        /// 고정 경로 이동입니다.
        /// </summary>
        public EnemyMovement Movement { get; private set; }

        /// <summary>
        /// 현재 이 적을 저지하는 아군입니다.
        /// </summary>
        public CharacterUnit Blocker { get; private set; }

        /// <summary>
        /// 현재 준비하거나 진행 중인 공격의 아군 대상입니다.
        /// </summary>
        internal CharacterUnit AttackTarget { get; set; }

        /// <summary>
        /// 아직 처치로 정산하지 않은 생존 상태입니다. 공방 도착 이후에도 유지됩니다.
        /// </summary>
        public bool IsActive => !resolved && Health != null && Health.IsAlive;

        /// <summary>
        /// 고정 경로를 끝까지 이동하여 공방을 공격할 수 있는 상태입니다.
        /// </summary>
        public bool HasReachedWorkshop => Movement != null && Movement.HasArrived;

        /// <inheritdoc/>
        public override bool IsMoving => IsActive && !Movement.IsStopped && !Movement.HasArrived;

        /// <summary>
        /// 처치 정산을 한 생명마다 한 번만 요청합니다.
        /// </summary>
        public event Action<EnemyUnit> Resolved;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 풀에서 대여한 적을 새로운 생명과 경로 시작점으로 초기화합니다.
        /// </summary>
        public bool Initialize(UnitEnemyData definition, FixedRoute route)
        {
            if (definition == null || !definition.IsValid || route == null)
            {
                SWLog.LogWarning("[EnemyUnit] 초기화 실패: 적 정의 또는 경로가 없습니다.");
                return false;
            }

            Health nextHealth = Health.Create(definition.MaximumHealth);
            EnemyMovement nextMovement = EnemyMovement.Create(transform, route, definition.MoveSpeed);
            if (nextHealth == null || nextMovement == null)
            {
                return false;
            }

            if (Movement != null)
            {
                Movement.Arrived -= OnArrived;
            }

            Definition = definition;
            Blocker = null;
            AttackTarget = null;
            resolved = false;
            Movement = nextMovement;
            Movement.Arrived += OnArrived;
            SetHealth(nextHealth);
            NotifyInitialized();
            NotifyMovingChanged();
            return true;
        }

        #endregion // 초기화

        #region 함수
        /// <inheritdoc/>
        public override void Tick(float deltaTime)
        {
            if (IsActive && Movement.Advance(deltaTime))
            {
                NotifyMoved();
            }
        }

        /// <summary>
        /// 근접 저지 연결을 적용하거나 해제합니다.
        /// </summary>
        public void SetBlocker(CharacterUnit ally)
        {
            if (Blocker == ally)
            {
                return;
            }

            Attack.Cancel();
            AttackTarget = null;
            Blocker = ally;
            Movement.SetStopped(ally != null);
            NotifyMovingChanged();
        }

        /// <summary>
        /// 경로 끝 도착을 공방 공격 가능 상태로 전환합니다.
        /// </summary>
        private void OnArrived()
        {
            Attack.Cancel();
            AttackTarget = null;
            NotifyMovingChanged();
        }

        /// <summary>
        /// 공격과 이동을 멈추고 처치 완료를 한 번 알립니다.
        /// </summary>
        protected override void OnDied()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            SetBlocker(null);
            Attack.Cancel();
            Movement.SetStopped(true);
            NotifyMovingChanged();
            Resolved?.Invoke(this);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Movement != null)
            {
                Movement.Arrived -= OnArrived;
            }
        }

        #endregion // 함수
    }
}
