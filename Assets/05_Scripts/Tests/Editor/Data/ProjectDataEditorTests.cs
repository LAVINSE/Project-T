using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

using NUnit.Framework;

using ProjectT.Data;
using ProjectT.Editor.Data;

namespace ProjectT.Tests
{
    /// <summary>
    /// 편집 실패의 원본 보존과 저장·실행 취소·복제 참조의 경계를 검사합니다.
    /// </summary>
    public sealed class ProjectDataEditorTests
    {
        #region 필드
        private readonly List<string> createdPaths = new List<string>();
        private string testName;

        #endregion // 필드

        #region 준비와 정리
        /// <summary>
        /// 검사별로 원본과 겹치지 않는 생성 이름을 준비합니다.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            testName = "EditorVerification_" + Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 이 검사에서 만든 파일과 실행 취소 기록만 제거합니다.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (string path in createdPaths.AsEnumerable().Reverse())
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                {
                    Undo.ClearUndo(asset);
                }

                AssetDatabase.DeleteAsset(path);
            }

            createdPaths.Clear();
        }

        /// <summary>
        /// 유효한 원본 전사를 복제해 검사용 자산을 준비합니다.
        /// </summary>
        private AllyClassDefinition CreateAlly()
        {
            var template = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>("Assets/02_Res/Data/Units/Warrior.asset");
            Assert.That(
                ProjectDataAssetService.TryCreate(template, testName, out ScriptableObject created, out string reason),
                Is.True,
                reason);
            createdPaths.Add(AssetDatabase.GetAssetPath(created));
            return (AllyClassDefinition)created;
        }

        #endregion // 준비와 정리

        #region 원본 보존과 저장
        /// <summary>
        /// 현재 프로젝트의 대표 데이터가 제작 검사 규칙에 맞는지 확인합니다.
        /// </summary>
        [Test]
        public void ExistingDataPassesValidation()
        {
            foreach (var asset in ProjectDataCatalog.Find())
            {
                Assert.That(ProjectDataValidation.Validate(asset), Is.Empty, asset.name);
            }
        }

        /// <summary>
        /// 잘못된 수치가 있는 초안은 다른 올바른 변경도 원본에 부분 적용하지 않습니다.
        /// </summary>
        [Test]
        public void InvalidDraftPreservesAllOriginalValues()
        {
            var source = CreateAlly();
            string original = EditorJsonUtility.ToJson(source);
            string savedFile = File.ReadAllText(AssetDatabase.GetAssetPath(source));
            using (var session = ProjectDataEditSession.Create(source))
            {
                session.Serialized.FindProperty("deploymentCost").doubleValue = source.DeploymentCost + 30;
                session.Serialized.FindProperty("maximumHealth").floatValue = -1;
                session.Serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(session.TryApply(out var issues, out _), Is.False);
                Assert.That(issues.Any(issue => issue.PropertyPath == "maximumHealth"), Is.True);
                Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(original));
                Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(source)), Is.EqualTo(savedFile));
            }
        }

        /// <summary>
        /// 검사 후 적용한 복수 값이 한 번의 실행 취소와 다시 실행으로 복구됩니다.
        /// </summary>
        [Test]
        public void ApplyUndoRedoPreservesIdentityAndReferences()
        {
            var source = CreateAlly();
            Undo.ClearUndo(source);
            float originalHealth = source.MaximumHealth;
            double originalCost = source.DeploymentCost;
            string identifier = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            var appearance = source.Appearance;
            using (var session = ProjectDataEditSession.Create(source))
            {
                session.Serialized.FindProperty("maximumHealth").floatValue = originalHealth + 25;
                session.Serialized.FindProperty("deploymentCost").doubleValue = originalCost + 10;
                session.Serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(source.MaximumHealth, Is.EqualTo(originalHealth));
                Assert.That(session.TryApply(out _, out string reason), Is.True, reason);
                Assert.That(session.HasChanges, Is.False);
                Assert.That(source.MaximumHealth, Is.EqualTo(originalHealth + 25));
                Assert.That(source.hideFlags, Is.EqualTo(HideFlags.None));
                Undo.PerformUndo();
                Assert.That(source.MaximumHealth, Is.EqualTo(originalHealth));
                Assert.That(source.DeploymentCost, Is.EqualTo(originalCost));
                Undo.PerformRedo();
                Assert.That(source.MaximumHealth, Is.EqualTo(originalHealth + 25));
                Assert.That(source.DeploymentCost, Is.EqualTo(originalCost + 10));
                Assert.That(source.Appearance, Is.SameAs(appearance));
                Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)), Is.EqualTo(identifier));
                AssetDatabase.SaveAssetIfDirty(source);
                Assert.That(EditorUtility.IsDirty(source), Is.False);
            }
        }

        /// <summary>
        /// 다른 편집기에서 바뀐 원본을 오래된 초안으로 덮어쓰지 않습니다.
        /// </summary>
        [Test]
        public void ExternalChangeRejectsStaleDraft()
        {
            var source = CreateAlly();
            using (var session = ProjectDataEditSession.Create(source))
            {
                session.Serialized.FindProperty("maximumHealth").floatValue += 10;
                session.Serialized.ApplyModifiedPropertiesWithoutUndo();
                using (var external = new SerializedObject(source))
                {
                    external.FindProperty("maximumHealth").floatValue = 777;
                    external.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(session.HasExternalChanges, Is.True);
                Assert.That(session.TryApply(out _, out _), Is.False);
                Assert.That(source.MaximumHealth, Is.EqualTo(777));
            }
        }

        /// <summary>
        /// 창 재열기용 초안 복원이 원본을 변경하지 않고 참조를 유지합니다.
        /// </summary>
        [Test]
        public void RestoredDraftPreservesPendingValuesAndReferences()
        {
            var source = CreateAlly();
            var appearance = AssetDatabase.LoadAssetAtPath<UnitAppearance>("Assets/02_Res/Data/Appearance/MageAppearance.asset");
            var originalAppearance = source.Appearance;
            string draft;
            string snapshot;
            using (var session = ProjectDataEditSession.Create(source))
            {
                session.Serialized.FindProperty("appearance").objectReferenceValue = appearance;
                session.Serialized.ApplyModifiedPropertiesWithoutUndo();
                draft = EditorJsonUtility.ToJson(session.Draft);
                snapshot = session.SourceSnapshot;
            }

            using (var restored = ProjectDataEditSession.Create(source))
            {
                restored.RestoreDraft(draft, snapshot);
                Assert.That(restored.HasChanges, Is.True);
                Assert.That(restored.HasExternalChanges, Is.False);
                Assert.That(((AllyClassDefinition)restored.Draft).Appearance, Is.SameAs(appearance));
                Assert.That(source.Appearance, Is.SameAs(originalAppearance));
            }
        }

        #endregion // 원본 보존과 저장

        #region 생성과 시험
        /// <summary>
        /// 동일 이름 복제는 별도 파일과 식별자를 만들고 하위 참조만 공유합니다.
        /// </summary>
        [Test]
        public void DuplicateUsesUniqueFileAndSharesAppearance()
        {
            var first = CreateAlly();
            var second = CreateAlly();
            Assert.That(AssetDatabase.GetAssetPath(first), Is.Not.EqualTo(AssetDatabase.GetAssetPath(second)));
            Assert.That(first.Appearance, Is.SameAs(second.Appearance));
            Assert.That(ProjectDataAssetService.TryCreate(first, "../Invalid", out var rejected, out _), Is.False);
            Assert.That(rejected, Is.Null);
        }

        /// <summary>
        /// 자산 생성도 실행 취소와 다시 실행에서 파일과 참조를 복원합니다.
        /// </summary>
        [Test]
        public void CreatedAssetCanBeUndoneAndRedone()
        {
            var source = CreateAlly();
            string path = AssetDatabase.GetAssetPath(source);
            string appearancePath = AssetDatabase.GetAssetPath(source.Appearance);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(AssetDatabase.LoadAssetAtPath<AllyClassDefinition>(path), Is.Null);
            Undo.PerformRedo();
            var restored = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>(path);
            Assert.That(restored, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(restored.Appearance), Is.EqualTo(appearancePath));
        }

        /// <summary>
        /// 참조 누락과 경로 중복의 정확한 수정 위치를 알려 줍니다.
        /// </summary>
        [Test]
        public void ValidationReportsNestedReferenceAndRouteLocation()
        {
            var route = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyRouteDefinition>("Assets/02_Res/Data/Navigation/Stage01EnemyRoute.asset"));
            try
            {
                using (var serialized = new SerializedObject(route))
                {
                    var points = serialized.FindProperty("points");
                    points.GetArrayElementAtIndex(1).vector2Value = points.GetArrayElementAtIndex(0).vector2Value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(
                    ProjectDataValidation.Validate(route).Any(issue => issue.PropertyPath == "points.Array.data[1]"),
                    Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(route);
            }
        }

        /// <summary>
        /// 시험 복사본은 선택한 수치를 반영하고 원본 전투 데이터와 열린 장면을 보존합니다.
        /// </summary>
        [Test]
        public void TrialCopiesDataGraphAndPreservesOpenScenes()
        {
            var selected = CreateAlly();
            using (var serialized = new SerializedObject(selected))
            {
                serialized.FindProperty("maximumHealth").floatValue = 456;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/02_Res/Data/Stage/Stage01.asset");
            string original = EditorJsonUtility.ToJson(stage);
            int sceneCount = SceneManager.sceneCount;
            string activeScene = SceneManager.GetActiveScene().path;
            Assert.That(
                ProjectDataTrialService.TryCreate(stage, selected, null, "Assets/01_Scenes/Stage01_Grassland.unity", out string path, out string reason),
                Is.True,
                reason);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            createdPaths.Add(folder);
            var trial = AssetDatabase.LoadAssetAtPath<StageDefinition>(folder + "/Stage01.asset");
            Assert.That(trial, Is.Not.Null);
            Assert.That(trial.Classes.Last().MaximumHealth, Is.EqualTo(456));
            Assert.That(trial.Classes.Last(), Is.Not.SameAs(selected));
            Assert.That(AssetDatabase.GetAssetPath(trial.Enemy), Does.StartWith(folder + "/"));
            Assert.That(AssetDatabase.GetAssetPath(trial.EnemyRoute), Does.StartWith(folder + "/"));
            foreach (var ally in trial.Classes)
            {
                Assert.That(AssetDatabase.GetAssetPath(ally.Appearance), Does.StartWith(folder + "/"));
            }

            Assert.That(EditorJsonUtility.ToJson(stage), Is.EqualTo(original));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(activeScene));
        }

        #endregion // 생성과 시험
    }
}
