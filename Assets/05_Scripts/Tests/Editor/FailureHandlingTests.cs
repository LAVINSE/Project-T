using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Deployment;
using ProjectT.Economy;
using ProjectT.Editor;
using ProjectT.Navigation;
using ProjectT.Presentation;
using ProjectT.Units;

namespace ProjectT.Tests
{
    /// <summary>
    /// 로그 기반 실패 처리에서 불완전한 생성과 기존 상태의 손상을 방지하는지 확인합니다.
    /// </summary>
    public sealed class FailureHandlingTests
    {
        #region 함수
        /// <summary>
        /// 잘못된 생성 입력은 사용 가능한 객체를 반환하지 않습니다.
        /// </summary>
        [Test]
        public void InvalidCreationReturnsNull()
        {
#if SW_DEBUG_MODE
            LogAssert.Expect(LogType.Warning, "[CombatHealth] 생성 실패: 최대 체력은 0보다 큰 유한한 수여야 합니다.");
#endif
            Assert.That(CombatHealth.Create(float.NaN), Is.Null);
            Assert.That(CombatHealth.Create(float.PositiveInfinity), Is.Null);
            Assert.That(BattleDeploymentWallet.Create(double.NaN), Is.Null);
            Assert.That(RouteProgress.Create(null), Is.Null);
            Assert.That(WorkshopObjective.Create(0, Vector2.zero), Is.Null);
            Assert.That(WorkshopObjective.Create(100, new Vector2(float.NaN, 0)), Is.Null);
            Assert.That(AllyDeploymentService.Create(null, null, null, null, null), Is.Null);
        }

        /// <summary>
        /// 아군 초기화 실패는 기존 생명·목적지·좌표를 유지합니다.
        /// </summary>
        [Test]
        public void FailedAllyInitializationPreservesExistingLifeAndMovement()
        {
            var battlefieldObject = new GameObject("FailureTestBattlefield");
            var allyObject = new GameObject("FailureTestAlly");
            try
            {
                var battlefield = battlefieldObject.AddComponent<WalkableBattlefield>();
                Assert.That(battlefield.Configure(new Rect(-5, -5, 10, 10), 0.5f, null), Is.True);
                var definition = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>("Assets/02_Res/Data/Units/Warrior.asset");
                var ally = allyObject.AddComponent<AllyUnit>();
                Assert.That(ally.Initialize(definition, battlefield, Vector2.zero), Is.True);
                ally.Health.TakeDamage(10);
                Assert.That(ally.TryMove(Vector2.right * 3), Is.True);
                CombatHealth originalHealth = ally.Health;
                Vector2 originalDestination = ally.RequestedDestination;
                Vector3 originalPosition = ally.transform.position;
                Assert.That(ally.Initialize(null, battlefield, Vector2.one), Is.False);
                Assert.That(ally.Initialize(definition, null, Vector2.one), Is.False);
                Assert.That(ally.Health, Is.SameAs(originalHealth));
                Assert.That(ally.Health.Current, Is.EqualTo(definition.MaximumHealth - 10));
                Assert.That(ally.RequestedDestination, Is.EqualTo(originalDestination));
                Assert.That(ally.transform.position, Is.EqualTo(originalPosition));
                Assert.That(ally.Movement.IsMoving, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(allyObject);
                Object.DestroyImmediate(battlefieldObject);
            }
        }

        /// <summary>
        /// 잘못된 새 공격은 이미 시작한 공격과 타격 시점을 바꾸지 않습니다.
        /// </summary>
        [Test]
        public void FailedAttackStartPreservesPendingImpact()
        {
            var health = CombatHealth.Create(100);
            var attack = new UnitAttackSequence();
            Assert.That(attack.Begin(1, 2, 2, 0.5f, Vector2.right, health), Is.True);
            Assert.That(attack.Begin(float.NaN, 1, 1, 0.5f, Vector2.zero, health), Is.False);
            Assert.That(attack.Begin(2, 1, 1, 0.5f, Vector2.zero, null), Is.False);
            Assert.That(attack.StartedAt, Is.EqualTo(1));
            Assert.That(attack.TargetPosition, Is.EqualTo(Vector2.right));
            Assert.That(attack.TryImpact(1.9f), Is.False);
            Assert.That(attack.TryImpact(2), Is.True);
            Assert.That(attack.TryImpact(2), Is.False);
        }

        /// <summary>
        /// 체력바 연결 실패는 기존 표시 대상과 체력 비율을 보존합니다.
        /// </summary>
        [Test]
        public void FailedHealthBarBindingPreservesPreviousOwner()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/04_Prefabs/Health/CommonHealthBar.prefab");
            var root = Object.Instantiate(prefab);
            try
            {
                var colors = AssetDatabase.LoadAssetAtPath<ColorData>("Assets/02_Res/Data/Common/ColorData.asset");
                var health = CombatHealth.Create(100);
                health.TakeDamage(40);
                var bar = root.GetComponent<HealthBarPresentation>();
                Assert.That(bar.Bind(health, colors, HealthBarPresentation.OwnerKind.Character, true), Is.True);
                Assert.That(bar.Bind(null, colors, HealthBarPresentation.OwnerKind.Enemy, true), Is.False);
                Assert.That(bar.Bind(CombatHealth.Create(200), null, HealthBarPresentation.OwnerKind.Enemy, true), Is.False);
                Assert.That(bar.Fraction, Is.EqualTo(0.6f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// 편집기 속성 묶음의 일부가 잘못되면 앞서 요청한 값도 적용하지 않습니다.
        /// </summary>
        [Test]
        public void InvalidSerializedBatchDoesNotPartiallyApply()
        {
            var definition = ScriptableObject.CreateInstance<StageDefinition>();
            try
            {
                Assert.That(StageOneGameplayBuilder.Set(definition, "displayName", "기존 이름"), Is.True);
                Assert.That(StageOneGameplayBuilder.Set(definition, "displayName", "변경 이름", "missingField", 1), Is.False);
                Assert.That(definition.DisplayName, Is.EqualTo("기존 이름"));
                Assert.That(StageOneGameplayBuilder.Set(definition, "displayName", "변경 이름", "spawnInterval", "잘못된 형식"), Is.False);
                Assert.That(definition.DisplayName, Is.EqualTo("기존 이름"));
                Assert.That(StageOneGameplayBuilder.Set(definition, "displayName"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        /// <summary>
        /// 필수 스프라이트가 누락되면 기존 외형 자산을 수정하지 않습니다.
        /// </summary>
        [Test]
        public void MissingSpritesPreserveExistingAppearance()
        {
            var appearance = AssetDatabase.LoadAssetAtPath<UnitAppearance>("Assets/02_Res/Data/Appearance/WarriorAppearance.asset");
            Assert.That(appearance, Is.Not.Null);
            string before = EditorJsonUtility.ToJson(appearance);
            const string missingPath = "Assets/MissingCodeStyleTestSprite.png";
            Assert.That(
                StageOneGameplayBuilder.Appearance("WarriorAppearance", missingPath, missingPath, missingPath, missingPath),
                Is.Null);
            Assert.That(EditorJsonUtility.ToJson(appearance), Is.EqualTo(before));
        }

        #endregion // 함수
    }
}
