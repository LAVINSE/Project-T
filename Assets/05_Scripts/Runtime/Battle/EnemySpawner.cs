using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Pooling;
using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Units;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 라운드의 적을 간격마다 경로 입구에 생성하고, 처치된 적을 사망 동작이 끝난 뒤 풀에 반환합니다.
    /// </summary>
    public sealed class EnemySpawner
    {
        #region 필드
        private readonly UnitEnemyData enemyData;
        private readonly FixedRoute route;
        private readonly Transform unitParent;
        private readonly ColorData colors;
        private readonly SWTimer spawnTimer;
        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
        private readonly List<EnemyUnit> resolvedEnemies = new List<EnemyUnit>();
        private readonly List<EnemyUnit> dyingEnemies = new List<EnemyUnit>();
        private int roundEnemyCount;
        private int spawnedCount;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 아직 처치되지 않은 전장의 적 목록입니다. 교전 계산이 같은 목록을 사용합니다.
        /// </summary>
        public List<EnemyUnit> Enemies => enemies;

        /// <summary>
        /// 이번 라운드의 적을 모두 생성했고 전장에 남은 적이 없는지 반환합니다.
        /// </summary>
        public bool IsRoundCleared => spawnedCount >= roundEnemyCount && enemies.Count == 0;

        /// <summary>
        /// 적이 처치되어 정산이 필요할 때 발생합니다.
        /// </summary>
        public event Action<EnemyUnit> EnemyResolved;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 생성할 적·경로·배치 부모·표시 색상과 생성 간격을 연결합니다.
        /// </summary>
        public EnemySpawner(UnitEnemyData enemyData, FixedRoute route, Transform unitParent, ColorData colors, float spawnInterval)
        {
            this.enemyData = enemyData;
            this.route = route;
            this.unitParent = unitParent;
            this.colors = colors;
            spawnTimer = new SWTimer(spawnInterval, false, false, SWTimer.TimeMode.Manual);
        }

        #endregion // 초기화

        #region 생성
        /// <summary>
        /// 새 라운드의 적 수를 설정하고 첫 적을 다음 갱신에 바로 생성합니다.
        /// </summary>
        public void StartRound(int enemyCount)
        {
            roundEnemyCount = enemyCount;
            spawnedCount = 0;
            spawnTimer.Complete();
        }

        /// <summary>
        /// 생성 간격이 지났으면 적 하나를 생성합니다. 생성에 실패하면 false를 반환합니다.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            spawnTimer.Tick(deltaTime);
            if (spawnedCount >= roundEnemyCount || !spawnTimer.IsDone)
            {
                return true;
            }

            SWPool pool = SWPool.Instance;
            EnemyUnit enemy = pool.Spawn<EnemyUnit>(enemyData.Prefab, route.GetPoint(0), Quaternion.identity, unitParent);
            if (enemy == null || !enemy.Initialize(enemyData, route))
            {
                if (enemy != null)
                {
                    pool.Release(enemy.gameObject);
                }

                SWLog.LogWarning("[EnemySpawner] 적 생성 실패: 적 프리팹과 초기화 설정을 확인해 주세요.");
                return false;
            }

            enemy.GetComponent<UnitView>().Initialize(colors);
            enemy.Resolved += OnEnemyResolved;
            enemies.Add(enemy);
            spawnedCount++;
            spawnTimer.Start();
            return true;
        }

        /// <summary>
        /// 모든 적의 이동을 진행합니다.
        /// </summary>
        public void TickEnemies(float deltaTime)
        {
            for (int index = 0; index < enemies.Count; index++)
            {
                enemies[index].Tick(deltaTime);
            }
        }

        #endregion // 생성

        #region 정리
        /// <summary>
        /// 처치된 적을 전장 목록에서 제외하고 사망 동작이 끝난 뒤 풀에 반환합니다.
        /// </summary>
        public void ReleaseResolved()
        {
            if (resolvedEnemies.Count == 0)
            {
                return;
            }

            SWPool pool = SWPool.Instance;
            dyingEnemies.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeSelf);
            foreach (EnemyUnit enemy in resolvedEnemies)
            {
                enemies.Remove(enemy);
                dyingEnemies.Add(enemy);
                pool.Release(enemy.gameObject, enemy.Definition.Animation.DeathDuration);
            }

            resolvedEnemies.Clear();
        }

        /// <summary>
        /// 장면 종료 때 전장과 사망 대기 중인 적을 모두 풀에 반환합니다.
        /// </summary>
        public void ReleaseAll()
        {
            SWPool pool = SWPool.HasInstance ? SWPool.Instance : null;
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.Resolved -= OnEnemyResolved;
                    ReleaseActive(pool, enemy);
                }
            }

            foreach (EnemyUnit enemy in dyingEnemies)
            {
                ReleaseActive(pool, enemy);
            }

            enemies.Clear();
            dyingEnemies.Clear();
        }

        /// <summary>
        /// 처치된 적을 반환 대기 목록에 넣고 정산을 알립니다.
        /// </summary>
        private void OnEnemyResolved(EnemyUnit enemy)
        {
            enemy.Resolved -= OnEnemyResolved;
            resolvedEnemies.Add(enemy);
            EnemyResolved?.Invoke(enemy);
        }

        /// <summary>
        /// 아직 활성 상태인 개체만 풀에 반환합니다.
        /// </summary>
        private static void ReleaseActive(SWPool pool, EnemyUnit enemy)
        {
            if (pool != null && enemy != null && enemy.gameObject.activeSelf)
            {
                pool.Release(enemy.gameObject);
            }
        }

        #endregion // 정리
    }
}
