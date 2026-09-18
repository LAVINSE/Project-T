using System;
using UnityEngine;

using SW.Base;
using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 아군의 목적지 명령과 이동 상태를 관리합니다. 이동 중에는 공격과 저지를 허용하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Navigation", "ProjectT.Defense.Runtime", "AllyDestinationMovement")]
    public sealed class AllyDestinationMovement : SWMonoBehaviour
    {
        #region 필드
        private WalkableBattlefield battlefield;
        private RouteProgress progress;
        private float moveSpeed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 목적지까지 이동 중인지 반환합니다.
        /// </summary>
        public bool IsMoving => progress != null && !progress.HasArrived;

        /// <summary>
        /// 가장 최근에 수락한 목적지입니다. 부활 후에도 유지합니다.
        /// </summary>
        public Vector2 Destination { get; private set; }

        /// <summary>
        /// 목적지 이동이 시작되거나 끝났을 때 교전 상태를 갱신하는 알림입니다.
        /// </summary>
        public event Action MovementChanged;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 생성 위치와 이동속도를 설정합니다.
        /// </summary>
        public bool Initialize(WalkableBattlefield terrain, Vector2 spawnPosition, float speed)
        {
            if (terrain == null || !terrain.IsWalkable(spawnPosition))
            {
                SWLog.LogWarning("[AllyDestinationMovement] 초기화 실패: 전장이 없거나 생성 위치가 통행 가능 영역 밖입니다.");
                return false;
            }

            if (speed <= 0f || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                SWLog.LogWarning("[AllyDestinationMovement] 초기화 실패: 이동속도는 0보다 큰 유한한 수여야 합니다.");
                return false;
            }

            battlefield = terrain;
            moveSpeed = speed;
            progress = null;
            Destination = spawnPosition;
            ApplyPosition(spawnPosition);
            return true;
        }

        /// <summary>
        /// 현재 위치에서 새 목적지로 이동합니다. 경로가 없으면 기존 이동과 목적지를 유지합니다.
        /// </summary>
        public bool TryMove(Vector2 destination)
        {
            if (battlefield == null || !battlefield.TryFindPath(transform.position, destination, out Vector2[] points))
            {
                return false;
            }

            RouteProgress nextProgress = points.Length > 1 ? RouteProgress.Create(FixedRoute.Create(points)) : null;
            if (points.Length > 1 && nextProgress == null)
            {
                return false;
            }

            progress = nextProgress;
            Destination = destination;
            MovementChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 사망 등 외부 수명 처리에서 현재 이동만 중단하고 목적지는 보존합니다.
        /// </summary>
        public void Stop()
        {
            progress = null;
            MovementChanged?.Invoke();
        }

        /// <summary>
        /// 게임 시간이 흐른 만큼 이동하고 도착했을 때 교전을 다시 허용합니다.
        /// </summary>
        public void Advance(float elapsedSeconds)
        {
            if (elapsedSeconds < 0f || float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds))
            {
                SWLog.LogWarning("[AllyDestinationMovement] 이동 실패: 경과 시간은 0 이상의 유한한 수여야 합니다.");
                return;
            }

            if (!IsMoving || elapsedSeconds == 0f)
            {
                return;
            }

            float distance = elapsedSeconds >= progress.RemainingDistance / moveSpeed
                ? progress.RemainingDistance
                : elapsedSeconds * moveSpeed;
            bool arrived = progress.Advance(distance);
            ApplyPosition(progress.Position);
            if (arrived)
            {
                MovementChanged?.Invoke();
            }
        }

        /// <summary>
        /// 현재 프레임의 게임 시간만큼 아군 이동을 진행합니다.
        /// </summary>
        private void Update()
        {
            Advance(Time.deltaTime);
        }

        /// <summary>
        /// 경로 진행 위치를 아군 객체에 반영합니다.
        /// </summary>
        private void ApplyPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        #endregion // 함수
    }
}
