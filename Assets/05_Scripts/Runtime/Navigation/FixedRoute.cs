using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 제작자가 정한 경유점을 복사하여 변경되지 않는 적 이동 경로를 구성합니다.
    /// </summary>
    public sealed class FixedRoute
    {
        #region 필드
        private readonly Vector2[] points;
        private readonly float[] accumulatedDistances;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 모든 경유점을 따라 이동하는 실제 경로 길이입니다.
        /// </summary>
        public float Length => accumulatedDistances[accumulatedDistances.Length - 1];

        /// <summary>
        /// 경로를 구성하는 경유점 수입니다.
        /// </summary>
        public int PointCount => points.Length;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 생성 함수가 검증하고 복사한 경유점과 누적 거리를 보관합니다.
        /// </summary>
        private FixedRoute(Vector2[] routePoints, float[] distances)
        {
            points = routePoints;
            accumulatedDistances = distances;
        }

        /// <summary>
        /// 경로를 검증하여 생성합니다. 잘못된 입력은 경고를 남기고 null을 반환합니다.
        /// </summary>
        public static FixedRoute Create(IReadOnlyList<Vector2> sourcePoints)
        {
            if (sourcePoints == null || sourcePoints.Count < 2)
            {
                SWLog.LogWarning("[FixedRoute] 생성 실패: 경로에는 경유점이 두 개 이상 필요합니다.");
                return null;
            }

            var points = new Vector2[sourcePoints.Count];
            var accumulatedDistances = new float[sourcePoints.Count];
            for (int index = 0; index < points.Length; index++)
            {
                Vector2 point = sourcePoints[index];
                if (!IsFinite(point.x) || !IsFinite(point.y))
                {
                    SWLog.LogWarning("[FixedRoute] 생성 실패: 경유점 좌표는 유한한 수여야 합니다.");
                    return null;
                }

                points[index] = point;
                if (index == 0)
                {
                    continue;
                }

                float segmentLength = Vector2.Distance(points[index - 1], point);
                float totalLength = accumulatedDistances[index - 1] + segmentLength;
                if (!IsFinite(totalLength) || segmentLength <= 0f || totalLength <= accumulatedDistances[index - 1])
                {
                    SWLog.LogWarning("[FixedRoute] 생성 실패: 인접 경유점은 구분되어야 하며 경로 길이를 계산할 수 있어야 합니다.");
                    return null;
                }

                accumulatedDistances[index] = totalLength;
            }

            return new FixedRoute(points, accumulatedDistances);
        }

        /// <summary>
        /// 지정 순서의 경유점을 값으로 반환합니다.
        /// </summary>
        public Vector2 GetPoint(int index)
        {
            return points[index];
        }

        /// <summary>
        /// 출발점에서 경로를 따라 이동한 거리의 위치입니다. 경로 밖 거리는 양 끝점으로 제한합니다.
        /// </summary>
        public Vector2 GetPosition(float distance)
        {
            if (!IsFinite(distance))
            {
                SWLog.LogWarning("[FixedRoute] 위치 조회 실패: 이동 거리는 유한한 수여야 합니다.");
                return points[0];
            }

            if (distance <= 0f)
            {
                return points[0];
            }

            if (distance >= Length)
            {
                return points[points.Length - 1];
            }

            int lower = 1;
            int upper = accumulatedDistances.Length - 1;
            while (lower < upper)
            {
                int middle = lower + (upper - lower) / 2;
                if (accumulatedDistances[middle] < distance)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle;
                }
            }

            float startDistance = accumulatedDistances[lower - 1];
            float fraction = (distance - startDistance) / (accumulatedDistances[lower] - startDistance);
            return Vector2.Lerp(points[lower - 1], points[lower], fraction);
        }

        /// <summary>
        /// 경로 좌표와 거리가 유한한 값인지 확인합니다.
        /// </summary>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion // 함수
    }
}
