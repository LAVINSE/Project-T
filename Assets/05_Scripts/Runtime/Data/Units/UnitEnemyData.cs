using System;
using System.Collections.Generic;
using UnityEngine;

using ProjectT.Units;

namespace ProjectT.Data
{
    /// <summary>
    /// 적의 공통 유닛 정보와 공방 공격·처치 보상을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitEnemyData", menuName = "Project T/데이터/적")]
    public sealed class UnitEnemyData : UnitData
    {
        #region 필드
        [SerializeField] private float workshopAttackDamage;
        [SerializeField] private float workshopAttackInterval;
        [SerializeField] private RewardEntry[] rewards = Array.Empty<RewardEntry>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 공방에 주는 타격 피해입니다.
        /// </summary>
        public float WorkshopAttackDamage => workshopAttackDamage;

        /// <summary>
        /// 공방 공격 시작 사이의 게임 시간입니다.
        /// </summary>
        public float WorkshopAttackInterval => workshopAttackInterval;

        /// <summary>
        /// 처치 시 독립 확률로 계산할 보상 목록입니다.
        /// </summary>
        public IReadOnlyList<RewardEntry> Rewards => rewards;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 공통 설정과 공방 공격·보상 목록을 검사합니다. 보상이 없는 적은 허용합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = base.Validate(issues);
            valid &= CheckPositive(workshopAttackDamage, nameof(workshopAttackDamage), issues);
            valid &= CheckPositive(workshopAttackInterval, nameof(workshopAttackInterval), issues);
            for (int index = 0; index < rewards.Length; index++)
            {
                string path = nameof(rewards) + ".Array.data[" + index + "]";
                valid &= rewards[index] != null
                    ? rewards[index].Validate(this, path, issues)
                    : Check(false, path, "빈 보상 항목을 제거하세요.", issues);
            }

            return valid;
        }

        /// <summary>
        /// 적 프리팹에는 EnemyUnit이 필요합니다.
        /// </summary>
        protected override bool HasUnitComponent(GameObject unitPrefab)
        {
            return unitPrefab.GetComponent<EnemyUnit>() != null;
        }

        #endregion // 검사
    }
}
