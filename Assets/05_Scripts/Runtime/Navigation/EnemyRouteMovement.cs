using System;
using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 게임 시간과 이동속도로 고정 경로를 이동하고 최초 도착을 전달합니다. 피해와 보상은 처리하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Navigation", "ProjectT.Defense.Runtime", "EnemyRouteMovement")]
    public sealed class EnemyRouteMovement : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("현재 이동 상태")]
        [SerializeField, SWReadOnly] private float moveSpeed;
        [SerializeField, SWReadOnly] private bool stopped;
        private RouteProgress progress;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 적용 중인 초당 이동 거리입니다.
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// 저지 등 외부 규칙에 의해 이동이 중단되었는지 반환합니다.
        /// </summary>
        public bool IsStopped => stopped;

        /// <summary>
        /// 초기화된 개체가 경로 끝에 도착했는지 반환합니다.
        /// </summary>
        public bool HasArrived => progress != null && progress.HasArrived;

        /// <summary>
        /// 미초기화 개체는 0, 초기화된 개체는 출구까지 남은 경로 거리를 반환합니다.
        /// </summary>
        public float RemainingDistance => progress != null ? progress.RemainingDistance : 0f;

        /// <summary>
        /// 초기화한 한 번의 이동에서 출구 도착을 한 번만 알립니다.
        /// </summary>
        public event Action<EnemyRouteMovement> Arrived;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 경로와 속도를 검증한 뒤 새 이동을 출발점에서 시작합니다. 기존 도착 상태와 중단 상태를 제거합니다.
        /// </summary>
        public bool Initialize(FixedRoute route, float speed)
        {
            if (!ValidateNonNegative(speed, nameof(speed)))
            {
                return false;
            }

            RouteProgress nextProgress = RouteProgress.Create(route);
            if (nextProgress == null)
            {
                return false;
            }

            progress = nextProgress;
            moveSpeed = speed;
            stopped = false;
            ApplyPosition();
            return true;
        }

        /// <summary>
        /// 저지나 해제 때 경로와 진행 거리를 유지한 채 이동만 중단하거나 재개합니다.
        /// </summary>
        public void SetStopped(bool value)
        {
            stopped = value;
        }

        /// <summary>
        /// 현재 위치와 진행 거리를 유지하면서 초당 이동 거리를 변경합니다.
        /// </summary>
        public void SetMoveSpeed(float value)
        {
            if (!ValidateNonNegative(value, nameof(value)))
            {
                return;
            }

            moveSpeed = value;
        }

        /// <summary>
        /// 전달받은 게임 시간만 진행합니다. 0초 또는 중단 상태에서는 위치와 도착 알림을 유지합니다.
        /// </summary>
        public void Advance(float elapsedSeconds)
        {
            if (!ValidateNonNegative(elapsedSeconds, nameof(elapsedSeconds)))
            {
                return;
            }

            if (progress == null || stopped || progress.HasArrived || elapsedSeconds == 0f)
            {
                return;
            }

            // 곱셈이 넘치지 않도록 남은 이동 시간과 먼저 비교합니다.
            float distance = moveSpeed > 0f && elapsedSeconds >= progress.RemainingDistance / moveSpeed
                ? progress.RemainingDistance
                : moveSpeed * elapsedSeconds;
            bool arrivedNow = progress.Advance(distance);
            ApplyPosition();
            if (arrivedNow)
            {
                Arrived?.Invoke(this);
            }
        }

        /// <summary>
        /// 현재 프레임의 게임 시간만큼 적 이동을 진행합니다.
        /// </summary>
        private void Update()
        {
            Advance(Time.deltaTime);
        }

        /// <summary>
        /// 고정 경로의 현재 위치를 적 객체에 반영합니다.
        /// </summary>
        private void ApplyPosition()
        {
            Vector2 position = progress.Position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        /// <summary>
        /// 이동 수치가 0 이상의 유한한 값인지 확인하고 실패를 기록합니다.
        /// </summary>
        private static bool ValidateNonNegative(float value, string parameterName)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
            {
                SWLog.LogWarning($"[EnemyRouteMovement] 요청 거부: {parameterName} 값은 0 이상의 유한한 수여야 합니다.");
                return false;
            }

            return true;
        }

        #endregion // 함수
    }
}
