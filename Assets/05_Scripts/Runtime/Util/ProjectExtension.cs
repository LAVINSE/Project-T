using UnityEngine;

using SW.Util;
using SW.Stat;

namespace ProjectT
{
    /// <summary>
    /// 수치 검사·선 표시·재화 문자열 등 여러 기능이 공유하는 확장 메서드 모음입니다.
    /// </summary>
    public static class ProjectExtension
    {
        #region 스탯
        /// <summary>
        /// SWStat의 개별 설정값 또는 기본값을 읽습니다. 참조가 없으면 0이며 원본 스탯을 변경하지 않습니다.
        /// </summary>
        public static float ExGetConfiguredValue(this SWStatOverride setting)
        {
            return setting?.Stat == null ? 0f : setting.IsUseOverride ? setting.OverrideDefaultValue : setting.Stat.DefaultValue;
        }

        #endregion // 스탯

        #region 화면 위치
        /// <summary>
        /// 포인터 기준 사각형을 부모 영역 안으로 배치합니다. 부모 또는 좌표 변환이 없으면 위치를 유지합니다.
        /// </summary>
        public static void ExPlaceInside(this RectTransform target, Vector2 screenPoint, Camera eventCamera, Vector2 offset)
        {
            RectTransform parent = target.parent as RectTransform;
            if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, eventCamera, out Vector2 point))
            {
                return;
            }

            Vector2 size = target.rect.size;
            point += offset;
            point.x = Mathf.Clamp(point.x, parent.rect.xMin + size.x * target.pivot.x, parent.rect.xMax - size.x * (1f - target.pivot.x));
            point.y = Mathf.Clamp(point.y, parent.rect.yMin + size.y * target.pivot.y, parent.rect.yMax - size.y * (1f - target.pivot.y));
            target.localPosition = new Vector3(point.x, point.y, 0f);
        }

        #endregion // 화면 위치

        #region 아이템 수량
        /// <summary>
        /// 보상 수량이 정수 정밀도를 잃지 않는 0 이상의 아이템 개수인지 확인합니다.
        /// </summary>
        public static bool ExIsItemCount(this double amount)
        {
            return amount.ExIsNonNegative()
                && amount <= ProjectDefine.Inventory.MaximumRewardCount
                && System.Math.Truncate(amount) == amount;
        }

        #endregion // 아이템 수량

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
        /// 현재 경험치와 다음 레벨 기준을 표시합니다. 기준이 없으면 현재 값과 미설정을 함께 표시합니다.
        /// </summary>
        public static string ExToExperienceText(this double current, double required)
        {
            if (!current.ExIsNonNegative() || !required.ExIsNonNegative())
            {
                return "미설정";
            }

            return current.ToString("N0") + " / " + (required > 0d ? required.ToString("N0") : "미설정");
        }

        /// <summary>
        /// 경험치 막대의 비율을 계산합니다. 잘못된 값이나 기준 미설정이면 0입니다.
        /// </summary>
        public static float ExToExperienceFraction(this double current, double required)
        {
            if (!current.ExIsNonNegative() || !required.ExIsFinite() || required <= 0d)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(current / required));
        }

        /// <summary>
        /// 전투 단계를 사용자 문구로 바꿉니다. 알 수 없는 값은 빈 문구입니다.
        /// </summary>
        public static string ExToPhaseText(this BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Preparation:
                    return "전투 준비";
                case BattlePhase.Fighting:
                    return "전투 진행 중";
                case BattlePhase.RoundBreak:
                    return "라운드 휴식";
                case BattlePhase.FinalRest:
                    return "최종 휴식";
                case BattlePhase.Victory:
                    return "승리";
                case BattlePhase.Defeat:
                    return "패배";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 실제 경과 초를 누적 분과 초로 표시합니다. 잘못된 시간은 대시입니다.
        /// </summary>
        public static string ExToElapsedTimeText(this double seconds)
        {
            if (!seconds.ExIsNonNegative())
            {
                return "-";
            }

            double totalSeconds = System.Math.Floor(seconds);
            return System.Math.Floor(totalSeconds / 60d).ToString("00") + ":" + (totalSeconds % 60d).ToString("00");
        }

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
