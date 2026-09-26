using System;
using UnityEngine;

using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 기본 공격 한 번의 결과입니다. 회피하면 피해량은 0입니다.
    /// </summary>
    public readonly struct DamageResult
    {
        #region 프로퍼티
        /// <summary>
        /// 대상이 공격을 회피했는지 반환합니다.
        /// </summary>
        public bool Evaded { get; }

        /// <summary>
        /// 치명타가 발생했는지 반환합니다.
        /// </summary>
        public bool Critical { get; }

        /// <summary>
        /// 방어력까지 적용한 최종 피해입니다.
        /// </summary>
        public float Amount { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 계산한 결과를 보관합니다.
        /// </summary>
        public DamageResult(bool evaded, bool critical, float amount)
        {
            Evaded = evaded;
            Critical = critical;
            Amount = amount;
        }

        #endregion // 초기화
    }

    /// <summary>
    /// 회피 → 치명타 → 방어·관통 순서로 기본 공격 피해를 계산합니다. 모든 보조 수치가 0이면 기본 피해를 그대로 반환합니다.
    /// </summary>
    public sealed class DamageCalculator
    {
        #region 필드
        private readonly Func<float> random;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 0 이상 1 이하의 값을 돌려주는 난수 함수를 연결합니다.
        /// </summary>
        public DamageCalculator(Func<float> random)
        {
            this.random = random;
        }

        #endregion // 초기화

        #region 계산
        /// <summary>
        /// 공격자와 대상의 현재 수치로 한 번의 피해를 계산합니다. 확률이 0이면 난수를 사용하지 않습니다.
        /// </summary>
        public DamageResult Calculate(AttackProfile attack, float defense, float evasion)
        {
            if (Roll(Mathf.Min(evasion, ProjectDefine.Battle.MaximumEvasion)))
            {
                return new DamageResult(true, false, 0f);
            }

            bool critical = Roll(Mathf.Min(attack.CriticalChance, ProjectDefine.Battle.MaximumCriticalChance));
            float damage = critical
                ? attack.Damage * (ProjectDefine.Battle.BaseCriticalMultiplier + Mathf.Max(0f, attack.CriticalDamage))
                : attack.Damage;
            float effectiveDefense = defense - Mathf.Max(0f, attack.ArmorPenetration);
            if (!(effectiveDefense > 0f))
            {
                return new DamageResult(false, critical, damage);
            }

            float scale = ProjectDefine.Battle.DefenseScale;
            return new DamageResult(false, critical, damage * scale / (scale + effectiveDefense));
        }

        /// <summary>
        /// 확률 판정입니다. 0 이하는 항상 실패, 1 이상은 항상 성공입니다.
        /// </summary>
        private bool Roll(float chance)
        {
            if (!(chance > 0f))
            {
                return false;
            }

            return chance >= 1f || random() < chance;
        }

        #endregion // 계산
    }
}
