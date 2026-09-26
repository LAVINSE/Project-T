using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools.Util;
using SW.Stat;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 독립 데이터 창의 한글 입력, 목록 순서와 참조 탐색을 구성합니다.
    /// </summary>
    public sealed partial class DataEditorWindow
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
            var statTables = new Dictionary<string, VisualElement>();
            var iterator = session.Serialized.GetIterator();
            bool enter = true;
            while (iterator.NextVisible(enter))
            {
                enter = false;
                if (iterator.propertyPath == "m_Script")
                {
                    continue;
                }
                if (session.Draft is ItemData equipmentItem && equipmentItem.Equipment != null && iterator.propertyPath == "icon")
                {
                    detail.Add(Text("아이콘은 연결한 장비에서 설정합니다. 미등록이면 빈 아이콘으로 표시됩니다.", "project-data-description"));
                    continue;
                }
                if (session.Draft is ItemData pricedEquipmentItem && pricedEquipmentItem.Equipment != null
                    && (iterator.propertyPath == "sellPrice" || iterator.propertyPath == "sellCurrency"))
                {
                    if (iterator.propertyPath == "sellPrice")
                    {
                        detail.Add(Text("판매 재화와 금액은 연결한 장비에서 설정합니다. 현재 판매가 (1개): "
                            + ((double?)pricedEquipmentItem.SellPrice).ExToSellPriceText(pricedEquipmentItem.SellCurrency), "project-data-description"));
                    }
                    continue;
                }

                var description = DataCatalog.Describe(iterator.propertyPath);
                if (!groups.TryGetValue(description.Group, out Foldout group))
                {
                    group = new Foldout
                    {
                        text = description.Group,
                        value = true,
                        viewDataKey = "section-" + description.Group
                    };
                    group.AddToClassList("project-data-section");
                    groups.Add(description.Group, group);
                    InsertGroupInOrder(group, description.Group, groups);
                }

                if (session.Draft is EquipmentData && iterator.propertyPath == "rarity")
                {
                    group.Add(BuildEquipmentCategoryField(iterator.Copy(), description.Label, description.Description));
                }
                else if (session.Draft is ItemData && iterator.propertyPath == "performanceGrade")
                {
                    group.Add(BuildFilteredReference(iterator.Copy(), description.Label, description.Description, FindItemGradeChoices));
                }
                else if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String)
                {
                    group.Add(BuildArray(iterator.Copy(), description.Label, description.Description));
                }
                else if (iterator.propertyType == SerializedPropertyType.Generic && iterator.type == nameof(SWStatOverride))
                {
                    GetStatTable(group, statTables, description.Group)
                        .Add(BuildStatSetting(iterator.Copy(), description.Label, description.Description));
                }
                else
                {
                    group.Add(BuildField(iterator.Copy(), description.Label, description.Description));
                }
            }
        }

        /// <summary>
        /// 새 그룹을 정해진 표시 순서 자리에 넣습니다. 등록하지 않은 그룹은 뒤에 붙습니다.
        /// </summary>
        private void InsertGroupInOrder(Foldout group, string groupName, Dictionary<string, Foldout> groups)
        {
            int order = DataCatalog.GetGroupOrder(groupName);
            int position = detail.childCount;
            for (int index = 0; index < detail.childCount; index++)
            {
                if (detail[index] is Foldout existing
                    && groups.ContainsKey(existing.text)
                    && DataCatalog.GetGroupOrder(existing.text) > order)
                {
                    position = index;
                    break;
                }
            }

            detail.Insert(position, group);
        }

        /// <summary>
        /// 경유점을 경로 그림과 순서 표로 만듭니다. 좌표를 고치면 같은 화면의 그림을 다시 그립니다.
        /// </summary>
        private VisualElement BuildRouteArray(SerializedProperty property)
        {
            string path = property.propertyPath;
            var section = Element("project-data-array");
            fieldElements[path] = section;
            var preview = new RoutePreview(ReadRoutePoints());
            section.Add(preview);
            section.Add(Text("숫자는 이동 순서입니다. 첫 점이 입구, 마지막 점이 공방입니다.", "project-data-description"));

            var table = Element("project-data-stat-table");
            var header = Element("project-data-stat-row");
            header.AddToClassList("project-data-stat-header");
            header.Add(Text("순서", "project-data-route-order"));
            header.Add(Text("위치", "project-data-route-role"));
            header.Add(Text("좌표", "project-data-route-point"));
            header.Add(Text(string.Empty, "project-data-stat-tools"));
            table.Add(header);
            int last = property.arraySize - 1;
            for (int index = 0; index < property.arraySize; index++)
            {
                int position = index;
                var row = Element("project-data-stat-row");
                row.Add(Text((index + 1).ToString(), "project-data-route-order"));
                row.Add(Text(index == 0 ? "입구" : index == last ? "공방" : string.Empty, "project-data-route-role"));
                var point = new Vector2Field { value = property.GetArrayElementAtIndex(index).vector2Value };
                point.AddToClassList("project-data-route-point");
                point.RegisterValueChangedCallback(change =>
                {
                    ChangeRoutePoint(path, position, change.newValue);
                    preview.Present(ReadRoutePoints());
                });
                row.Add(point);
                var tools = Element("project-data-stat-tools");
                Button up = ActionButton("↑", () => ChangeArray(path, position, -1));
                up.tooltip = "위로 이동";
                up.SetEnabled(index > 0);
                tools.Add(up);
                Button down = ActionButton("↓", () => ChangeArray(path, position, 1));
                down.tooltip = "아래로 이동";
                down.SetEnabled(index < last);
                tools.Add(down);
                Button remove = ActionButton("−", () => ChangeArray(path, position, 0));
                remove.tooltip = "경유점 제거";
                tools.Add(remove);
                row.Add(tools);
                table.Add(row);
            }

            section.Add(table);
            var footer = Element("project-data-row");
            footer.Add(ActionButton("+ 경유점 추가", () => ChangeArray(path, -1, 0)));
            section.Add(footer);
            return section;
        }

        /// <summary>
        /// 편집용 복사본의 현재 경유점을 읽습니다. 경로 데이터가 아니면 빈 배열입니다.
        /// </summary>
        private Vector2[] ReadRoutePoints()
        {
            SerializedProperty property = session?.Serialized.FindProperty("points");
            if (property == null)
            {
                return Array.Empty<Vector2>();
            }

            var points = new Vector2[property.arraySize];
            for (int index = 0; index < points.Length; index++)
            {
                points[index] = property.GetArrayElementAtIndex(index).vector2Value;
            }

            return points;
        }

        /// <summary>
        /// 경유점 하나의 좌표를 편집용 복사본에 기록합니다. 항목이 없으면 아무것도 바꾸지 않습니다.
        /// </summary>
        private void ChangeRoutePoint(string path, int index, Vector2 value)
        {
            if (session == null)
            {
                return;
            }

            session.Serialized.Update();
            SerializedProperty points = session.Serialized.FindProperty(path);
            if (points == null || index < 0 || index >= points.arraySize)
            {
                return;
            }

            points.GetArrayElementAtIndex(index).vector2Value = value;
            session.Serialized.ApplyModifiedProperties();
            StoreDraft();
            RefreshStatus();
        }

        /// <summary>
        /// 스탯 목록을 머리글이 있는 표 하나로 만듭니다. 각 줄에서 순서 변경과 제거를 함께 제공합니다.
        /// </summary>
        private VisualElement BuildStatArray(SerializedProperty property)
        {
            string path = property.propertyPath;
            var section = Element("project-data-array");
            fieldElements[path] = section;
            var table = Element("project-data-stat-table");
            table.Add(BuildStatTableHeader());
            for (int index = 0; index < property.arraySize; index++)
            {
                int position = index;
                var controls = Element("project-data-array-controls");
                Button up = ActionButton("↑", () => ChangeArray(path, position, -1));
                up.tooltip = "위로 이동";
                up.SetEnabled(index > 0);
                controls.Add(up);
                Button down = ActionButton("↓", () => ChangeArray(path, position, 1));
                down.tooltip = "아래로 이동";
                down.SetEnabled(index + 1 < property.arraySize);
                controls.Add(down);
                Button remove = ActionButton("−", () => ChangeArray(path, position, 0));
                remove.tooltip = "항목 제거";
                controls.Add(remove);
                table.Add(BuildStatSetting(property.GetArrayElementAtIndex(index), string.Empty, string.Empty, controls));
            }

            section.Add(table);
            var footer = Element("project-data-row");
            footer.Add(ActionButton("+ 스탯 추가", () => ChangeArray(path, -1, 0)));
            section.Add(footer);
            section.Add(Text("여기에 넣은 스탯은 전투 규칙에 쓰이지 않고 결과 화면에만 표시합니다.", "project-data-description"));
            return section;
        }

        /// <summary>
        /// 그룹의 스탯 표를 가져옵니다. 아직 없으면 머리글과 함께 만들어 그룹에 넣습니다.
        /// </summary>
        private VisualElement GetStatTable(VisualElement group, Dictionary<string, VisualElement> tables, string groupName)
        {
            if (tables.TryGetValue(groupName, out VisualElement table))
            {
                return table;
            }

            table = Element("project-data-stat-table");
            table.Add(BuildStatTableHeader());
            tables.Add(groupName, table);
            group.Add(table);
            return table;
        }

        /// <summary>
        /// 원본이 아닌 편집용 속성에 입력 요소와 설명을 연결합니다.
        /// </summary>
        private VisualElement BuildField(SerializedProperty property, string title, string description)
        {
            string path = property.propertyPath;
            var container = Element("project-data-field");
            container.name = "field-" + path;
            UnityEngine.Object previousReference = property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue : null;
            var field = new PropertyField(property, title);
            field.BindProperty(property);
            field.RegisterCallback<SerializedPropertyChangeEvent>(change =>
            {
                RefreshStatus();
                if (path == "equipment" || path.EndsWith(".definition"))
                {
                    UnityEngine.Object currentReference = session?.Serialized.FindProperty(path)?.objectReferenceValue;
                    if (currentReference != previousReference)
                    {
                        previousReference = currentReference;
                        detail.schedule.Execute(RebuildDetail);
                    }
                }
            });
            container.Add(field);
            if (!string.IsNullOrEmpty(description))
            {
                field.tooltip = description;
                container.tooltip = description;
            }

            fieldElements[path] = container;
            return container;
        }

        /// <summary>
        /// 항목 형식에 맞는 전용 편집 화면을 만듭니다. 전용 화면이 없는 형식은 null입니다.
        /// </summary>
        private VisualElement BuildArrayEntry(SerializedProperty element)
        {
            switch (element.type)
            {
                case nameof(RewardEntry):
                    return BuildRewardEntry(element);
                case nameof(EquipmentStatBonus):
                    return BuildStatBonusEntry(element);
                case nameof(EquipmentPerformanceGrade):
                    return BuildEquipmentGradeEntry(element);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 배열 항목을 순서 변경·제거 버튼과 함께 편집용으로 만듭니다.
        /// </summary>
        private VisualElement BuildArray(SerializedProperty property, string label, string description)
        {
            string path = property.propertyPath;
            var section = new Foldout
            {
                text = label + " · " + property.arraySize + "개",
                value = true,
                viewDataKey = "array-" + path
            };
            section.AddToClassList("project-data-array");
            section.tooltip = description;
            fieldElements[path] = section;
            if (property.arrayElementType == nameof(SWStatOverride))
            {
                return BuildStatArray(property);
            }

            if (session.Draft is EnemyRouteData && path == "points")
            {
                return BuildRouteArray(property);
            }

            for (int index = 0; index < property.arraySize; index++)
            {
                int position = index;
                var element = property.GetArrayElementAtIndex(index);
                var row = Element("project-data-array-row");
                string title = "항목 " + (index + 1);
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
                VisualElement entry = BuildArrayEntry(element);
                if (entry != null)
                {
                    var header = Element("project-data-array-header");
                    header.Add(Text(title, "project-data-entry-title"));
                    header.Add(controls);
                    row.Add(header);
                    row.Add(entry);
                }
                else if (element.type == nameof(SWStatOverride))
                {
                    row.Add(BuildStatSetting(element, title, string.Empty));
                    row.Add(controls);
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
                section.Add(Text("배치 재화는 전투 지갑에, 소울·아이템은 영구 보관소에 자동 지급합니다.", "project-data-description"));
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
                    added.FindPropertyRelative("randomizeEquipmentGrade").boolValue = true;
                }
                else if (path == "performanceGrades")
                {
                    var added = property.GetArrayElementAtIndex(property.arraySize - 1);
                    added.FindPropertyRelative("performanceGrade").objectReferenceValue = null;
                    added.FindPropertyRelative("effect").objectReferenceValue = null;
                    added.FindPropertyRelative("selectionWeight").floatValue = 1f;
                    added.FindPropertyRelative("statOverrides").arraySize = 0;
                    added.FindPropertyRelative("additionalEffects").arraySize = 0;
                }
                else if (path == "statBonuses")
                {
                    var added = property.GetArrayElementAtIndex(property.arraySize - 1);
                    added.FindPropertyRelative("stat").objectReferenceValue = null;
                    added.FindPropertyRelative("amount").floatValue = 0f;
                }
                else if (property.GetArrayElementAtIndex(property.arraySize - 1).propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = null;
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
        /// 현재 편집 복사본을 유효성 검사 없이 저장합니다.
        /// </summary>
        private void ApplyCurrent()
        {
            ApplyCurrentData();
        }

        /// <summary>
        /// 저장 결과를 화면에 알립니다. 유효성 검사는 실행하지 않으며 실패 시 현재 편집 화면을 유지합니다.
        /// </summary>
        private bool ApplyCurrentData()
        {
            if (session == null)
            {
                return false;
            }

            bool success = session.TrySave(out notice);
            if (success)
            {
                issues.Clear();
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
        /// 사용자 요청으로 현재 편집값을 검사하고 결과를 표시합니다. 선택이 없거나 생성 중이면 실행하지 않습니다.
        /// </summary>
        private void ValidateCurrentData()
        {
            if (session?.Draft == null || creatingData)
            {
                return;
            }

            issues = DataCatalog.CollectIssues(session.Draft, session.Source);
            notice = "현재 편집값 검사: 오류 " + issues.Count + "개. 검사 결과와 관계없이 저장할 수 있습니다.";
            ShowPage("검사");
        }

        /// <summary>
        /// 선택 데이터와 전체 데이터의 검사 결과를 위치 이동 버튼으로 표시합니다. 버튼을 누르기 전에는 검사하지 않습니다.
        /// </summary>
        private void BuildValidation()
        {
            var row = Element("project-data-row");
            row.Add(ActionButton(
                "현재 편집값 검사",
                ValidateCurrentData,
                "validateCurrent"));
            row.Add(ActionButton(
                "전체 저장 데이터 검사",
                () =>
            {
                issues = new List<DataIssue>();
                var unique = new HashSet<string>();
                foreach (var asset in DataCatalog.Find())
                {
                    foreach (var issue in DataCatalog.CollectIssues(asset))
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
            detail.Add(Text("검사는 선택 사항이며 저장을 제한하지 않습니다. 오류 항목을 누르면 해당 입력 위치로 이동합니다. 검사로 값을 변경하지 않습니다.", "project-data-description"));
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
        private void FocusIssue(DataIssue issue)
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
        private DataEditorWindow NavigateToAsset(ScriptableObject asset)
        {
            if (!DataCatalog.TryGetKind(asset, out DataKind targetKind))
            {
                return null;
            }

            var window = targetKind == kind ? this : Open(targetKind);
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

            if (reference is ScriptableObject asset && DataCatalog.IsSupported(asset))
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
                    || !DataCatalog.IsSupported(reference))
                {
                    continue;
                }

                string path = iterator.propertyPath;
                var section = Element("project-data-reference");
                var usages = DataCatalog.FindUsages(reference);
                section.Add(Text(
                    DataCatalog.Describe(path).Label + " → " + reference.name + " · 사용처 " + usages.Count + "개",
                    "project-data-heading"));
                section.Add(ActionButton("참조 원본 열기", () => OpenReference(path)));
                section.Add(ActionButton("별도 복제 후 편집값에 연결", () => BeginReferenceCopy(path)));
                detail.Add(section);
            }

            detail.Add(Text("이 원본을 참조하는 사용처", "project-data-heading"));
            var sourceUsages = DataCatalog.FindUsages(session.Source);
            foreach (string path in sourceUsages)
            {
                string assetPath = path;
                var button = ActionButton(
                    path,
                    () =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (DataCatalog.IsSupported(asset))
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
