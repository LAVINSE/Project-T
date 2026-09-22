using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

using ProjectT.Navigation;

namespace ProjectT.Data
{
    /// <summary>
    /// 적의 고정 경로를 편집하며, 실행 시 원본과 분리된 경로를 제공합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyRouteData", menuName = "Project T/데이터/적 고정 경로")]
    public sealed class EnemyRouteData : ProjectData
    {
        #region 필드
        [SWGroup("적 고정 경로")]
        [SerializeField, Tooltip("입구에서 출구까지의 월드 좌표를 순서대로 지정합니다.")] private Vector2[] points = Array.Empty<Vector2>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 입구부터 공방까지의 경유점입니다.
        /// </summary>
        public IReadOnlyList<Vector2> Points => points;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 현재 정의를 복사한 실행 경로를 만듭니다. 잘못된 경로이면 null입니다.
        /// </summary>
        public FixedRoute CreateRoute()
        {
            return FixedRoute.Create(points);
        }

        /// <summary>
        /// 경유점 수·좌표·인접 점 구분을 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = Check(points.Length >= 2, nameof(points), "경유점은 두 개 이상 필요합니다.", issues);
            for (int index = 0; index < points.Length; index++)
            {
                string path = nameof(points) + ".Array.data[" + index + "]";
                valid &= Check(points[index].ExIsFinite(), path, "좌표는 유한한 수여야 합니다.", issues);
                if (index > 0)
                {
                    valid &= Check(points[index] != points[index - 1], path, "인접 점을 구분해 주세요.", issues);
                }
            }

            return valid;
        }

        #endregion // 함수
    }
}
