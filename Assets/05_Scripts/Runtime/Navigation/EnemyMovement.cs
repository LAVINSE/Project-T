using System;
using UnityEngine;

using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 게임 시간과 이동속도로 고정 경로를 이동하고 최초 도착을 전달합니다. 피해와 보상은 처리하지 않습니다.
    /// </summary>
    public sealed class EnemyMovement
    {
        #region 필드
        private readonly Transform target;
        private readonly RouteProgress progress;
        private float moveSpeed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 저지 등 외부 규칙에 의해 이동이 중단되었는지 반환합니다.
        /// </summary>
        public bool IsStopped { get; private set; }

        /// <summary>
        /// 경로 끝에 도착했는지 반환합니다.
        /// </summary>
        public bool HasArrived => progress.HasArrived;

        /// <summary>
        /// 출구까지 남은 경로 거리입니다.
        /// </summary>
        public float RemainingDistance => progress.RemainingDistance;

        /// <summary>
        /// 경로 끝 도착을 한 번만 알립니다.
        /// </summary>
        public event Action Arrived;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 대상·경로 진행·이동속도를 보관하고 출발점에 배치합니다.
        /// </summary>
        private EnemyMovement(Transform target, RouteProgress progress, float moveSpeed)
        {
            this.target = target;
            this.progress = progress;
            this.moveSpeed = moveSpeed;
            ApplyPosition();
        }

        /// <summary>
        /// 경로와 속도를 검증한 뒤 새 이동을 출발점에서 시작합니다. 잘못된 입력이면 null을 반환합니다.
        /// </summary>
        public static EnemyMovement Create(Transform target, FixedRoute route, float speed)
        {
            if (target == null || !speed.ExIsNonNegative())
            {
                SWLog.LogWarning("[EnemyMovement] 생성 실패: 대상과 0 이상의 이동속도가 필요합니다.");
                return null;
            }

            RouteProgress progress = RouteProgress.Create(route);
            return progress != null ? new EnemyMovement(target, progress, speed) : null;
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 런타임 이동속도를 반영합니다. 음수나 유한하지 않은 값은 기존 속도를 유지합니다.
        /// </summary>
        public bool SetMoveSpeed(float speed)
        {
            if (!speed.ExIsNonNegative())
            {
                SWLog.LogWarning("[Movement] 이동속도 변경 실패: 0 이상 유한한 값이 필요합니다.");
                return false;
            }

            moveSpeed = speed;
            return true;
        }

        /// <summary>
        /// 저지나 해제 때 경로와 진행 거리를 유지한 채 이동만 중단하거나 재개합니다.
        /// </summary>
        public void SetStopped(bool value)
        {
            IsStopped = value;
        }

        /// <summary>
        /// 전달받은 게임 시간만 진행합니다. 위치가 바뀌었으면 참을 반환합니다.
        /// </summary>
        public bool Advance(float deltaTime)
        {
            if (IsStopped || progress.HasArrived || deltaTime <= 0f || moveSpeed <= 0f)
            {
                return false;
            }

            float distance = Mathf.Min(progress.RemainingDistance, moveSpeed * deltaTime);
            bool arrivedNow = progress.Advance(distance);
            ApplyPosition();
            if (arrivedNow)
            {
                Arrived?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 고정 경로의 현재 위치를 대상 객체에 반영합니다.
        /// </summary>
        private void ApplyPosition()
        {
            Vector2 position = progress.Position;
            target.position = new Vector3(position.x, position.y, target.position.z);
        }

        #endregion // 함수
    }
}
