using System;
using UnityEngine;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.Units
{
    /// <summary>
    /// 캐릭터와 적이 공유하는 체력·공격·상태 알림입니다. 표시 컴포넌트는 이 알림만 구독하고 매 프레임 상태를 조회하지 않습니다.
    /// </summary>
    public abstract class UnitBase : SWMonoBehaviour
    {
        #region 프로퍼티
        /// <summary>
        /// 개체별 능력치 복제본입니다. 초기화 전에는 null입니다.
        /// </summary>
        public RuntimeStatCollection Stats { get; private set; }

        /// <summary>
        /// 현재 공격력입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float AttackDamage => Stats?.GetValue(Data.AttackDamageStat) ?? 0f;

        /// <summary>
        /// 현재 초당 공격 횟수입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float AttackSpeed => Stats?.GetValue(Data.AttackSpeedStat) ?? 0f;

        /// <summary>
        /// 현재 공격 간격입니다. 공격 불가 수치이면 0입니다.
        /// </summary>
        public float AttackInterval => AttackSpeed.ExIsPositive() ? 1f / AttackSpeed : 0f;

        /// <summary>
        /// 현재 사거리입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float AttackRange => Stats?.GetValue(Data.AttackRangeStat) ?? 0f;

        /// <summary>
        /// 현재 이동속도입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float MoveSpeed => Stats?.GetValue(Data.MoveSpeedStat) ?? 0f;

        /// <summary>
        /// 현재 방어력입니다. 초기화 전에는 0입니다.
        /// </summary>
        public float Defense => Stats?.GetValue(Data.DefenseStat) ?? 0f;

        /// <summary>
        /// 현재 회피 확률입니다. 1은 100%이며 상한은 전투 계산에서 적용합니다. 초기화 전에는 0입니다.
        /// </summary>
        public float Evasion => Stats?.GetValue(Data.EvasionStat) ?? 0f;

        /// <summary>
        /// 이 개체의 공통 능력치와 표시 정보입니다. 초기화 전에는 null입니다.
        /// </summary>
        public abstract UnitData Data { get; }

        /// <summary>
        /// 현재 생명과 체력입니다. 풀 재사용마다 새로 만듭니다.
        /// </summary>
        public Health Health { get; private set; }

        /// <summary>
        /// 이 개체의 공격 동작과 타격 시각입니다.
        /// </summary>
        public UnitAttack Attack { get; } = new UnitAttack();

        /// <summary>
        /// 걷는 동작을 표시해야 하는 이동 상태인지 반환합니다.
        /// </summary>
        public abstract bool IsMoving { get; }

        /// <summary>
        /// 풀에서 꺼내 새 생명으로 초기화했을 때 발생합니다.
        /// </summary>
        public event Action Initialized;

        /// <summary>
        /// 피해·사망·부활로 체력이 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action HealthChanged;

        /// <summary>
        /// 런타임 능력치가 변경되었을 때 화면과 외부 모듈에 알립니다.
        /// </summary>
        public event Action StatsChanged;

        /// <summary>
        /// 이동으로 위치가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action Moved;

        /// <summary>
        /// 이동 시작·정지·목적지 변경처럼 이동 상태가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action MovingChanged;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 검증이 끝난 복제본으로 교체하고 이전 복제본을 해제합니다.
        /// </summary>
        protected void SetStats(RuntimeStatCollection nextStats)
        {
            Stats?.Dispose();
            Stats = nextStats;
            Stats.Changed += OnStatsChanged;
        }

        /// <summary>
        /// 런타임 최대 체력과 이동속도 변경을 반영합니다.
        /// </summary>
        protected virtual void OnStatsChanged()
        {
            float maximum = Stats.GetValue(Data.MaximumHealthStat);
            if (maximum.ExIsPositive())
            {
                Health?.SetMaximum(maximum);
            }

            StatsChanged?.Invoke();
        }

        /// <summary>
        /// 전투 관리자가 매 프레임 전달하는 게임 시간만큼 이동과 부활을 진행합니다.
        /// </summary>
        public abstract void Tick(float deltaTime);

        /// <summary>
        /// 새 체력으로 교체하고 이전 체력의 알림 구독을 해제합니다.
        /// </summary>
        protected void SetHealth(Health nextHealth)
        {
            if (Health != null)
            {
                Health.Changed -= OnHealthChanged;
                Health.Died -= OnDied;
            }

            Health = nextHealth;
            Health.Changed += OnHealthChanged;
            Health.Died += OnDied;
            Attack.Reset();
        }

        /// <summary>
        /// 체력이 처음 0이 되었을 때 파생 개체의 사망 처리를 수행합니다.
        /// </summary>
        protected abstract void OnDied();

        /// <summary>
        /// 초기화 완료를 표시 컴포넌트에 알립니다.
        /// </summary>
        protected void NotifyInitialized()
        {
            Initialized?.Invoke();
        }

        /// <summary>
        /// 위치 변경을 표시 컴포넌트에 알립니다.
        /// </summary>
        protected void NotifyMoved()
        {
            Moved?.Invoke();
        }

        /// <summary>
        /// 이동 상태 변경을 표시 컴포넌트에 알립니다.
        /// </summary>
        protected void NotifyMovingChanged()
        {
            MovingChanged?.Invoke();
        }

        /// <summary>
        /// 체력 변경을 그대로 전달합니다.
        /// </summary>
        private void OnHealthChanged()
        {
            HealthChanged?.Invoke();
        }

        /// <summary>
        /// 제거될 때 체력 알림 구독을 해제합니다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            Stats?.Dispose();
            if (Health != null)
            {
                Health.Changed -= OnHealthChanged;
                Health.Died -= OnDied;
            }
        }

        #endregion // 함수
    }
}
