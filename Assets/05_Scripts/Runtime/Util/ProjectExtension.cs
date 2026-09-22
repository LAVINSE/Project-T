using UnityEngine;

using SW.Util;

namespace ProjectT
{
    /// <summary>
    /// 수치 검사·선 표시·재화 문자열 등 여러 기능이 공유하는 확장 메서드 모음입니다.
    /// </summary>
    public static class ProjectExtension
    {
        #region 수치
        /// <summary>
        /// NaN과 무한대가 아닌 값인지 확인합니다.
        /// </summary>
        public static bool ExIsFinite(this float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// NaN과 무한대가 아닌 값인지 확인합니다.
        /// </summary>
        public static bool ExIsFinite(this double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>
        /// 두 좌표가 모두 유한한 값인지 확인합니다.
        /// </summary>
        public static bool ExIsFinite(this Vector2 value)
        {
            return value.x.ExIsFinite() && value.y.ExIsFinite();
        }

        /// <summary>
        /// 0보다 큰 유한한 값인지 확인합니다.
        /// </summary>
        public static bool ExIsPositive(this float value)
        {
            return value.ExIsFinite() && value > 0f;
        }

        /// <summary>
        /// 0 이상의 유한한 값인지 확인합니다.
        /// </summary>
        public static bool ExIsNonNegative(this float value)
        {
            return value.ExIsFinite() && value >= 0f;
        }

        /// <summary>
        /// 0 이상의 유한한 값인지 확인합니다.
        /// </summary>
        public static bool ExIsNonNegative(this double value)
        {
            return value.ExIsFinite() && value >= 0d;
        }

        #endregion // 수치

        #region 표시
        /// <summary>
        /// 지면 높이가 낮을수록 앞에 그려지는 정렬 순서를 반환합니다.
        /// </summary>
        public static int ExToSortingOrder(this float groundHeight)
        {
            int limit = ProjectDefine.Battle.SortingOrderLimit;
            return Mathf.Clamp(Mathf.RoundToInt(-groundHeight * 100f), -limit, limit);
        }

        /// <summary>
        /// 선 렌더러의 모든 점을 월드 좌표 원 위에 배치합니다.
        /// </summary>
        public static void ExDrawCircle(this LineRenderer line, Vector2 center, float radius)
        {
            line.ExDrawEllipse(center, new Vector2(radius, radius));
        }

        /// <summary>
        /// 선 렌더러의 모든 점을 월드 좌표 타원 위에 배치합니다.
        /// </summary>
        public static void ExDrawEllipse(this LineRenderer line, Vector2 center, Vector2 radius)
        {
            for (int index = 0; index < line.positionCount; index++)
            {
                float angle = index * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(index, center + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y));
            }
        }

        /// <summary>
        /// 양수 잔액만 SWUtils 형식으로 표시하며 미연결·빈 잔액은 대시를 반환합니다.
        /// </summary>
        public static string ExToCurrencyText(this double? balance)
        {
            if (!balance.HasValue || !balance.Value.ExIsFinite() || balance.Value <= 0d)
            {
                return "-";
            }

            return SWAmountFormat.Format(balance.Value, null);
        }

        #endregion // 표시
    }
}
