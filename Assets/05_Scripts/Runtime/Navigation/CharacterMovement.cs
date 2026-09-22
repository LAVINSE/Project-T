using System;
using UnityEngine;

using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 아군의 목적지 명령과 이동 상태를 관리합니다. 이동 중에는 공격과 저지를 허용하지 않습니다.
    /// </summary>
    public sealed class CharacterMovement
    {
        #region 필드
        private readonly Transform target;
        private readonly WalkableBattlefield battlefield;
        private readonly float moveSpeed;
        private RouteProgress progress;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 목적지까지 이동 중인지 반환합니다.
        /// </summary>
        public bool IsMoving => progress != null && !progress.HasArrived;

        /// <summary>
        /// 목적지 이동이 시작되거나 끝났을 때 교전 상태를 갱신하는 알림입니다.
        /// </summary>
        public event Action MovementChanged;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 대상·전장·이동속도를 보관합니다.
        /// </summary>
        private CharacterMovement(Transform target, WalkableBattlefield battlefield, float moveSpeed)
        {
            this.target = target;
            this.battlefield = battlefield;
            this.moveSpeed = moveSpeed;
        }

        /// <summary>
        /// 생성 위치와 이동속도를 검증해 이동을 만듭니다. 잘못된 입력이면 null을 반환합니다.
        /// </summary>
        public static CharacterMovement Create(Transform target, WalkableBattlefield battlefield, Vector2 spawnPosition, float speed)
        {
            if (target == null || battlefield == null || !speed.ExIsPositive())
            {
                SWLog.LogWarning("[CharacterMovement] 생성 실패: 대상·전장과 0보다 큰 이동속도가 필요합니다.");
                return null;
            }

            var movement = new CharacterMovement(target, battlefield, speed);
            return movement.Reset(spawnPosition) ? movement : null;
        }

        /// <summary>
        /// 이동을 멈추고 지정 위치에서 다시 시작합니다. 통행 불가 위치이면 기존 상태를 유지합니다.
        /// </summary>
        public bool Reset(Vector2 position)
        {
            if (!battlefield.IsWalkable(position))
            {
                SWLog.LogWarning("[CharacterMovement] 위치 설정 실패: 통행 가능 영역 밖입니다.");
                return false;
            }

            progress = null;
            ApplyPosition(position);
            return true;
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 현재 위치에서 새 목적지로 이동합니다. 경로가 없으면 기존 이동을 유지합니다.
        /// </summary>
        public bool TryMove(Vector2 destination)
        {
            if (!battlefield.TryFindPath(target.position, destination, out Vector2[] points))
            {
                return false;
            }

            RouteProgress nextProgress = null;
            if (points.Length > 1)
            {
                nextProgress = RouteProgress.Create(FixedRoute.Create(points));
                if (nextProgress == null)
                {
                    return false;
                }
            }

            progress = nextProgress;
            MovementChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 사망 등 외부 수명 처리에서 현재 이동만 중단합니다.
        /// </summary>
        public void Stop()
        {
            progress = null;
            MovementChanged?.Invoke();
        }

        /// <summary>
        /// 게임 시간이 흐른 만큼 이동합니다. 위치가 바뀌었으면 참을 반환하고 도착하면 알립니다.
        /// </summary>
        public bool Advance(float deltaTime)
        {
            if (!IsMoving || deltaTime <= 0f)
            {
                return false;
            }

            float distance = Mathf.Min(progress.RemainingDistance, deltaTime * moveSpeed);
            bool arrived = progress.Advance(distance);
            ApplyPosition(progress.Position);
            if (arrived)
            {
                MovementChanged?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 경로 위치를 대상 객체에 반영합니다. 깊이 좌표는 유지합니다.
        /// </summary>
        private void ApplyPosition(Vector2 position)
        {
            target.position = new Vector3(position.x, position.y, target.position.z);
        }

        #endregion // 함수
    }
}
