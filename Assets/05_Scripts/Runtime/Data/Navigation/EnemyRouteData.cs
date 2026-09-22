using System;
using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Data
{
    /// <summary>
    /// 적의 고정 경로를 편집하며, 실행 시 원본과 분리된 경로를 제공합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyRouteData", menuName = "Project T/데이터/적 고정 경로")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Data", "ProjectT.Runtime", "EnemyRouteDefinition")]
    public sealed class EnemyRouteData : SWScriptableObject
    {
        #region 필드
        [SWGroup("적 고정 경로")]
        [SerializeField, Tooltip("입구에서 출구까지의 월드 좌표를 순서대로 지정합니다.")] private Vector2[] points = Array.Empty<Vector2>();

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 현재 정의를 복사합니다. 잘못된 경로이면 실패 이유를 반환하고 실행 경로를 만들지 않습니다.
        /// </summary>
        public bool TryCreateRoute(out FixedRoute route, out string reason)
        {
            route = FixedRoute.Create(points);
            reason = route != null ? string.Empty : "경로 데이터의 경유점과 좌표를 확인해 주세요.";
            return route != null;
        }

        #endregion // 함수
    }
}
