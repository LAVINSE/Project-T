using System;
using UnityEngine;

using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 공유 경로를 변경하지 않고 개체 하나의 이동 거리와 도착 상태를 관리합니다.
    /// </summary>
    public sealed class RouteProgress
    {
        #region 필드
        private readonly FixedRoute route;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 출발점에서 실제 경로를 따라 이동한 거리입니다.
        /// </summary>
        public float Distance { get; private set; }

        /// <summary>
        /// 출구까지 남은 실제 경로 길이입니다.
        /// </summary>
        public float RemainingDistance => route.Length - Distance;

        /// <summary>
        /// 이 개체의 현재 경로상 위치입니다.
        /// </summary>
        public Vector2 Position => route.GetPosition(Distance);

        /// <summary>
        /// 경로 끝에 도착했는지 반환합니다.
        /// </summary>
        public bool HasArrived => Distance >= route.Length;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 새 개체의 진행을 경로 출발점에서 시작합니다.
        /// </summary>
        private RouteProgress(FixedRoute route)
        {
            this.route = route;
        }

        /// <summary>
        /// 유효한 경로의 진행 상태를 생성하며 경로가 없으면 null을 반환합니다.
        /// </summary>
        public static RouteProgress Create(FixedRoute route)
        {
            if (route == null)
            {
                SWLog.LogWarning("[RouteProgress] 생성 실패: 경로가 없습니다.");
                return null;
            }

            return new RouteProgress(route);
        }

        /// <summary>
        /// 경로를 따라 전진합니다. 이번 요청에서 처음 도착했을 때만 참을 반환합니다.
        /// </summary>
        public bool Advance(float distance)
        {
            if (distance < 0f || float.IsNaN(distance) || float.IsInfinity(distance))
            {
                SWLog.LogWarning("[RouteProgress] 전진 실패: 거리는 0 이상의 유한한 수여야 합니다.");
                return false;
            }

            if (HasArrived || distance == 0f)
            {
                return false;
            }

            Distance = distance >= RemainingDistance ? route.Length : Distance + distance;
            return HasArrived;
        }

        #endregion // 함수
    }
}
