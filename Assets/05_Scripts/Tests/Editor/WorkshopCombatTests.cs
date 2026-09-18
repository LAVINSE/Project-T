using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using NUnit.Framework;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Units;

namespace ProjectT.Tests
{
    /// <summary>
    /// 공방 도착·타격 시점·저지 전환과 처치 정산의 경계를 검증합니다.
    /// </summary>
    public sealed class WorkshopCombatTests
    {
        #region 필드
        private readonly List<GameObject> objects = new List<GameObject>();
        private readonly List<AllyUnit> allies = new List<AllyUnit>();
        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();
        private WalkableBattlefield terrain;
        private WorkshopObjective workshop;
        private BattleCombatSystem combat;
        private EnemyDefinition enemyDefinition;
        private FixedRoute route;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 실제 클래스·공격 자산과 짧은 시험 경로를 연결합니다.
        /// </summary>
        [SetUp]
        public void CreateBattle()
        {
            terrain = CreateObject("WorkshopTestTerrain").AddComponent<WalkableBattlefield>();
            terrain.Configure(new Rect(-10, -10, 20, 20), 0.5f, null);
            workshop = WorkshopObjective.Create(300, new Vector2(1, 0));
            combat = new BattleCombatSystem(allies, enemies, workshop);
            enemyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/02_Res/Data/Units/Skeleton.asset");
            route = FixedRoute.Create(new[] { new Vector2(-5, 0), Vector2.zero });
        }

        /// <summary>
        /// 시험 개체만 제거하고 실제 데이터 자산은 보존합니다.
        /// </summary>
        [TearDown]
        public void RemoveBattle()
        {
            foreach (GameObject instance in objects)
            {
                Object.DestroyImmediate(instance);
            }

            objects.Clear();
            allies.Clear();
            enemies.Clear();
        }

        /// <summary>
        /// 도착 자체는 피해나 종결이 아니며 지정 타격 시점에서만 한 번 피해를 줍니다.
        /// </summary>
        [Test]
        public void ArrivalKeepsEnemyAliveAndWorkshopDamageUsesImpactFrame()
        {
            EnemyUnit enemy = CreateEnemy();
            int resolutions = 0;
            enemy.Resolved += _ => resolutions++;
            enemy.Movement.Advance(100);
            Assert.That(enemy.IsActive, Is.True);
            Assert.That(enemy.HasReachedWorkshop, Is.True);
            Assert.That(resolutions, Is.Zero);
            combat.Tick(0);
            float impact = enemy.Attack.ImpactAt;
            combat.Tick(impact - 0.001f);
            Assert.That(workshop.Health.Current, Is.EqualTo(300));
            combat.Tick(impact);
            combat.Tick(impact);
            Assert.That(workshop.Health.Current, Is.EqualTo(300 - enemyDefinition.WorkshopAttackDamage));
            Assert.That(resolutions, Is.Zero);
        }

        /// <summary>
        /// 공방 공격 중 원거리 아군이 사거리 안에 있어도 공방을 계속 공격합니다.
        /// </summary>
        [Test]
        public void NearbyMageDoesNotRedirectWorkshopAttack()
        {
            AllyUnit mage = CreateAlly("Mage", new Vector2(0.3f, 0));
            EnemyUnit enemy = CreateEnemy();
            enemy.Movement.Advance(100);
            combat.Tick(0);
            Assert.That(enemy.Blocker, Is.Null);
            Assert.That(enemy.Attack.MatchesTarget(workshop.Health), Is.True);
            combat.Tick(enemy.Attack.ImpactAt);
            Assert.That(mage.Health.Current, Is.EqualTo(mage.Health.Maximum));
            Assert.That(workshop.Health.Current, Is.LessThan(300));
        }

        /// <summary>
        /// 뒤늦은 실제 저지는 준비 중인 공방 타격을 취소하고, 이동 또는 사망 해제 뒤 공방으로 돌아갑니다.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LateBlockCancelsWorkshopImpactAndReleaseReturnsToWorkshop(bool killBlocker)
        {
            AllyUnit warrior = CreateAlly("Warrior", new Vector2(5, 5));
            EnemyUnit enemy = CreateEnemy();
            enemy.Movement.Advance(100);
            combat.Tick(0);
            float previousImpact = enemy.Attack.ImpactAt;
            warrior.transform.position = new Vector2(0.3f, 0);
            combat.Tick(previousImpact * 0.5f);
            Assert.That(enemy.Blocker, Is.SameAs(warrior));
            combat.Tick(previousImpact);
            Assert.That(workshop.Health.Current, Is.EqualTo(300));
            combat.Tick(2);
            Assert.That(enemy.Attack.MatchesTarget(warrior.Health), Is.True);
            combat.Tick(enemy.Attack.ImpactAt);
            Assert.That(warrior.Health.Current, Is.LessThan(warrior.Health.Maximum));
            if (killBlocker)
            {
                warrior.Health.TakeDamage(10000);
            }
            else
            {
                Assert.That(warrior.TryMove(new Vector2(5, 5)), Is.True);
            }

            Assert.That(enemy.Blocker, Is.Null);
            combat.Tick(4);
            Assert.That(enemy.Attack.MatchesTarget(workshop.Health), Is.True);
            combat.Tick(enemy.Attack.ImpactAt);
            Assert.That(workshop.Health.Current, Is.LessThan(300));
        }

