using UnityEngine;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 실제 전장 거리와 일치하는 원형 표시의 좌표를 계산합니다.
    /// </summary>
    public static class BattleIndicatorGeometry
    {
        #region 함수
        /// <summary>
        /// 중심과 반지름을 사용해 월드 좌표 선을 갱신합니다.
        /// </summary>
        public static void DrawCircle(LineRenderer line, Vector2 center, float radius)
        {
            for (int index = 0; index < line.positionCount; index++)
            {
                float angle = index * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(index, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        #endregion // 함수
    }
}
