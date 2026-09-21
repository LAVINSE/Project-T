using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 복수 보상의 원본 참조·개별 수량·독립 확률 입력과 계산 요약을 제공합니다.
    /// </summary>
    public partial class ProjectDataEditorWindow
    {
        #region 보상 입력
        /// <summary>
        /// 보상 한 항목을 한글 입력으로 표시합니다. 개별 수량이 꺼져 있으면 원본 기본값을 보여 줍니다.
        /// </summary>
        private VisualElement BuildRewardEntry(SerializedProperty entry)
        {
            string path = entry.propertyPath;
            var container = Element("project-data-field");
            container.name = "field-" + path;
            fieldElements[path] = container;
            container.Add(BuildField(entry.FindPropertyRelative("definition"), "재화·아이템", "공유하는 보상 원본입니다. 지급량은 아래에서 개별 설정할 수 있습니다."));
            var useOverride = entry.FindPropertyRelative("useAmountOverride");
            var toggle = BuildField(useOverride, "수량 덮어쓰기", "켜면 이 적의 보상 항목에만 개별 수량을 사용합니다.");
            container.Add(toggle);
            var amount = BuildField(entry.FindPropertyRelative("overrideAmount"), "개별 수량", "0 이상의 수량입니다. 원본 기본 수량은 변경하지 않습니다.");
            amount.SetEnabled(useOverride.boolValue);
            toggle.Q<PropertyField>().RegisterCallback<SerializedPropertyChangeEvent>(change =>
            {
                var current = session?.Serialized.FindProperty(path + ".useAmountOverride");
                amount.SetEnabled(current != null && current.boolValue);
            });
            container.Add(amount);
            container.Add(BuildField(entry.FindPropertyRelative("acquisitionProbability"), "획득 확률 (%)", "다른 항목과 독립적으로 판정합니다. 100은 항상 지급하고 0은 지급하지 않습니다."));
            var summary = Text(string.Empty, "project-data-description");
            container.Add(summary);
            container.schedule.Execute(() => RefreshRewardSummary(path, summary)).Every(300);
            RefreshRewardSummary(path, summary);
            return container;
        }

        /// <summary>
        /// 실제 적용 수량과 확률을 표시합니다. 미연결 항목은 참조 입력을 안내합니다.
        /// </summary>
        private void RefreshRewardSummary(string path, Label summary)
        {
            if (session?.Draft == null)
            {
                return;
            }

            var entry = session.Serialized.FindProperty(path);
            if (entry == null)
            {
                return;
            }

            var definition = entry.FindPropertyRelative("definition").objectReferenceValue as RewardDefinition;
            if (definition == null)
            {
                summary.text = "재화 또는 아이템 자산을 연결하세요.";
                return;
            }

            bool overridden = entry.FindPropertyRelative("useAmountOverride").boolValue;
            double amount = overridden ? entry.FindPropertyRelative("overrideAmount").doubleValue : definition.DefaultAmount;
            summary.text = definition.DisplayName + " · 적용 수량 " + amount + (overridden ? " (개별 설정)" : " (원본 기본값)")
                + " · 획득 확률 " + entry.FindPropertyRelative("acquisitionProbability").floatValue + "%";
        }
        #endregion // 보상 입력
    }
}