        /// <summary>
        /// 저지 수가 가득 찬 근접 아군의 주변 적은 공방 공격을 유지합니다.
        /// </summary>
        [Test]
        public void ExcessEnemiesKeepAttackingWorkshopBesideOccupiedWarrior()
        {
            AllyUnit warrior = CreateAlly("Warrior", new Vector2(0.3f, 0));
            EnemyUnit blocked = CreateEnemy();
            EnemyUnit excess = CreateEnemy();
            blocked.Movement.Advance(100);
            excess.Movement.Advance(100);
            combat.Tick(0);
            Assert.That(blocked.Blocker, Is.SameAs(warrior));
            Assert.That(excess.Blocker, Is.Null);
            Assert.That(blocked.Attack.MatchesTarget(warrior.Health), Is.True);
            Assert.That(excess.Attack.MatchesTarget(workshop.Health), Is.True);
        }

        /// <summary>
        /// 처치된 적의 공방 타격은 취소되며 재사용한 개체는 새 경로에서 시작합니다.
        /// </summary>
        [Test]
        public void KillResolvesOnceCancelsImpactAndReuseClearsArrival()
        {
            EnemyUnit enemy = CreateEnemy();
            int resolutions = 0;
            enemy.Resolved += _ => resolutions++;
            enemy.Movement.Advance(100);
            combat.Tick(0);
            float impact = enemy.Attack.ImpactAt;
            enemy.Health.TakeDamage(10000);
            enemy.Health.TakeDamage(10000);
            combat.Tick(impact);
            Assert.That(workshop.Health.Current, Is.EqualTo(300));
            Assert.That(resolutions, Is.EqualTo(1));
            enemy.Initialize(enemyDefinition, route);
            Assert.That(enemy.HasReachedWorkshop, Is.False);
            combat.Tick(impact + 1);
            Assert.That(workshop.Health.Current, Is.EqualTo(300));
        }

        /// <summary>
        /// 공방 도착 전에는 기존 규칙대로 저지하지 않는 아군도 피격 대상입니다.
        /// </summary>
        [Test]
        public void BeforeArrivalEnemyStillAttacksNearbyMage()
        {
            AllyUnit mage = CreateAlly("Mage", new Vector2(-4.7f, 0));
            EnemyUnit enemy = CreateEnemy();
            combat.Tick(0);
            Assert.That(enemy.Attack.MatchesTarget(mage.Health), Is.True);
            combat.Tick(enemy.Attack.ImpactAt);
            Assert.That(mage.Health.Current, Is.LessThan(mage.Health.Maximum));
            Assert.That(workshop.Health.Current, Is.EqualTo(300));
        }

        /// <summary>
        /// 테스트 종료 시 함께 제거할 객체를 생성합니다.
        /// </summary>
        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objects.Add(instance);
            return instance;
        }

        /// <summary>
        /// 지정 경로와 설정으로 검증용 적을 초기화합니다.
        /// </summary>
        private EnemyUnit CreateEnemy()
        {
            var enemy = CreateObject("WorkshopTestEnemy").AddComponent<EnemyUnit>();
            enemy.Initialize(enemyDefinition, route);
            enemies.Add(enemy);
            return enemy;
        }

        /// <summary>
        /// 지정 클래스와 위치로 검증용 아군을 초기화합니다.
        /// </summary>
        private AllyUnit CreateAlly(string className, Vector2 position)
        {
            var ally = CreateObject("WorkshopTestAlly").AddComponent<AllyUnit>();
            var definition = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>("Assets/02_Res/Data/Units/" + className + ".asset");
            ally.Initialize(definition, terrain, position);
            ally.AvailabilityChanged += combat.ReleaseUnavailableBlocker;
            allies.Add(ally);
            return ally;
        }

        #endregion // 함수
    }
}
