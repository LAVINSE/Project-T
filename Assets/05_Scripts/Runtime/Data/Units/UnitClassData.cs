using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Stat;
using SW.Attributes;

using ProjectT.Units;

namespace ProjectT.Data
{
    /// <summary>
    /// 캐릭터의 전투 설정과 성장 표시 값을 관리합니다.
    /// 전투 규칙이 직접 읽는 능력치만 이름을 가진 항목으로 두고, 표시만 하는 능력치는 추가 능력치 목록에 넣습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitClassData", menuName = "Project T/데이터/캐릭터")]
    public sealed class UnitClassData : UnitData
    {
        #region 필드

        [SerializeField] private SWStatOverride deploymentCost;
        [SerializeField] private SWStatOverride blockCapacity;
        [SerializeField] private SWStatOverride revivalSeconds;

        [SWGroup("성장 표시")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private double experience;
        [SerializeField, Min(0)] private double requiredExperience;

        [SWGroup("추가 능력치 · 전투 계산 미적용")]

        [SerializeField] private SWStatOverride[] additionalStats = Array.Empty<SWStatOverride>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 동시 저지 수 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat BlockCapacityStat => blockCapacity?.Stat;

        /// <summary>
        /// 부활 대기시간 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat RevivalSecondsStat => revivalSeconds?.Stat;

        /// <summary>
        /// 배치 비용 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat DeploymentCostStat => deploymentCost?.Stat;

        /// <summary>
        /// 캐릭터 한 명을 배치할 때 필요한 비용입니다.
        /// </summary>
        public double DeploymentCost => deploymentCost.ExGetConfiguredValue();

        /// <summary>
        /// 동시에 저지할 적 수입니다. 0이면 저지하지 않습니다.
        /// </summary>
        public int BlockCapacity => (int)blockCapacity.ExGetConfiguredValue();

        /// <summary>
        /// 사망 후 무료 부활까지 걸리는 게임 시간입니다.
        /// </summary>
        public float RevivalSeconds => revivalSeconds.ExGetConfiguredValue();

        /// <summary>
        /// 캐릭터에 표시하는 설정 레벨입니다. 자동 레벨업은 수행하지 않습니다.
        /// </summary>
        public int Level => level;

        /// <summary>
        /// 현재 레벨의 설정 경험치입니다. 전투 보상으로 증가시키지 않습니다.
        /// </summary>
        public double Experience => experience;

        /// <summary>
        /// 다음 레벨에 필요한 경험치입니다. 0은 아직 기준이 설정되지 않았음을 뜻합니다.
        /// </summary>
        public double RequiredExperience => requiredExperience;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 공통 전투 설정과 성장·추가 능력치의 입력 범위를 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = base.Validate(issues);
            valid &= CheckNonNegative(DeploymentCost, nameof(deploymentCost), issues);
            float capacity = blockCapacity.ExGetConfiguredValue();
            valid &= Check(capacity.ExIsNonNegative() && capacity < int.MaxValue && capacity == Mathf.Floor(capacity),
                nameof(blockCapacity), "저지 수는 정수 범위의 0 이상 정수여야 합니다.", issues);
            float cost = deploymentCost.ExGetConfiguredValue();
            valid &= Check(cost.ExIsNonNegative() && cost == Mathf.Floor(cost),
                nameof(deploymentCost), "배치 비용은 0 이상 정수여야 합니다.", issues);
            valid &= CheckPositive(RevivalSeconds, nameof(revivalSeconds), issues);
            valid &= Check(level >= 1, nameof(level), "레벨은 1 이상이어야 합니다.", issues);
            valid &= CheckNonNegative(experience, nameof(experience), issues);
            valid &= CheckNonNegative(requiredExperience, nameof(requiredExperience), issues);
            valid &= CheckStat(blockCapacity, nameof(blockCapacity), issues);
            valid &= CheckStat(revivalSeconds, nameof(revivalSeconds), issues);
            valid &= CheckStat(deploymentCost, nameof(deploymentCost), issues);
            foreach (SWStatOverride setting in additionalStats)
            {
                valid &= CheckStat(setting, nameof(additionalStats), issues);
                valid &= CheckNonNegative(setting.ExGetConfiguredValue(), nameof(additionalStats), issues);
            }

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

        #region 스탯 정의
        /// <summary>
        /// 개체별 런타임 스탯으로 복제할 설정을 열거합니다. 참조 유효성은 Validate에서 검사합니다.
        /// </summary>
        public override IEnumerable<SWStatOverride> GetStatSettings()
        {
            foreach (SWStatOverride setting in base.GetStatSettings())
            {
                yield return setting;
            }

            yield return blockCapacity;
            yield return revivalSeconds;
            yield return deploymentCost;
            foreach (SWStatOverride setting in additionalStats)
            {
                yield return setting;
            }
        }

        #endregion // 스탯 정의
    }
}
