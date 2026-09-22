using System;
using UnityEngine;

using SW.Base;
using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Units
{
    /// <summary>
    /// 구매한 아군 한 명의 수명과 명령을 관리합니다. 같은 클래스도 서로 독립된 상태를 가집니다.
    /// </summary>
    [RequireComponent(typeof(CharacterMovement))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Units", "ProjectT.Runtime", "AllyUnit")]
    public sealed class CharacterUnit : SWMonoBehaviour
    {
        #region 필드
        private WalkableBattlefield battlefield;
        private Vector2 requestedDestination;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 개체가 사용하는 클래스입니다.
        /// </summary>
        public UnitClassData Definition { get; private set; }

        /// <summary>
        /// 현재 생명과 체력입니다.
        /// </summary>
        public CombatHealth Health { get; private set; }

        /// <summary>
        /// 이동 명령과 현재 이동 상태입니다.
        /// </summary>
        public CharacterMovement Movement { get; private set; }

        /// <summary>
        /// 부활까지 남은 게임 시간입니다.
        /// </summary>
        public float RevivalRemaining { get; private set; }

        /// <summary>
        /// 생존하고 목적지에 도착한 아군만 교전할 수 있습니다.
        /// </summary>
        public bool CanFight => Health != null && Health.IsAlive && !Movement.IsMoving;

        /// <summary>
        /// 이 개체의 마지막 이동 목적지입니다.
        /// </summary>
        public Vector2 RequestedDestination => requestedDestination;

        /// <summary>
        /// 이 아군의 공격 동작과 타격 시각입니다.
        /// </summary>
        public UnitAttackSequence Attack { get; } = new UnitAttackSequence();

        /// <summary>
        /// 현재 준비하거나 진행 중인 공격의 적 대상입니다.
        /// </summary>
        internal EnemyUnit AttackTarget { get; set; }

        /// <summary>
        /// 저지 해제 등 외부 교전 상태를 즉시 갱신하는 알림입니다.
        /// </summary>
        public event Action<CharacterUnit> AvailabilityChanged;

        #endregion // 프로퍼티

        #region 함수
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

            CombatHealth nextHealth = CombatHealth.Create(definition.MaximumHealth);
            var nextMovement = GetComponent<CharacterMovement>();
            if (nextHealth == null || nextMovement == null || !nextMovement.Initialize(terrain, spawn, definition.MoveSpeed))
            {
                return false;
            }

            if (Health != null)
            {
                Health.Died -= OnDied;
            }

            if (Movement != null)
            {
                Movement.MovementChanged -= OnMovementChanged;
            }

            Movement = nextMovement;
            Definition = definition;
            battlefield = terrain;
            requestedDestination = spawn;
            Health = nextHealth;
            Health.Died += OnDied;
            Movement.MovementChanged += OnMovementChanged;
            RevivalRemaining = 0f;
            Attack.Reset();
            AttackTarget = null;
            return true;
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

            requestedDestination = destination;
            return true;
        }

        /// <summary>
        /// 게임 시간으로 부활을 진행합니다. 팝업이 전투를 정지하면 부활도 정지합니다.
        /// </summary>
        public void AdvanceRevival(float elapsedSeconds)
        {
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                SWLog.LogWarning("[CharacterUnit] 부활 진행 실패: 경과 시간은 0 이상의 유한한 수여야 합니다.");
                return;
            }

            if (Health == null || Health.IsAlive || elapsedSeconds == 0f)
            {
                return;
            }

            RevivalRemaining = Mathf.Max(0f, RevivalRemaining - elapsedSeconds);
            if (RevivalRemaining > 0f)
            {
                return;
            }

            if (!Movement.Initialize(battlefield, transform.position, Definition.MoveSpeed))
            {
                return;
            }

            Health.Revive();
            Movement.TryMove(requestedDestination);
            Attack.Reset();
            AttackTarget = null;
            AvailabilityChanged?.Invoke(this);
        }

        /// <summary>
        /// 현재 프레임의 게임 시간만큼 부활 대기를 진행합니다.
        /// </summary>
        private void Update()
        {
            AdvanceRevival(Time.deltaTime);
        }

        /// <summary>
        /// 공격과 이동을 중단하고 무료 부활 대기를 시작합니다.
        /// </summary>
        private void OnDied()
        {
            Attack.Cancel();
            RevivalRemaining = Definition.RevivalSeconds;
            Movement.Stop();
            AvailabilityChanged?.Invoke(this);
        }

        /// <summary>
        /// 이동 시작 시 공격을 취소하고 저지 가능 상태의 변화를 알립니다.
        /// </summary>
        private void OnMovementChanged()
        {
            if (Movement.IsMoving)
            {
                Attack.Cancel();
            }

            AvailabilityChanged?.Invoke(this);
        }

        /// <summary>
        /// 체력과 이동 컴포넌트의 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.Died -= OnDied;
            }

            if (Movement != null)
            {
                Movement.MovementChanged -= OnMovementChanged;
            }
        }

        #endregion // 함수
    }
}
