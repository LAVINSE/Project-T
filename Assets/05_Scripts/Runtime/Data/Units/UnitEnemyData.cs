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
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Data", "ProjectT.Runtime", "EnemyDefinition")]
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

        /// <summary>
        /// 능력치와 적 프리팹 구성이 유효할 때만 참입니다. 미설정 적은 생성되지 않습니다.
        /// </summary>
        public override bool IsValid => base.IsValid
            && Prefab.GetComponent<EnemyUnit>() != null
            && Positive(workshopAttackDamage)
            && Positive(workshopAttackInterval);

        #endregion // 프로퍼티
    }
}
