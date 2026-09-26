using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Stat;

namespace ProjectT.Data
{
    /// <summary>
    /// 스테이지의 라운드 구성과 전투 시작 경제를 정의합니다. 이후 스테이지는 별도 자산으로 확장합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageData", menuName = "Project T/데이터/스테이지")]
    public sealed class StageData : ProjectData
    {
        #region 필드
        [SerializeField] private string displayName;
        [SerializeField] private string scenePath;
        [SerializeField] private double startingCurrency;
        [SerializeField] private CurrencyData deploymentCurrency;

        [SerializeField] private SWStatOverride workshopMaximumHealth;
        [SerializeField] private float spawnInterval;
        [SerializeField] private int[] enemiesPerRound = Array.Empty<int>();
        [SerializeField] private EnemyRouteData enemyRoute;
        [SerializeField] private UnitEnemyData enemy;
        [SerializeField] private UnitClassData[] classes = Array.Empty<UnitClassData>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 공방 최대 체력 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat WorkshopMaximumHealthStat => workshopMaximumHealth?.Stat;

        /// <summary>
        /// 스테이지 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 거점에서 출전할 때 불러올 전투 장면 경로입니다. 빌드 설정에 등록되어 있어야 합니다.
        /// </summary>
        public string ScenePath => scenePath;

        /// <summary>
        /// 새 전투마다 지급하는 배치 재화입니다.
        /// </summary>
        public double StartingCurrency => startingCurrency;

        /// <summary>
        /// 시작 금액·배치 비용·처치 보상이 사용하는 전투 재화의 자산 정의입니다.
        /// </summary>
        public CurrencyData DeploymentCurrency => deploymentCurrency;

        /// <summary>
        /// 새 출전에서 공방에 부여하는 최대 체력입니다. 라운드 휴식으로 회복하지 않습니다.
        /// </summary>
        public float WorkshopMaximumHealth => workshopMaximumHealth.ExGetConfiguredValue();

        /// <summary>
        /// 라운드 안에서 적이 등장하는 간격입니다.
        /// </summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>
        /// 전체 라운드 수입니다.
        /// </summary>
        public int RoundCount => enemiesPerRound.Length;

        /// <summary>
        /// 모든 라운드의 적 수 합계입니다.
        /// </summary>
        public int TotalEnemyCount
        {
            get
            {
                int total = 0;
                foreach (int count in enemiesPerRound)
                {
                    total += count;
                }

                return total;
            }
        }

        /// <summary>
        /// 고정된 적 이동 경로입니다.
        /// </summary>
        public EnemyRouteData EnemyRoute => enemyRoute;

        /// <summary>
        /// 이 스테이지의 기본 적입니다.
        /// </summary>
        public UnitEnemyData Enemy => enemy;

        /// <summary>
        /// 이 전투에서 구매할 수 있는 클래스 목록입니다.
        /// </summary>
        public IReadOnlyList<UnitClassData> Classes => classes;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 0부터 시작하는 라운드 순서의 적 수입니다.
        /// </summary>
        public int GetEnemyCount(int roundIndex)
        {
            return enemiesPerRound[roundIndex];
        }

        /// <summary>
        /// 이 스테이지의 구매 목록에 클래스가 포함되는지 확인합니다.
        /// </summary>
        public bool HasClass(UnitClassData unitClass)
        {
            return unitClass != null && Array.IndexOf(classes, unitClass) >= 0;
        }

        /// <summary>
        /// 경제·라운드·참조 설정을 검사합니다. 참조된 데이터 내부는 각 데이터가 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckName(displayName, nameof(displayName), issues);
            valid &= Check(!string.IsNullOrEmpty(scenePath) && SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0,
                nameof(scenePath), "빌드 설정에 등록된 전투 장면 경로를 입력하세요. 예: Assets/01_Scenes/Stage01_Grassland.unity", issues);
            valid &= CheckNonNegative(startingCurrency, nameof(startingCurrency), issues);
            valid &= CheckRequired(deploymentCurrency, nameof(deploymentCurrency), issues);
            valid &= CheckPositive(WorkshopMaximumHealth, nameof(workshopMaximumHealth), issues);
            valid &= CheckPositive(spawnInterval, nameof(spawnInterval), issues);
            valid &= CheckRequired(enemyRoute, nameof(enemyRoute), issues);
            valid &= CheckRequired(enemy, nameof(enemy), issues);
            valid &= Check(enemiesPerRound.Length > 0, nameof(enemiesPerRound), "라운드를 한 개 이상 추가하세요.", issues);
            for (int index = 0; index < enemiesPerRound.Length; index++)
            {
                valid &= CheckPositive(enemiesPerRound[index], nameof(enemiesPerRound) + ".Array.data[" + index + "]", issues);
            }

            valid &= Check(classes.Length > 0, nameof(classes), "항목을 한 개 이상 추가하세요.", issues);
            for (int index = 0; index < classes.Length; index++)
            {
                valid &= CheckRequired(classes[index], nameof(classes) + ".Array.data[" + index + "]", issues);
            }

            valid &= CheckStat(workshopMaximumHealth, nameof(workshopMaximumHealth), issues);
            return valid;
        }

        #endregion // 함수

        #region 스탯 정의
        /// <summary>
        /// 개체별 런타임 스탯으로 복제할 설정을 열거합니다. 참조 유효성은 Validate에서 검사합니다.
        /// </summary>
        public  IEnumerable<SWStatOverride> GetStatSettings()
        {
            yield return workshopMaximumHealth;
        }

        #endregion // 스탯 정의
    }
}
