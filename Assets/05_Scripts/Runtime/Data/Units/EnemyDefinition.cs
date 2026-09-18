using UnityEngine;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.Data
{
    /// <summary>
    /// 적의 전투 수치와 처치 보상 및 외형을 정의합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/디펜스/적")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Units", "ProjectT.Defense.Runtime", "EnemyDefinition")]
    public sealed class EnemyDefinition : SWScriptableObject
    {
        #region 필드
        [SerializeField] private float maximumHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackInterval;
        [SerializeField, Min(0.01f)] private float workshopAttackDamage = 10f;
        [SerializeField, Min(0.01f)] private float workshopAttackInterval = 1.4f;
        [SerializeField] private double killReward;
        [SerializeField] private UnitAppearance appearance;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 최대 체력입니다.
        /// </summary>
        public float MaximumHealth => maximumHealth;

        /// <summary>
        /// 초당 경로 이동 거리입니다.
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// 아군에게 주는 단일 공격 피해입니다.
        /// </summary>
        public float AttackDamage => attackDamage;

        /// <summary>
        /// 아군을 공격할 수 있는 거리입니다.
        /// </summary>
        public float AttackRange => attackRange;

        /// <summary>
        /// 공격 사이의 대기 시간입니다.
        /// </summary>
        public float AttackInterval => attackInterval;

        /// <summary>
        /// 공방에 주는 단일 타격 피해입니다. 아군 대상 피해와 별도로 조정합니다.
        /// </summary>
        public float WorkshopAttackDamage => workshopAttackDamage;

        /// <summary>
        /// 공방 공격의 시작 간격입니다. 실제 타격은 공격 애니메이션의 지정 시점에 발생합니다.
        /// </summary>
        public float WorkshopAttackInterval => workshopAttackInterval;

        /// <summary>
        /// 사망이 확정된 적 한 명의 배치 재화 보상입니다.
        /// </summary>
        public double KillReward => killReward;

        /// <summary>
        /// 상태별 애니메이션 외형입니다.
        /// </summary>
        public UnitAppearance Appearance => appearance;

        #endregion // 프로퍼티
    }
}
