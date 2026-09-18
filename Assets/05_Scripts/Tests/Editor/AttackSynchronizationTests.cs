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
    /// 공격 그림의 타격 시각과 실제 피해, 취소 및 대상 생명 교체를 검증합니다.
    /// </summary>
    public sealed class AttackSynchronizationTests
    {
        #region 함수
        /// <summary>
        /// 공격 준비 중에는 피해가 없고 지정 프레임에서 한 번만 피해가 발생합니다.
        /// </summary>
        [TestCase("Warrior", 2, 11)]
        [TestCase("Mage", 9, 18)]
        public void DamageOccursOnceAtConfiguredAnimationFrame(
            string className,
            int impactFrame,
            int frameCount)
        {
            var allyObject = new GameObject("Attacker");
            var enemyObject = new GameObject("Target");
            var terrainObject = new GameObject("Battlefield");
            try
            {
                var terrain = terrainObject.AddComponent<WalkableBattlefield>();
                terrain.Configure(new Rect(-5, -5, 10, 10), 0.5f, null);
                var ally = allyObject.AddComponent<AllyUnit>();
                var enemy = enemyObject.AddComponent<EnemyUnit>();
                var definition = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>("Assets/02_Res/Data/Units/" + className + ".asset");
                ally.Initialize(definition, terrain, Vector2.zero);
                enemy.Initialize(
                    AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/02_Res/Data/Units/Skeleton.asset"),
                    FixedRoute.Create(new[] { new Vector2(0.8f, 0), new Vector2(4, 0) }));
                var combat = new BattleCombatSystem(
                    new List<AllyUnit> { ally },
                    new List<EnemyUnit> { enemy },
                    WorkshopObjective.Create(300, new Vector2(4, 0)));
                Assert.That(definition.Appearance.AttackImpactRatio, Is.EqualTo((float)impactFrame / frameCount).Within(0.0001f));
                combat.Tick(0f);
                Assert.That(enemy.Health.Current, Is.EqualTo(enemy.Health.Maximum));
                float impact = ally.Attack.ImpactAt;
                combat.Tick(impact - 0.001f);
                Assert.That(enemy.Health.Current, Is.EqualTo(enemy.Health.Maximum));
                combat.Tick(impact);
                Assert.That(enemy.Health.Current, Is.EqualTo(enemy.Health.Maximum - definition.AttackDamage));
                combat.Tick(impact);
                Assert.That(enemy.Health.Current, Is.EqualTo(enemy.Health.Maximum - definition.AttackDamage));
                Assert.That(ally.Attack.IsPlaying(impact + 0.001f), Is.True, "타격 뒤 회복 프레임도 끝까지 재생해야 합니다.");
                combat.Tick(definition.AttackInterval + 0.01f);
                Assert.That(ally.TryMove(new Vector2(-3, 0)), Is.True);
                float health = enemy.Health.Current;
                combat.Tick(ally.Attack.ImpactAt + 0.01f);
                Assert.That(enemy.Health.Current, Is.EqualTo(health), "이동 명령은 준비 중인 타격을 취소해야 합니다.");
            }
            finally
            {
                Object.DestroyImmediate(allyObject);
                Object.DestroyImmediate(enemyObject);
                Object.DestroyImmediate(terrainObject);
            }
        }

        /// <summary>
        /// 풀에서 다시 생성하거나 부활한 대상에게 이전 생명을 겨냥한 공격이 전달되지 않습니다.
        /// </summary>
        [Test]
        public void PendingAttackRejectsReusedAndRevivedLife()
        {
            var health = CombatHealth.Create(100);
            var attack = new UnitAttackSequence();
            attack.Begin(0, 1, 1, 0.5f, Vector2.right, health);
            Assert.That(attack.MatchesTarget(CombatHealth.Create(100)), Is.False);
            health.TakeDamage(100);
            health.Revive();
            Assert.That(attack.MatchesTarget(health), Is.False);
            attack.Cancel();
            Assert.That(attack.TryImpact(0.5f), Is.False);
            Assert.That(attack.CanStart(0.5f), Is.False);
            Assert.That(attack.CanStart(1), Is.True);
        }

        #endregion // 함수
    }
}
