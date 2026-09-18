using UnityEngine;

using SW.Util;

using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 한 전투의 공방 체력과 공격 위치입니다. 화면 표시와 승패 확정은 외부 모듈이 담당합니다.
    /// </summary>
    public sealed class WorkshopObjective
    {
        #region 프로퍼티
        /// <summary>
        /// 새 출전마다 독립적으로 생성되는 공방 체력입니다.
        /// </summary>
        public CombatHealth Health { get; }

        /// <summary>
        /// 적이 바라보고 타격 효과를 표시할 공방의 월드 위치입니다.
        /// </summary>
        public Vector2 Position { get; }

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 공방의 최대 체력과 타격 위치를 지정해 새 방어 목표를 생성합니다.
        /// </summary>
        private WorkshopObjective(CombatHealth health, Vector2 position)
        {
            Health = health;
            Position = position;
        }

        /// <summary>
        /// 공방 체력과 좌표를 검증하고 실패 시 null을 반환합니다.
        /// </summary>
        public static WorkshopObjective Create(float maximumHealth, Vector2 position)
        {
            if (float.IsNaN(position.x)
                || float.IsInfinity(position.x)
                || float.IsNaN(position.y)
                || float.IsInfinity(position.y))
            {
                SWLog.LogWarning("[WorkshopObjective] 생성 실패: 공방 좌표는 유한한 수여야 합니다.");
                return null;
            }

            CombatHealth health = CombatHealth.Create(maximumHealth);
            return health != null ? new WorkshopObjective(health, position) : null;
        }

        #endregion // 함수
    }
}
