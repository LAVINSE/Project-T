using System;
using UnityEngine;

using SW.Base;

using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Units;

namespace ProjectT.Data
{
    /// <summary>
    /// 스테이지의 라운드 구성과 전투 시작 경제를 정의합니다. 이후 스테이지는 별도 자산으로 확장합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/디펜스/스테이지")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Battle", "ProjectT.Defense.Runtime", "StageDefinition")]
    public sealed class StageDefinition : SWScriptableObject
    {
        #region 필드
        [SerializeField] private string displayName;
        [SerializeField] private double startingCurrency;
        [SerializeField, Min(1f)] private float workshopMaximumHealth = 300f;
        [SerializeField] private float spawnInterval;
        [SerializeField] private int[] enemiesPerRound = Array.Empty<int>();
        [SerializeField] private EnemyRouteDefinition enemyRoute;
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField] private AllyClassDefinition[] classes = Array.Empty<AllyClassDefinition>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 스테이지 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 새 전투마다 지급하는 배치 재화입니다.
        /// </summary>
        public double StartingCurrency => startingCurrency;

        /// <summary>
        /// 새 출전에서 공방에 부여하는 최대 체력입니다. 라운드 휴식으로 회복하지 않습니다.
        /// </summary>
        public float WorkshopMaximumHealth => workshopMaximumHealth;

        /// <summary>
        /// 라운드 안에서 적이 등장하는 간격입니다.
        /// </summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>
        /// 전체 라운드 수입니다.
        /// </summary>
        public int RoundCount => enemiesPerRound.Length;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 지정한 라운드의 적 수입니다.
        /// </summary>
        public int GetEnemyCount(int round)
        {
            return enemiesPerRound[round];
        }

        /// <summary>
        /// 고정된 적 이동 경로입니다.
        /// </summary>
        public EnemyRouteDefinition EnemyRoute => enemyRoute;

        /// <summary>
        /// 이 스테이지의 기본 적입니다.
        /// </summary>
        public EnemyDefinition Enemy => enemy;

        /// <summary>
        /// 이 전투에서 구매할 수 있는 클래스 목록입니다.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<AllyClassDefinition> Classes => classes;

        #endregion // 함수
    }
}
