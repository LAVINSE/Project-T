using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Util;

namespace ProjectT.Navigation
{
    /// <summary>
    /// 맵의 통행 영역에서 아군 경로를 찾습니다. 목적지는 격자에 맞추지 않고 클릭한 좌표를 유지합니다.
    /// </summary>
    public sealed class WalkableBattlefield : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Rect bounds;
        [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
        [SerializeField] private Rect[] obstacles = Array.Empty<Rect>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 맵의 통행 범위입니다.
        /// </summary>
        public Rect Bounds => bounds;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 편집기에서 정의한 맵 범위와 장애물을 적용합니다.
        /// </summary>
        public bool Configure(Rect area, float resolution, Rect[] blockedAreas)
        {
            if (!area.position.ExIsFinite()
                || !area.width.ExIsPositive()
                || !area.height.ExIsPositive()
                || !resolution.ExIsFinite()
                || resolution < 0.1f
                || Math.Ceiling(area.width / resolution) * Math.Ceiling(area.height / resolution) > 20000)
            {
                SWLog.LogWarning("[WalkableBattlefield] 설정 실패: 통행 범위와 탐색 해상도가 올바르지 않습니다.");
                return false;
            }

            bounds = area;
            cellSize = resolution;
            obstacles = blockedAreas != null ? (Rect[])blockedAreas.Clone() : Array.Empty<Rect>();
            return true;
        }

        /// <summary>
        /// 맵 안이며 장애물이 점유하지 않은 좌표인지 확인합니다.
        /// </summary>
        public bool IsWalkable(Vector2 point)
        {
            if (!point.ExIsFinite() || !bounds.Contains(point))
            {
                return false;
            }

            foreach (Rect obstacle in obstacles)
            {
                if (obstacle.Contains(point))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 장애물을 가로지르지 않는 경유점을 반환합니다. 실패하면 기존 이동 명령은 변경하지 않습니다.
        /// </summary>
        public bool TryFindPath(Vector2 start, Vector2 destination, out Vector2[] points)
        {
            points = null;
            if (!IsWalkable(start) || !IsWalkable(destination))
            {
                return false;
            }

            if (ClearSegment(start, destination))
            {
                points = start == destination ? new[] { start } : new[] { start, destination };
                return true;
            }

            int width = Mathf.CeilToInt(bounds.width / cellSize);
            int height = Mathf.CeilToInt(bounds.height / cellSize);
            int count = width * height;
            int startIndex = Index(start, width);
            int destinationIndex = Index(destination, width);
            if (startIndex == destinationIndex)
            {
                return false;
            }

            int[] previous = new int[count];
            float[] cost = new float[count];
            bool[] closed = new bool[count];
            for (int index = 0; index < count; index++)
            {
                previous[index] = -1;
                cost[index] = float.PositiveInfinity;
            }

            var open = new List<int>
            {
                startIndex
            };
            cost[startIndex] = 0f;
            while (open.Count > 0)
            {
                int bestPosition = 0;
                float bestScore = float.PositiveInfinity;
                for (int index = 0; index < open.Count; index++)
                {
                    int candidate = open[index];
                    float score = cost[candidate] + Vector2.Distance(Position(candidate), destination);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPosition = index;
                    }
                }

                int current = open[bestPosition];
                open.RemoveAt(bestPosition);
                if (current == destinationIndex)
                {
                    var result = new List<Vector2>
                    {
                        destination
                    };
                    for (int index = previous[current]; index >= 0; index = previous[index])
                    {
                        result.Add(Position(index));
                    }

                    result.Reverse();
                    points = Simplify(result);
                    return true;
                }

                closed[current] = true;
                int column = current % width;
                int row = current / width;
                for (int vertical = -1; vertical <= 1; vertical++)
                {
                    for (int horizontal = -1; horizontal <= 1; horizontal++)
                    {
                        if (horizontal == 0 && vertical == 0)
                        {
                            continue;
                        }

                        int nextColumn = column + horizontal;
                        int nextRow = row + vertical;
                        if (nextColumn < 0 || nextRow < 0 || nextColumn >= width || nextRow >= height)
                        {
                            continue;
                        }

                        int next = nextRow * width + nextColumn;
                        if (closed[next] || !ClearSegment(Position(current), Position(next)))
                        {
                            continue;
                        }

                        float nextCost = cost[current] + Vector2.Distance(Position(current), Position(next));
                        if (nextCost >= cost[next])
                        {
                            continue;
                        }

                        cost[next] = nextCost;
                        previous[next] = current;
                        if (!open.Contains(next))
                        {
                            open.Add(next);
                        }
                    }
                }
            }

            return false;
            Vector2 Position(int index)
            {
                return index == startIndex
                    ? start
                    : index == destinationIndex
                        ? destination
                        : bounds.min + new Vector2(index % width + 0.5f, index / width + 0.5f) * cellSize;
            }
        }

        /// <summary>
        /// 직선 이동 구간과 장애물 사각형의 교차를 정확히 검사합니다.
        /// </summary>
        private bool ClearSegment(Vector2 start, Vector2 end)
        {
            if (!IsWalkable(start) || !IsWalkable(end))
            {
                return false;
            }

            foreach (Rect obstacle in obstacles)
            {
                float minimum = 0f;
                float maximum = 1f;
                Vector2 direction = end - start;
                if (Clip(start.x, direction.x, obstacle.xMin, obstacle.xMax, ref minimum, ref maximum)
                    && Clip(start.y, direction.y, obstacle.yMin, obstacle.yMax, ref minimum, ref maximum))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 한 축에서 선분과 장애물이 겹치는 구간을 계산합니다.
        /// </summary>
        private static bool Clip(
            float origin,
            float direction,
            float lower,
            float upper,
            ref float minimum,
            ref float maximum)
        {
            if (Mathf.Abs(direction) < 0.000001f)
            {
                return origin >= lower && origin <= upper;
            }

            float first = (lower - origin) / direction;
            float second = (upper - origin) / direction;
            minimum = Mathf.Max(minimum, Mathf.Min(first, second));
            maximum = Mathf.Min(maximum, Mathf.Max(first, second));
            return minimum <= maximum;
        }

        /// <summary>
        /// 장애물을 통과하지 않는 직선 구간으로 이동 경유점을 줄입니다.
        /// </summary>
        private Vector2[] Simplify(List<Vector2> path)
        {
            var result = new List<Vector2>
            {
                path[0]
            };
            int current = 0;
            while (current < path.Count - 1)
            {
                int next = path.Count - 1;
                while (next > current + 1 && !ClearSegment(path[current], path[next]))
                {
                    next--;
                }

                result.Add(path[next]);
                current = next;
            }

            return result.ToArray();
        }

        /// <summary>
        /// 월드 위치를 경로 탐색 격자의 일차원 인덱스로 변환합니다.
        /// </summary>
        private int Index(Vector2 point, int width)
        {
            return Mathf.FloorToInt((point.y - bounds.yMin) / cellSize) * width + Mathf.FloorToInt((point.x - bounds.xMin) / cellSize);
        }

        #endregion // 함수
    }
}
