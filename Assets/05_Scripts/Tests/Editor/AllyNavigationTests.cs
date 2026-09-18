using UnityEngine;

using NUnit.Framework;

using ProjectT.Navigation;
using ProjectT.Units;

namespace ProjectT.Tests
{
    /// <summary>
    /// 자유 목적지, 장애물 우회, 실패한 명령 보존과 생명주기의 경계를 검증합니다.
    /// </summary>
    public sealed class AllyNavigationTests
    {
        #region 함수
        /// <summary>
        /// 장애물 반대편 목적지로 이동할 때 벽을 통과하지 않고 클릭 좌표에 정확히 도착합니다.
        /// </summary>
        [Test]
        public void MovementDetoursObstacleAndKeepsExactDestination()
        {
            var mapObject = new GameObject("NavigationTest");
            var unitObject = new GameObject("AllyTest");
            try
            {
                var terrain = mapObject.AddComponent<WalkableBattlefield>();
                var obstacle = new Rect(-0.6f, -1.5f, 1.2f, 3f);
                terrain.Configure(new Rect(-5f, -5f, 10f, 10f), 0.5f, new[] { obstacle });
                var movement = unitObject.AddComponent<AllyDestinationMovement>();
                movement.Initialize(terrain, new Vector2(-3f, 0f), 2f);
                Vector2 destination = new Vector2(3.13f, 0.27f);
                Assert.That(movement.TryMove(destination), Is.True);
                for (int index = 0; index < 1000 && movement.IsMoving; index++)
                {
                    movement.Advance(0.01f);
                    Assert.That(obstacle.Contains(unitObject.transform.position), Is.False);
                }

                Assert.That((Vector2)unitObject.transform.position, Is.EqualTo(destination));
                Assert.That(movement.IsMoving, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(unitObject);
                Object.DestroyImmediate(mapObject);
            }
        }

        /// <summary>
        /// 통과할 수 없는 목적지와 비정상 좌표는 진행 중인 이동을 취소하지 않습니다.
        /// </summary>
        [Test]
        public void FailedCommandPreservesCurrentDestinationAndProgress()
        {
            var mapObject = new GameObject("CommandTest");
            try
            {
                var terrain = mapObject.AddComponent<WalkableBattlefield>();
                terrain.Configure(new Rect(-5, -5, 10, 10), 0.5f, new[] { new Rect(0, -5, 1, 10) });
                var movement = mapObject.AddComponent<AllyDestinationMovement>();
                movement.Initialize(terrain, new Vector2(-4, 0), 1);
                Assert.That(movement.TryMove(new Vector2(-2, 0)), Is.True);
                movement.Advance(0.5f);
                Assert.That(movement.TryMove(new Vector2(3, 0)), Is.False);
                Assert.That(movement.TryMove(new Vector2(float.NaN, 0)), Is.False);
                Assert.That(movement.Destination, Is.EqualTo(new Vector2(-2, 0)));
                Assert.That(movement.IsMoving, Is.True);
                movement.Advance(100f);
                Assert.That((Vector2)mapObject.transform.position, Is.EqualTo(new Vector2(-2, 0)));
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
            }
        }

        /// <summary>
        /// 원래 위치로 명령하거나 게임 시간이 멈춘 경우 상태가 불필요하게 진행되지 않습니다.
        /// </summary>
        [Test]
        public void StationaryDestinationAndZeroTimeAreSafe()
        {
            var root = new GameObject("PauseTest");
            try
            {
                var terrain = root.AddComponent<WalkableBattlefield>();
                terrain.Configure(new Rect(-5, -5, 10, 10), 0.5f, null);
                var movement = root.AddComponent<AllyDestinationMovement>();
                movement.Initialize(terrain, Vector2.zero, 3f);
                Assert.That(movement.TryMove(Vector2.zero), Is.True);
                Assert.That(movement.IsMoving, Is.False);
                movement.TryMove(Vector2.right);
                movement.Advance(0f);
                Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
                movement.Advance(float.MaxValue);
                Assert.That(movement.IsMoving, Is.False);
                Assert.That((Vector2)root.transform.position, Is.EqualTo(Vector2.right));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 중복 피해는 사망 알림을 중복 발행하지 않으며 부활 뒤 새 생명으로 취급합니다.
        /// </summary>
        [Test]
        public void DeathIsOncePerLifeAndInvalidDamageDoesNotChangeHealth()
        {
            var health = CombatHealth.Create(100);
            int deaths = 0;
            health.Died += () => deaths++;
            Assert.That(health.TakeDamage(float.NaN), Is.False);
            Assert.That(health.TakeDamage(-10), Is.False);
            health.TakeDamage(120);
            health.TakeDamage(50);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(health.Current, Is.Zero);
            health.Revive();
            Assert.That(health.Current, Is.EqualTo(100));
            health.TakeDamage(100);
            Assert.That(deaths, Is.EqualTo(2));
        }

        /// <summary>
        /// 같은 탐색 칸 안에 얇은 벽이 있어도 경로를 생략해 반대편으로 통과하지 않습니다.
        /// </summary>
        [Test]
        public void ThinObstacleWithinOneCellCannotBeSkipped()
        {
            var root = new GameObject("ThinObstacleTest");
            try
            {
                var terrain = root.AddComponent<WalkableBattlefield>();
                terrain.Configure(new Rect(-5, -5, 10, 10), 0.5f, new[] { new Rect(-0.12f, -0.5f, 0.02f, 1f) });
                Assert.That(terrain.TryFindPath(new Vector2(-0.2f, 0.1f), new Vector2(-0.05f, 0.1f), out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        #endregion // 함수
    }
}
