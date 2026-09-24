using UnityEngine;

using SW.Util;

using ProjectT.Units;
using ProjectT.Data;

namespace ProjectT.Battle
{
    /// <summary>
    /// 한 전투의 공방 체력과 공격 위치입니다. 화면 표시와 승패 확정은 외부 모듈이 담당합니다.
    /// </summary>
    public sealed class Workshop : System.IDisposable
    {
        #region 필드
        private readonly StageData definition;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이번 전투에서만 사용하는 공방 스탯 복제본입니다.
        /// </summary>
        public RuntimeStatCollection Stats { get; }

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
        private Workshop(StageData definition, RuntimeStatCollection stats, Health health, Vector2 position)
        {
            this.definition = definition;
            Stats = stats;
            Stats.Changed += OnStatsChanged;
            Health = health;
            Position = position;
        }

        /// <summary>
        /// 공방 체력과 좌표를 검증하고 실패 시 null을 반환합니다.
        /// </summary>
        public static Workshop Create(StageData definition, Vector2 position)
        {
            if (definition == null || !position.ExIsFinite())
            {
                SWLog.LogWarning("[Workshop] 생성 실패: 공방 좌표는 유한한 수여야 합니다.");
                return null;
            }

            RuntimeStatCollection stats = RuntimeStatCollection.Create(definition.GetStatSettings());
            if (stats == null)
            {
                return null;
            }

            Health health = Health.Create(stats.GetValue(definition.WorkshopMaximumHealthStat));
            if (health == null)
            {
                stats.Dispose();
                return null;
            }

            return new Workshop(definition, stats, health, position);
        }

        #endregion // 초기화

        #region 수명
        /// <summary>
        /// 유효한 최대 체력 변경만 현재 공방에 반영합니다.
        /// </summary>
        private void OnStatsChanged()
        {
            float maximum = Stats.GetValue(definition.WorkshopMaximumHealthStat);
            if (maximum.ExIsPositive())
            {
                Health.SetMaximum(maximum);
            }
        }

        /// <summary>
        /// 전투 종료 시 복제본을 해제합니다. 반복 호출도 허용합니다.
        /// </summary>
        public void Dispose()
        {
            Stats.Dispose();
        }

        #endregion // 수명
    }
}
