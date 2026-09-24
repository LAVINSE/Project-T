using System;

using SW.Util;

namespace ProjectT.Units
{
    /// <summary>
    /// 피해와 사망을 한 개체 안에서 처리합니다. 보상과 부활 정책은 외부 전투 수명에서 결정합니다.
    /// </summary>
    public sealed class Health
    {
        #region 프로퍼티
        /// <summary>
        /// 최대 체력입니다.
        /// </summary>
        public float Maximum { get; private set; }

        /// <summary>
        /// 현재 체력입니다.
        /// </summary>
        public float Current { get; private set; }

        /// <summary>
        /// 0부터 1까지의 남은 체력 비율입니다.
        /// </summary>
        public float Fraction => Current / Maximum;

        /// <summary>
        /// 피해를 받거나 행동할 수 있는 생존 상태입니다.
        /// </summary>
        public bool IsAlive => Current > 0f;

        /// <summary>
        /// 같은 개체가 부활할 때 이전 공격과 구분하는 생명 번호입니다.
        /// </summary>
        public int LifeVersion { get; private set; }

        /// <summary>
        /// 피해·사망·부활로 체력이 바뀔 때마다 발생합니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 이번 생명에서 체력이 처음 0이 될 때 한 번 발생합니다.
        /// </summary>
        public event Action Died;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 최대 체력으로 새 생명을 시작합니다.
        /// </summary>
        private Health(float maximum)
        {
            Maximum = maximum;
            Current = maximum;
        }

        /// <summary>
        /// 유효한 최대 체력으로 생명을 생성하며 잘못된 수치에는 null을 반환합니다.
        /// </summary>
        public static Health Create(float maximum)
        {
            if (!maximum.ExIsPositive())
            {
                SWLog.LogWarning("[Health] 생성 실패: 최대 체력은 0보다 큰 유한한 수여야 합니다.");
                return null;
            }

            return new Health(maximum);
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 최대 체력을 변경하고 현재 체력은 새 상한까지만 유지합니다. 사망자는 부활하지 않으며 잘못된 값이면 false입니다.
        /// </summary>
        public bool SetMaximum(float maximum)
        {
            if (!maximum.ExIsPositive())
            {
                SWLog.LogWarning("[Health] 최대 체력 변경 실패: 양수가 필요합니다.");
                return false;
            }

            if (Maximum == maximum)
            {
                return true;
            }

            Maximum = maximum;
            Current = Math.Min(Current, Maximum);
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 유효한 피해만 적용합니다. 이미 사망한 개체의 중복 피해는 무시합니다.
        /// </summary>
        public bool TakeDamage(float amount)
        {
            if (!IsAlive || !amount.ExIsPositive())
            {
                return false;
            }

            Current = Math.Max(0f, Current - amount);
            Changed?.Invoke();
            if (!IsAlive)
            {
                Died?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// 같은 개체가 부활할 때 최대 체력으로 회복합니다.
        /// </summary>
        public void Revive()
        {
            LifeVersion++;
            Current = Maximum;
            Changed?.Invoke();
        }

        #endregion // 함수
    }
}
