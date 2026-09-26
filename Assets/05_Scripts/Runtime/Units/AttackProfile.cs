namespace ProjectT.Units
{
    /// <summary>
    /// 기본 공격 한 번의 피해 계산에 필요한 공격자 수치입니다. 치명타·관통이 없는 공격자는 0을 사용합니다.
    /// </summary>
    public readonly struct AttackProfile
    {
        #region 프로퍼티
        /// <summary>
        /// 방어 적용 전 기본 피해입니다.
        /// </summary>
        public float Damage { get; }

        /// <summary>
        /// 치명타 확률입니다. 1은 100%입니다.
        /// </summary>
        public float CriticalChance { get; }

        /// <summary>
        /// 기본 치명타 배율에 더하는 값입니다.
        /// </summary>
        public float CriticalDamage { get; }

        /// <summary>
        /// 대상 방어력에서 빼는 고정값입니다.
        /// </summary>
        public float ArmorPenetration { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 치명타·관통이 없는 공격 정보를 만듭니다.
        /// </summary>
        public AttackProfile(float damage)
            : this(damage, 0f, 0f, 0f)
        {
        }

        /// <summary>
        /// 공격자의 현재 수치를 보관합니다.
        /// </summary>
        public AttackProfile(
            float damage,
            float criticalChance,
            float criticalDamage,
            float armorPenetration)
        {
            Damage = damage;
            CriticalChance = criticalChance;
            CriticalDamage = criticalDamage;
            ArmorPenetration = armorPenetration;
        }

        #endregion // 초기화
    }
}
