using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools.Util;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 독립 데이터 창의 한글 입력, 목록 순서와 참조 탐색을 구성합니다.
    /// </summary>
    public partial class ProjectDataEditorWindow
    {
        #region 편집 필드
        /// <summary>
        /// 직렬화된 실제 항목만 그룹별로 편집용 복사본에 연결합니다.
        /// </summary>
        private void BuildFields()
        {
            if (session.HasExternalChanges)
            {
                detail.Add(new HelpBox("다른 창 또는 실행 취소에서 원본이 변경되었습니다. 되돌리기를 누르면 최신 원본을 다시 읽습니다.", HelpBoxMessageType.Warning));
            }

            var groups = new Dictionary<string, Foldout>();
            var iterator = session.Serialized.GetIterator();
            bool enter = true;
            while (iterator.NextVisible(enter))
            {
                enter = false;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                string[] description = ProjectDataCatalog.Describe(iterator.propertyPath);
                if (!groups.TryGetValue(description[2], out Foldout group))
                {
                    group = new Foldout
                    {
                        text = description[2],
                        value = true,
                        viewDataKey = "section-" + description[2]
                    };
                    group.AddToClassList("project-data-section");
                    groups.Add(description[2], group);
                    detail.Add(group);
                }

                if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String)
                {
                    group.Add(BuildArray(iterator.Copy(), description));
                }
                else
                {
                    group.Add(BuildField(iterator.Copy(), description[0], description[1]));
                }
            }
        }

        /// <summary>
        /// 원본이 아닌 편집용 속성에 입력 요소와 설명을 연결합니다.
        /// </summary>
        private VisualElement BuildField(SerializedProperty property, string title, string description)
        {
            string path = property.propertyPath;
            var container = Element("project-data-field");
            container.name = "field-" + path;
            var field = new PropertyField(property, title);
            field.BindProperty(property);
            field.RegisterCallback<SerializedPropertyChangeEvent>(change => RefreshStatus());
            container.Add(field);
            if (!string.IsNullOrEmpty(description))
            {
                field.tooltip = description;
                container.tooltip = description;
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                container.AddToClassList("project-data-reference-field");
                var menu = new ToolbarMenu
                {
                    text = "⋯",
                    tooltip = "참조 열기 · 별도 복제"
                };
                menu.AddToClassList("project-data-reference-menu");
                menu.menu.AppendAction(
                    "참조 열기",
                    action => OpenReference(path),
                    action => session?.Serialized.FindProperty(path)?.objectReferenceValue != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
                menu.menu.AppendAction(
                    "참조 별도 복제",
                    action => BeginReferenceCopy(path),
                    action => !EditorApplication.isPlayingOrWillChangePlaymode
                    && ProjectDataCatalog.GetKind(session?.Serialized.FindProperty(path)?.objectReferenceValue as ScriptableObject) >= 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
                container.Add(menu);
            }

            fieldElements[path] = container;
            return container;
        }

        /// <summary>
        /// 목록 추가·제거·순서 변경을 한글 조작으로 제공합니다.
        /// </summary>
        private VisualElement BuildArray(SerializedProperty property, string[] description)
        {
            string path = property.propertyPath;
            var section = new Foldout
            {
                text = description[0] + " · " + property.arraySize + "개",
                value = true,
                viewDataKey = "array-" + path
            };
            section.AddToClassList("project-data-array");
            section.tooltip = description[1];
            fieldElements[path] = section;
            for (int index = 0; index < property.arraySize; index++)
            {
                int position = index;
                var element = property.GetArrayElementAtIndex(index);
                var row = Element("project-data-array-row");
                string title = path.EndsWith("Frames") ? "프레임 " + index : "항목 " + (index + 1);
                var controls = Element("project-data-array-controls");
                var up = ActionButton("↑", () => ChangeArray(path, position, -1));
                up.tooltip = "위로 이동";
                up.SetEnabled(index > 0);
                controls.Add(up);
                var down = ActionButton("↓", () => ChangeArray(path, position, 1));
                down.tooltip = "아래로 이동";
                down.SetEnabled(index + 1 < property.arraySize);
                controls.Add(down);
                var remove = ActionButton("−", () => ChangeArray(path, position, 0));
                remove.tooltip = "항목 제거";
                controls.Add(remove);
                if (path == "rewards")
                {
                    var header = Element("project-data-array-header");
                    header.Add(Text(title, "project-data-entry-title"));
                    header.Add(controls);
                    row.Add(header);
                    row.Add(BuildRewardEntry(element));
                }
                else
                {
                    row.AddToClassList("project-data-array-inline");
                    row.Add(BuildField(element, title, string.Empty));
                    row.Add(controls);
                }

                section.Add(row);
            }

            var footer = Element("project-data-row");
            footer.Add(ActionButton("+ 항목 추가", () => ChangeArray(path, -1, 0)));
            section.Add(footer);
            if (path == "rewards")
            {
                section.Add(Text("각 항목은 독립 판정합니다. 100%는 확정 지급입니다.", "project-data-description"));
                section.Add(Text("현재 배치 재화만 지급되며, 아이템·다른 재화는 계산만 지원합니다.", "project-data-description"));
            }

            return section;
        }

        /// <summary>
        /// 목록 편집을 복사본에만 적용합니다. 객체 참조 목록 삭제도 정확히 한 항목을 제거합니다.
        /// </summary>
        private void ChangeArray(string path, int index, int direction)
        {
            session.Serialized.Update();
            var property = session.Serialized.FindProperty(path);
            if (index < 0)
            {
                property.InsertArrayElementAtIndex(property.arraySize);
                if (path == "rewards")
                {
                    var added = property.GetArrayElementAtIndex(property.arraySize - 1);
                    added.FindPropertyRelative("definition").objectReferenceValue = null;
                    added.FindPropertyRelative("useAmountOverride").boolValue = false;
                    added.FindPropertyRelative("overrideAmount").doubleValue = 1d;
                    added.FindPropertyRelative("acquisitionProbability").floatValue = 100f;
                }
            }
            else if (direction == 0)
            {
                int previousCount = property.arraySize;
                property.DeleteArrayElementAtIndex(index);
                if (previousCount == property.arraySize)
                {
                    property.DeleteArrayElementAtIndex(index);
                }
            }
            else
            {
                property.MoveArrayElement(index, index + direction);
            }

            session.Serialized.ApplyModifiedProperties();
            RebuildDetail();
        }

        #endregion // 편집 필드

        #region 적용과 검사
        /// <summary>
        /// 현재 편집 복사본을 검사하고 원본에 적용합니다.
        /// </summary>
        private void ApplyCurrent()
        {
            ApplyCurrentData();
        }

        /// <summary>
        /// 적용 결과를 화면에 알립니다. 실패하면 오류 페이지로 이동하고 원본을 유지합니다.
        /// </summary>
        private bool ApplyCurrentData()
        {
            if (session == null)
            {
                return false;
            }

            bool success = session.TryApply(out issues, out notice);
            if (!success && issues.Count > 0)
            {
                currentPage = "검사";
            }

            StoreDraft();
            RebuildDetail();
            RefreshCatalog();
            return success;
        }

        /// <summary>
        /// 미적용 값만 버리고 원본을 다시 읽습니다. 입력이 있으면 버릴지 확인합니다.
        /// </summary>
        private void CancelCurrent()
        {
            if (session == null
                || (session.HasChanges && !EditorUtility.DisplayDialog("수정 취소", "미적용 값을 버리고 최신 원본을 다시 읽을까요?", "수정 버리기", "계속 편집")))
            {
                return;
            }

            detail.Unbind();
            session.Reload();
            issues.Clear();
            notice = "최신 원본을 다시 읽었습니다.";
            StoreDraft();
            RebuildDetail();
        }

        /// <summary>
        /// 선택 데이터와 전체 데이터의 검사 결과를 위치 이동 버튼으로 표시합니다.
        /// </summary>
        private void BuildValidation()
        {
            var row = Element("project-data-row");
            row.Add(ActionButton(
                "현재 편집값 검사",
                () =>
            {
                issues = ProjectDataValidation.Validate(session.Draft);
                notice = "현재 편집값 검사: 오류 " + issues.Count + "개";
                RebuildDetail();
            },
                "validateCurrent"));
            row.Add(ActionButton(
                "전체 저장 데이터 검사",
                () =>
            {
                issues = new List<ProjectDataIssue>();
                var unique = new HashSet<string>();
                foreach (var asset in ProjectDataCatalog.Find())
                {
                    foreach (var issue in ProjectDataValidation.Validate(asset))
                    {
                        string key = AssetDatabase.GetAssetPath(issue.Asset) + "|" + issue.PropertyPath + "|" + issue.Message;
                        if (unique.Add(key))
                        {
                            issues.Add(issue);
                        }
                    }
                }

                notice = "전체 저장 데이터 검사: 오류 " + issues.Count + "개. 미적용 초안은 별도 검사합니다.";
                RebuildDetail();
            }));
            detail.Add(row);
            detail.Add(Text("오류 항목을 누르면 해당 데이터와 입력 위치로 이동합니다. 검사는 값을 자동 수정하지 않습니다.", "project-data-description"));
            foreach (var issue in issues)
            {
                var current = issue;
                var button = ActionButton(
                    (issue.Asset == null ? "데이터" : issue.Asset.name) + " / " + issue.PropertyPath + "\n" + issue.Message,
                    () => FocusIssue(current));
                button.AddToClassList("project-data-message");
                detail.Add(button);
            }

            if (issues.Count == 0)
            {
                detail.Add(Text("표시할 오류가 없습니다. 검사 버튼으로 현재 상태를 확인하세요.", "project-data-description"));
            }
        }

        /// <summary>
        /// 오류 원본 또는 초안으로 이동한 뒤 관련 입력을 표시합니다.
        /// </summary>
        private void FocusIssue(ProjectDataIssue issue)
        {
            if (issue.Asset != session.Draft && issue.Asset != session.Source && issue.Asset != null)
            {
                var window = NavigateToAsset(issue.Asset);
                if (window != null)
                {
                    window.FocusProperty(issue.PropertyPath);
                }

                return;
            }

            FocusProperty(issue.PropertyPath);
        }

        /// <summary>
        /// 편집 화면에서 오류가 발생한 입력 위치를 찾아 표시합니다. 없는 속성이면 화면만 엽니다.
        /// </summary>
        private void FocusProperty(string path)
        {
            currentPage = "편집";
            RebuildDetail();
            if (fieldElements.TryGetValue(path, out VisualElement field))
            {
                detail.schedule.Execute(() =>
                {
                    for (VisualElement ancestor = field; ancestor != null && ancestor != detail; ancestor = ancestor.parent)
                    {
                        if (ancestor is Foldout foldout)
                        {
                            foldout.value = true;
                        }
                    }

                    detail.schedule.Execute(() =>
                    {
                        detailScroll.ScrollTo(field);
                        field.Q<PropertyField>()?.Focus();
                    });
                });
            }
        }

        #endregion // 적용과 검사

        #region 참조
        /// <summary>
        /// 같은 종류는 현재 창에서 열고 다른 종류는 해당 전용 창에서 엽니다. 이동을 취소하면 null을 반환합니다.
        /// </summary>
        private ProjectDataEditorWindow NavigateToAsset(ScriptableObject asset)
        {
            int targetKind = ProjectDataCatalog.GetKind(asset);
            if (targetKind < 0)
            {
                return null;
            }

            var window = DataKind < 0 || DataKind == targetKind ? this : OpenKind(targetKind);
            return window != null && window.SelectAsset(asset) ? window : null;
        }

        /// <summary>
        /// 연결 자산을 열거나 지원하지 않는 그림 등은 Project 창에서 표시합니다.
        /// </summary>
        private void OpenReference(string path)
        {
            var property = session.Serialized.FindProperty(path);
            var reference = property?.objectReferenceValue;
            if (reference == null)
            {
                notice = "연결된 참조가 없습니다.";
                return;
            }

            if (reference is ScriptableObject asset && ProjectDataCatalog.GetKind(asset) >= 0)
            {
                NavigateToAsset(asset);
            }
            else
            {
                SWEditorUtils.PingAndSelect(reference);
            }
        }

        /// <summary>
        /// 직접 연결과 원본 사용처를 표시하며 참조 공유 범위를 알려 줍니다.
        /// </summary>
        private void BuildReferences()
        {
            detail.Add(Text("연결된 데이터", "project-data-heading"));
            detail.Add(Text("참조 자산을 직접 수정하면 이를 사용하는 다른 데이터에도 반영됩니다. 독립 변형은 참조 별도 복제를 사용하세요.", "project-data-description"));
            var iterator = session.Serialized.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference
                    || iterator.objectReferenceValue == null
                    || iterator.propertyPath == "m_Script")
                {
                    continue;
                }

                if (!(iterator.objectReferenceValue is ScriptableObject reference)
                    || ProjectDataCatalog.GetKind(reference) < 0)
                {
                    continue;
                }

                string path = iterator.propertyPath;
                var section = Element("project-data-reference");
                var usages = ProjectDataCatalog.FindUsages(reference);
                section.Add(Text(
                    ProjectDataCatalog.Describe(path)[0] + " → " + reference.name + " · 사용처 " + usages.Count + "개",
                    "project-data-heading"));
                section.Add(ActionButton("참조 원본 열기", () => OpenReference(path)));
                section.Add(ActionButton("별도 복제 후 편집값에 연결", () => BeginReferenceCopy(path)));
                detail.Add(section);
            }

            detail.Add(Text("이 원본을 참조하는 사용처", "project-data-heading"));
            var sourceUsages = ProjectDataCatalog.FindUsages(session.Source);
            foreach (string path in sourceUsages)
            {
                string assetPath = path;
                var button = ActionButton(
                    path,
                    () =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (ProjectDataCatalog.GetKind(asset) >= 0)
                    {
                        NavigateToAsset(asset);
                    }
                    else
                    {
                        SWEditorUtils.PingAssetAtPath(assetPath);
                    }
                });
                button.AddToClassList("project-data-message");
                detail.Add(button);
            }

            if (sourceUsages.Count == 0)
            {
                detail.Add(Text("관리 데이터·장면·프리팹에서 직접 참조하는 사용처가 없습니다.", "project-data-description"));
            }
        }

        #endregion // 참조
    }
}
