using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectT.Data;

namespace ProjectT.Data
{
    /// <summary>
    /// 클래스의 구매 비용, 개체 능력치와 부활 시간을 정의합니다. 개체별 현재 상태는 저장하지 않습니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/디펜스/아군 클래스")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Units", "ProjectT.Defense.Runtime", "AllyClassDefinition")]
    public sealed class AllyClassDefinition : SWScriptableObject
    {
        #region 필드
        [SWGroup("클래스 정보")]
        [SerializeField] private string displayName;
        [SerializeField] private double deploymentCost;
        [SerializeField] private float maximumHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackInterval;
        [SerializeField] private int blockCapacity;
        [SerializeField] private float revivalSeconds;
        [SerializeField] private UnitAppearance appearance;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 화면에 표시할 클래스 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 같은 클래스의 각 개체를 추가 배치할 때 드는 비용입니다.
        /// </summary>
        public double DeploymentCost => deploymentCost;

        /// <summary>
        /// 개체의 최대 체력입니다.
        /// </summary>
        public float MaximumHealth => maximumHealth;

        /// <summary>
        /// 초당 이동 거리입니다.
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// 한 번 공격할 때 주는 피해입니다.
        /// </summary>
        public float AttackDamage => attackDamage;

        /// <summary>
        /// 공격할 수 있는 거리입니다.
        /// </summary>
        public float AttackRange => attackRange;

        /// <summary>
        /// 공격 사이에 필요한 게임 시간입니다.
        /// </summary>
        public float AttackInterval => attackInterval;

        /// <summary>
        /// 동시에 저지할 수 있는 적 수입니다. 원거리 클래스는 0입니다.
        /// </summary>
        public int BlockCapacity => blockCapacity;

        /// <summary>
        /// 이 클래스의 무료 부활 대기 시간입니다.
        /// </summary>
        public float RevivalSeconds => revivalSeconds;

        /// <summary>
        /// 클래스의 외형과 애니메이션입니다.
        /// </summary>
        public UnitAppearance Appearance => appearance;

        /// <summary>
        /// 배치 전에 필수 수치와 외형 설정을 검증합니다.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(displayName)
            && deploymentCost >= 0d
            && !double.IsNaN(deploymentCost)
            && !double.IsInfinity(deploymentCost)
            && Positive(maximumHealth)
            && Positive(moveSpeed)
            && Positive(attackDamage)
            && Positive(attackRange)
            && Positive(attackInterval)
            && Positive(revivalSeconds)
            && blockCapacity >= 0
            && appearance != null;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 값이 0보다 큰 유한한 수인지 확인합니다.
        /// </summary>
        private static bool Positive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion // 함수
    }
}
