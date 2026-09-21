using System;
using System.Collections;
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
    /// 실제 편집 창의 입력 바인딩과 적용 버튼이 원본 분리 규칙을 지키는지 검증합니다.
    /// </summary>
    public sealed class ProjectDataEditorWindowTests
    {
        #region 검증
        /// <summary>
        /// 사용 가능한 체력 입력이 초안에만 반영되고 적용 버튼에서만 원본으로 전달됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator BoundFieldEditsDraftAndButtonAppliesOriginal()
        {
            string[] keys = { "source", "draft", "snapshot" };
            string[] previous = new string[keys.Length];
            for (int index = 0; index < keys.Length; index++)
            {
                previous[index] = SessionState.GetString("ProjectT.DataEditor.0." + keys[index], string.Empty);
            }
            var template = AssetDatabase.LoadAssetAtPath<AllyClassDefinition>("Assets/02_Res/Data/Units/Warrior.asset");
            Assert.That(ProjectDataAssetService.TryCreate(template, "WindowVerification_" + Guid.NewGuid().ToString("N"),
                out ScriptableObject created, out string reason), Is.True, reason);
            string path = AssetDatabase.GetAssetPath(created);
            var window = ScriptableObject.CreateInstance<AllyClassDataEditorWindow>();
            try
            {
                window.Show();
                yield return null;
                Assert.That(window.SelectAsset(created), Is.True);
                yield return null;
                yield return null;
                var field = window.rootVisualElement.Q<VisualElement>("field-maximumHealth").Q<FloatField>();
                Assert.That(field, Is.Not.Null);
                Assert.That(field.enabledInHierarchy, Is.True, "실제 입력 필드가 읽기 전용이면 안 됩니다.");
                float original = ((AllyClassDefinition)created).MaximumHealth;
                field.value = original + 19;
                yield return null;
                yield return null;
                Assert.That(((AllyClassDefinition)created).MaximumHealth, Is.EqualTo(original));
                var button = window.rootVisualElement.Q<Button>("applyData");
                Assert.That(button.enabledInHierarchy, Is.True);
                button.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    button.SendEvent(submit);
                }
                yield return null;
                Assert.That(((AllyClassDefinition)created).MaximumHealth, Is.EqualTo(original + 19));
                Assert.That(EditorUtility.IsDirty(created), Is.False);
            }
            finally
            {
                window.Close();
                Undo.ClearUndo(created);
                AssetDatabase.DeleteAsset(path);
                for (int index = 0; index < keys.Length; index++)
                {
                    SessionState.SetString("ProjectT.DataEditor.0." + keys[index], previous[index]);
                }
            }
        }
        /// <summary>
        /// 적별 수량 덮어쓰기 입력이 원본 자산과 분리되고 적용 후에만 저장됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator RewardOverrideFieldEditsOnlyDraftUntilApply()
        {
            string[] keys = { "source", "draft", "snapshot" };
            string[] previous = new string[keys.Length];
            for (int index = 0; index < keys.Length; index++)
            {
                previous[index] = SessionState.GetString("ProjectT.DataEditor.1." + keys[index], string.Empty);
                SessionState.SetString("ProjectT.DataEditor.1." + keys[index], string.Empty);
            }

            var template = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/02_Res/Data/Units/Skeleton.asset");
            Assert.That(ProjectDataAssetService.TryCreate(template, "RewardWindow_" + Guid.NewGuid().ToString("N"),
                out ScriptableObject created, out string reason), Is.True, reason);
            string path = AssetDatabase.GetAssetPath(created);
            var enemy = (EnemyDefinition)created;
            var definition = enemy.Rewards[0].Definition;
            double originalAmount = definition.DefaultAmount;
            var window = ScriptableObject.CreateInstance<EnemyDataEditorWindow>();
            try
            {
                window.Show();
                yield return null;
                Assert.That(window.SelectAsset(created), Is.True);
                yield return null;
                yield return null;
                var toggle = window.rootVisualElement.Q<VisualElement>("field-rewards.Array.data[0].useAmountOverride").Q<Toggle>();
                var amountContainer = window.rootVisualElement.Q<VisualElement>("field-rewards.Array.data[0].overrideAmount");
                toggle.value = false;
                double deadline = EditorApplication.timeSinceStartup + 3d;
                while (amountContainer.enabledInHierarchy && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(amountContainer.enabledInHierarchy, Is.False);
                toggle.value = true;
                deadline = EditorApplication.timeSinceStartup + 3d;
                while (!amountContainer.enabledInHierarchy && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(amountContainer.enabledInHierarchy, Is.True);
                bool amountAppliedToDraft = false;
                amountContainer.Q<PropertyField>().RegisterCallback<SerializedPropertyChangeEvent>(change =>
                {
                    amountAppliedToDraft = change.changedProperty.doubleValue == originalAmount + 7d;
                });
                amountContainer.Q<DoubleField>().value = originalAmount + 7d;
                deadline = EditorApplication.timeSinceStartup + 3d;
                while (!amountAppliedToDraft && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(amountAppliedToDraft, Is.True, "직렬화 바인딩이 변경값을 초안에 전달해야 합니다.");
                Assert.That(enemy.Rewards[0].Amount, Is.EqualTo(10d));
                var button = window.rootVisualElement.Q<Button>("applyData");
                deadline = EditorApplication.timeSinceStartup + 3d;
                while (!button.enabledInHierarchy && EditorApplication.timeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(button.enabledInHierarchy, Is.True);
                button.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled())
                {
                    button.SendEvent(submit);
                }

                yield return null;
                Assert.That(enemy.Rewards[0].Amount, Is.EqualTo(originalAmount + 7d));
                Assert.That(definition.DefaultAmount, Is.EqualTo(originalAmount));
                Assert.That(EditorUtility.IsDirty(created), Is.False);
            }
            finally
            {
                window.Close();
                Undo.ClearUndo(created);
                AssetDatabase.DeleteAsset(path);
                for (int index = 0; index < keys.Length; index++)
                {
                    SessionState.SetString("ProjectT.DataEditor.1." + keys[index], previous[index]);
                }
            }
        }
        #endregion // 검증
    }
}