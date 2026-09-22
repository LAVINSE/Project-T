using UnityEngine;

using ProjectT.Units;

namespace ProjectT.Data
{
    /// <summary>
    /// 캐릭터의 공통 유닛 정보와 구매·저지·부활 설정을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitClassData", menuName = "Project T/데이터/캐릭터")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Data", "ProjectT.Runtime", "AllyClassDefinition")]
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

        /// <summary>
        /// 능력치와 캐릭터 프리팹 구성이 유효할 때만 참입니다.
        /// </summary>
        public override bool IsValid => base.IsValid
            && Prefab.GetComponent<CharacterUnit>() != null
            && deploymentCost >= 0d
            && !double.IsNaN(deploymentCost)
            && !double.IsInfinity(deploymentCost)
            && blockCapacity >= 0
            && Positive(revivalSeconds);

        #endregion // 프로퍼티
    }
}
