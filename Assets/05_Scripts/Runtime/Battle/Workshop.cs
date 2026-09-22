using UnityEngine;

using SW.Util;

using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 한 전투의 공방 체력과 공격 위치입니다. 화면 표시와 승패 확정은 외부 모듈이 담당합니다.
    /// </summary>
    public sealed class Workshop
    {
        #region 프로퍼티
        /// <summary>
        /// 새 출전마다 독립적으로 생성되는 공방 체력입니다.
        /// </summary>
        public Health Health { get; }

        /// <summary>
        /// 적이 바라보고 타격 효과를 표시할 공방의 월드 위치입니다.
        /// </summary>
        public Vector2 Position { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 체력과 타격 위치를 보관합니다.
        /// </summary>
        private Workshop(Health health, Vector2 position)
        {
            Health = health;
            Position = position;
        }

        /// <summary>
        /// 공방 체력과 좌표를 검증하고 실패 시 null을 반환합니다.
        /// </summary>
        public static Workshop Create(float maximumHealth, Vector2 position)
        {
            if (!position.ExIsFinite())
            {
                SWLog.LogWarning("[Workshop] 생성 실패: 공방 좌표는 유한한 수여야 합니다.");
                return null;
            }

            Health health = Health.Create(maximumHealth);
            return health != null ? new Workshop(health, position) : null;
        }

        #endregion // 초기화
    }
}
