using System;
using UnityEngine;

using SW.Base;
using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Units
{
    /// <summary>
    /// 적 한 명의 경로 이동·공방 도착·저지·처치를 관리합니다. 도착만으로 적을 제거하지 않습니다.
    /// </summary>
    [RequireComponent(typeof(EnemyRouteMovement))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Units", "ProjectT.Defense.Runtime", "EnemyUnit")]
    public sealed class EnemyUnit : SWMonoBehaviour
    {
        #region 필드
        private bool resolved;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 적의 수치 정의입니다.
        /// </summary>
        public EnemyDefinition Definition { get; private set; }

        /// <summary>
        /// 이 개체의 체력입니다.
        /// </summary>
        public CombatHealth Health { get; private set; }

        /// <summary>
        /// 고정 경로 이동입니다.
        /// </summary>
        public EnemyRouteMovement Movement { get; private set; }

        /// <summary>
        /// 현재 이 적을 저지하는 아군입니다.
        /// </summary>
        public AllyUnit Blocker { get; private set; }

        /// <summary>
        /// 이 적의 공격 동작과 타격 시각입니다.
        /// </summary>
        public UnitAttackSequence Attack { get; } = new UnitAttackSequence();

        /// <summary>
        /// 현재 준비하거나 진행 중인 공격의 아군 대상입니다.
        /// </summary>
        internal AllyUnit AttackTarget { get; set; }

        /// <summary>
        /// 아직 처치로 정산하지 않은 생존 상태입니다. 공방 도착 이후에도 유지됩니다.
        /// </summary>
        public bool IsActive => !resolved && Health != null && Health.IsAlive;

        /// <summary>
        /// 고정 경로를 끝까지 이동하여 공방을 공격할 수 있는 상태입니다.
        /// </summary>
        public bool HasReachedWorkshop => Movement != null && Movement.HasArrived;

        /// <summary>
        /// 처치 정산을 한 생명마다 한 번만 요청합니다.
        /// </summary>
        public event Action<EnemyUnit> Resolved;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 풀에서 대여한 적을 새로운 생명과 경로 시작점으로 초기화합니다.
        /// </summary>
        public bool Initialize(EnemyDefinition definition, FixedRoute route)
        {
            if (definition == null || route == null)
            {
                SWLog.LogWarning("[EnemyUnit] 초기화 실패: 적 정의 또는 경로가 없습니다.");
                return false;
            }

            CombatHealth nextHealth = CombatHealth.Create(definition.MaximumHealth);
            var nextMovement = GetComponent<EnemyRouteMovement>();
            if (nextHealth == null || nextMovement == null || !nextMovement.Initialize(route, definition.MoveSpeed))
            {
                return false;
            }

            if (Health != null)
            {
                Health.Died -= OnDied;
            }

            if (Movement != null)
            {
                Movement.Arrived -= OnArrived;
            }

            Definition = definition;
            Health = nextHealth;
            Movement = nextMovement;
            Health.Died += OnDied;
            Movement.Arrived += OnArrived;
            Blocker = null;
            resolved = false;
            Attack.Reset();
            AttackTarget = null;
            return true;
        }

        /// <summary>
        /// 근접 저지 연결을 적용하거나 해제합니다.
        /// </summary>
        public void SetBlocker(AllyUnit ally)
        {
            if (Blocker != ally)
            {
                Attack.Cancel();
                AttackTarget = null;
            }

            Blocker = ally;
            Movement.SetStopped(ally != null);
        }

        /// <summary>
        /// 경로 끝 도착을 공방 공격 가능 상태로 전환합니다.
        /// </summary>
        private void OnArrived(EnemyRouteMovement movement)
        {
            Attack.Cancel();
            AttackTarget = null;
        }

        /// <summary>
        /// 공격을 취소하고 처치 완료를 한 번 알립니다.
        /// </summary>
        private void OnDied()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            Attack.Cancel();
            SetBlocker(null);
            Resolved?.Invoke(this);
        }

        /// <summary>
        /// 이동 도착과 체력 사망 이벤트의 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.Died -= OnDied;
            }

            if (Movement != null)
            {
                Movement.Arrived -= OnArrived;
            }
        }

        #endregion // 함수
    }
}
