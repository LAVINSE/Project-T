using System.Collections.Generic;
using UnityEngine;

using ProjectT.Units;

namespace ProjectT.Data
{
    /// <summary>
    /// 캐릭터의 공통 유닛 정보와 구매·저지·부활 설정을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitClassData", menuName = "Project T/데이터/캐릭터")]
    public sealed class UnitClassData : UnitData
    {
        #region 필드
        [SerializeField] private double deploymentCost;
        [SerializeField] private int blockCapacity;
        [SerializeField] private float revivalSeconds;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 캐릭터 한 명을 배치할 때 필요한 비용입니다.
        /// </summary>
        public double DeploymentCost => deploymentCost;

        /// <summary>
        /// 동시에 저지할 적 수입니다. 0이면 저지하지 않습니다.
        /// </summary>
        public int BlockCapacity => blockCapacity;

        /// <summary>
        /// 사망 후 무료 부활까지 걸리는 게임 시간입니다.
        /// </summary>
        public float RevivalSeconds => revivalSeconds;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 공통 설정과 구매·저지·부활 수치를 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = base.Validate(issues);
            valid &= CheckNonNegative(deploymentCost, nameof(deploymentCost), issues);
            valid &= CheckNonNegative(blockCapacity, nameof(blockCapacity), issues);
            valid &= CheckPositive(revivalSeconds, nameof(revivalSeconds), issues);
            return valid;
        }

        /// <summary>
        /// 캐릭터 프리팹에는 CharacterUnit이 필요합니다.
        /// </summary>
        protected override bool HasUnitComponent(GameObject unitPrefab)
        {
            return unitPrefab.GetComponent<CharacterUnit>() != null;
        }

        #endregion // 검사
    }
}
