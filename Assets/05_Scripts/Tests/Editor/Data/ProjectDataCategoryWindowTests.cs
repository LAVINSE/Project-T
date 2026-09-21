using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using NUnit.Framework;

using ProjectT.Data;
using ProjectT.Editor.Data;

namespace ProjectT.Tests
{
    /// <summary>
    /// 종류별 창의 생성 경로와 서로 독립된 미적용 초안 복원을 검증합니다.
    /// </summary>
    public sealed class ProjectDataCategoryWindowTests
    {
        #region 필드
        private static readonly Type[] windowTypes =
        {
            typeof(AllyClassDataEditorWindow),
            typeof(EnemyDataEditorWindow),
            typeof(StageDataEditorWindow),
            typeof(EnemyRouteDataEditorWindow),
            typeof(UnitAppearanceDataEditorWindow),
            typeof(CurrencyDataEditorWindow),
            typeof(ItemDataEditorWindow)
        };
        private readonly Dictionary<string, string> previousState = new Dictionary<string, string>();
        private readonly List<ProjectDataEditorWindow> windows = new List<ProjectDataEditorWindow>();
        private readonly List<string> createdPaths = new List<string>();
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 사용자의 기존 창 선택과 초안을 보관하고 검증용 상태를 시작합니다.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            for (int kind = 0; kind < windowTypes.Length; kind++)
            {
                foreach (string suffix in new[] { "source", "draft", "snapshot" })
                {
                    string key = "ProjectT.DataEditor." + kind + "." + suffix;
                    previousState[key] = SessionState.GetString(key, string.Empty);
                    SessionState.SetString(key, string.Empty);
                }
            }
        }

        /// <summary>
        /// 검증에서 생성한 창과 자산만 정리하고 기존 사용자의 초안을 복원합니다.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var window in windows)
            {
                if (window != null)
                {
                    window.Close();
                }
            }

            foreach (string path in createdPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null)
                {
                    Undo.ClearUndo(asset);
                    AssetDatabase.DeleteAsset(path);
                }
            }

            foreach (var pair in previousState)
            {
                SessionState.SetString(pair.Key, pair.Value);
            }

            windows.Clear();
            createdPaths.Clear();
            previousState.Clear();
        }
        #endregion // 초기화

        #region 검증
        /// <summary>
        /// 전투·재화·아이템 창이 해당 데이터만 나열하고 검색 결과가 없어도 버튼으로 올바른 폴더에 생성합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryCategoryCreatesDataWithEmptySearchResults()
        {
            for (int kind = 0; kind < windowTypes.Length; kind++)
            {
                var window = OpenWindow(kind);
                yield return null;
                var list = window.rootVisualElement.Q<ListView>();
                if (kind < 6)
                {
                    Assert.That(list.itemsSource.Count, Is.GreaterThan(0));
                }
                Assert.That(list.itemsSource.Cast<ScriptableObject>().All(asset => ProjectDataCatalog.GetKind(asset) == kind), Is.True);
                int otherKind = (kind + 1) % 5;
                Assert.That(window.SelectAsset(ProjectDataCatalog.Find(otherKind)[0]), Is.False);
                window.rootVisualElement.Q<TextField>("dataSearch").value = "NoMatchingData_" + Guid.NewGuid().ToString("N");
                Assert.That(list.itemsSource.Count, Is.Zero);

                Submit(window.rootVisualElement.Q<Button>("createData"));
                var templateField = window.rootVisualElement.Q<ObjectField>("creationTemplate");
                if (templateField != null)
                {
                    Assert.That(templateField.objectType, Is.EqualTo(ProjectDataCatalog.SupportedTypes[kind]));
                }
                else
                {
                    Assert.That(kind, Is.GreaterThanOrEqualTo(5), "처음 만드는 재화·아이템만 기본 양식을 사용합니다.");
                }

                var template = templateField?.value as ScriptableObject;
                string before = template == null ? string.Empty : EditorJsonUtility.ToJson(template);
                string name = "CategoryCreation_" + Guid.NewGuid().ToString("N");
                string path = ProjectDataCatalog.GetFolder(ProjectDataCatalog.SupportedTypes[kind]) + "/" + name + ".asset";
                createdPaths.Add(path);
                window.rootVisualElement.Q<TextField>("newDataName").value = name;
                Submit(window.rootVisualElement.Q<Button>("confirmCreateData"));
                yield return null;

                var created = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                Assert.That(created, Is.Not.Null);
                Assert.That(ProjectDataCatalog.GetKind(created), Is.EqualTo(kind));
                Assert.That(ProjectDataValidation.Validate(created), Is.Empty);
                Assert.That(EditorUtility.IsDirty(created), Is.False);
                if (template != null)
                {
                    Assert.That(EditorJsonUtility.ToJson(template), Is.EqualTo(before));
                }
                Assert.That(window.rootVisualElement.Q<Button>("confirmCreateData"), Is.Null);
                window.Close();
            }
        }

        /// <summary>
        /// 클래스와 적 창을 각각 닫고 다시 열어도 초안이 섞이지 않고 원본 값이 유지됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator DifferentCategoryDraftsRestoreIndependently()
        {
            var first = OpenWindow(0);
            var second = OpenWindow(1);
            yield return null;
            Assert.That(first.SelectAsset(ProjectDataCatalog.Find(0)[0]), Is.True);
            Assert.That(second.SelectAsset(ProjectDataCatalog.Find(1)[0]), Is.True);
            yield return null;
            yield return null;
            var firstField = first.rootVisualElement.Q<VisualElement>("field-maximumHealth").Q<FloatField>();
            var secondField = second.rootVisualElement.Q<VisualElement>("field-maximumHealth").Q<FloatField>();
            float firstOriginal = firstField.value;
            float secondOriginal = secondField.value;
            firstField.value = firstOriginal + 37;
            secondField.value = secondOriginal + 53;
            yield return null;
            yield return null;
            first.Close();
            second.Close();

            first = OpenWindow(0);
            second = OpenWindow(1);
            yield return null;
            yield return null;
            firstField = first.rootVisualElement.Q<VisualElement>("field-maximumHealth").Q<FloatField>();
            secondField = second.rootVisualElement.Q<VisualElement>("field-maximumHealth").Q<FloatField>();
            Assert.That(firstField.value, Is.EqualTo(firstOriginal + 37));
            Assert.That(secondField.value, Is.EqualTo(secondOriginal + 53));
            Assert.That(((AllyClassDefinition)ProjectDataCatalog.Find(0)[0]).MaximumHealth, Is.EqualTo(firstOriginal));
            Assert.That(((EnemyDefinition)ProjectDataCatalog.Find(1)[0]).MaximumHealth, Is.EqualTo(secondOriginal));
        }
        #endregion // 검증

        #region 보조 함수
        /// <summary>
        /// 검증 대상 종류의 새로운 편집 창을 만들고 정리 대상으로 기록합니다.
        /// </summary>
        private ProjectDataEditorWindow OpenWindow(int kind)
        {
            var window = ScriptableObject.CreateInstance(windowTypes[kind]) as ProjectDataEditorWindow;
            windows.Add(window);
            window.Show();
            return window;
        }

        /// <summary>
        /// 사용자가 누르는 것과 같은 제출 이벤트로 활성화된 버튼을 실행합니다.
        /// </summary>
        private static void Submit(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.enabledInHierarchy, Is.True);
            button.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                button.SendEvent(submit);
            }
        }
        #endregion // 보조 함수
    }
}