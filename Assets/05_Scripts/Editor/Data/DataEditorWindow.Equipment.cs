using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using ProjectT.Data;
using SW.Stat;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 장비의 성능 등급·스탯·효과 참조를 기존 편집 세션에 연결합니다. 생성과 관리는 각 데이터 탭에서 담당합니다.
    /// </summary>
    public sealed partial class DataEditorWindow
    {
        #region 장비 입력
        /// <summary>
        /// 스탯 설정 한 개를 표의 한 줄로 만듭니다. 정의 연결·기본값·개별값·최종값을 같은 줄에서 비교합니다.
        /// 이름을 비우면 연결한 스탯의 표시 이름을 사용하고, 추가 버튼은 오른쪽 도구 칸에 넣습니다.
        /// </summary>
        private VisualElement BuildStatSetting(SerializedProperty property, string title, string description, VisualElement extraTools = null)
        {
            string path = property.propertyPath;
            bool useStatName = string.IsNullOrEmpty(title);
            var row = Element("project-data-stat-row");
            row.tooltip = description;
            fieldElements[path] = row;
            DataEditSession owner = session;
            var name = Text(title, "project-data-stat-name");
            name.tooltip = title;
            var link = BuildStatLink(path, description);
            var shared = Text(string.Empty, "project-data-stat-shared");
            var controls = Element("project-data-stat-override");
            var useToggle = new Toggle { tooltip = "개별 기본값으로 스탯 정의의 기본값을 대체합니다." };
            useToggle.AddToClassList("project-data-stat-toggle");
            var amountField = new FloatField { isDelayed = true, tooltip = "이 데이터의 시작값입니다." };
            amountField.AddToClassList("project-data-stat-amount");
            controls.Add(useToggle);
            controls.Add(amountField);
            var result = Text(string.Empty, "project-data-stat-result");
            var tools = Element("project-data-stat-tools");
            Button pingStat = ActionButton("◎", () => PingReference(path + ".stat"));
            pingStat.tooltip = "연결한 스탯 자산을 Project 창에서 표시합니다.";
            tools.Add(pingStat);
            Button editStat = ActionButton("✎", () => OpenReference(path + ".stat"));
            editStat.tooltip = "연결한 스탯 자산을 편집합니다.";
            tools.Add(editStat);
            if (extraTools != null)
            {
                tools.Add(extraTools);
            }

            Action refresh = () =>
            {
                SerializedProperty current = owner?.Serialized.FindProperty(path);
                var stat = current?.FindPropertyRelative("stat")?.objectReferenceValue as SWStat;
                bool used = current?.FindPropertyRelative("isUseOverride")?.boolValue ?? false;
                float amount = current?.FindPropertyRelative("overrideDefaultValue")?.floatValue ?? 0f;
                if (useStatName)
                {
                    name.text = stat == null ? "미연결 스탯" : stat.DisplayName;
                    name.tooltip = name.text;
                }

                shared.text = stat == null ? "-" : FormatStatValue(stat, stat.DefaultValue);
                useToggle.SetValueWithoutNotify(used);
                useToggle.SetEnabled(stat != null);
                amountField.SetValueWithoutNotify(used || stat == null ? amount : stat.DefaultValue);
                amountField.SetEnabled(stat != null && used);
                result.text = stat == null ? "미연결" : FormatStatValue(stat, used ? amount : stat.DefaultValue);
                row.EnableInClassList("project-data-stat-excluded", stat == null);
            };

            useToggle.RegisterValueChangedCallback(change =>
            {
                ChangeStatSetting(path, "isUseOverride", target => target.boolValue = change.newValue);
                refresh();
            });
            amountField.RegisterValueChangedCallback(change =>
            {
                ChangeStatSetting(path, "overrideDefaultValue", target => target.floatValue = change.newValue);
                refresh();
            });
            link.RegisterCallback<SerializedPropertyChangeEvent>(change => refresh());
            statRefreshers[path] = refresh;

            row.Add(name);
            row.Add(link);
            row.Add(shared);
            row.Add(controls);
            row.Add(result);
            row.Add(tools);
            refresh();
            return row;
        }

        /// <summary>
        /// 스탯 정의를 고르는 좁은 선택 상자를 만듭니다. 고르면 같은 줄의 표시값을 다시 계산합니다.
        /// </summary>
        private VisualElement BuildStatLink(string path, string description)
        {
            string statPath = path + ".stat";
            var container = Element("project-data-stat-link");
            var dropdown = new DropdownField { tooltip = description };
            var choices = new List<ScriptableObject>();
            Action reload = () =>
            {
                var current = session?.Serialized.FindProperty(statPath)?.objectReferenceValue as ScriptableObject;
                choices = DataCatalog.Find(DataKind.Stat).Distinct().ToList();
                var names = choices.Select(DataCatalog.GetDisplayName).ToList();
                choices.Insert(0, null);
                names.Insert(0, "연결 없음");
                int selected = choices.FindIndex(asset => asset == current);
                if (current != null && selected < 0)
                {
                    choices.Add(current);
                    names.Add("범위 밖: " + current.name);
                    selected = choices.Count - 1;
                }

                dropdown.choices = names;
                dropdown.SetValueWithoutNotify(names[Math.Max(0, selected)]);
            };
            reload();
            dropdown.RegisterCallback<PointerDownEvent>(change => reload(), TrickleDown.TrickleDown);
            dropdown.RegisterValueChangedCallback(change =>
            {
                int selected = dropdown.choices.IndexOf(change.newValue);
                if (selected < 0 || selected >= choices.Count || session == null)
                {
                    return;
                }

                session.Serialized.Update();
                SerializedProperty target = session.Serialized.FindProperty(statPath);
                if (target == null)
                {
                    return;
                }

                target.objectReferenceValue = choices[selected];
                session.Serialized.ApplyModifiedProperties();
                StoreDraft();
                RefreshStatus();
                if (statRefreshers.TryGetValue(path, out Action refresh))
                {
                    refresh();
                }
            });
            container.Add(dropdown);
            fieldElements[statPath] = container;
            return container;
        }

        /// <summary>
        /// 스탯 설정의 한 속성을 편집용 복사본에 기록합니다. 속성이 없으면 아무것도 바꾸지 않습니다.
        /// </summary>
        private void ChangeStatSetting(string path, string relative, Action<SerializedProperty> change)
        {
            if (session == null)
            {
                return;
            }

            session.Serialized.Update();
            SerializedProperty target = session.Serialized.FindProperty(path)?.FindPropertyRelative(relative);
            if (target == null)
            {
                SW.Util.SWLog.LogWarning("[DataEditorWindow] 스탯 편집 실패: " + relative + " 속성이 없습니다.");
                return;
            }

            change(target);
            session.Serialized.ApplyModifiedProperties();
            StoreDraft();
            RefreshStatus();
        }

        /// <summary>
        /// 스탯 값을 표시 형식으로 바꿉니다. 백분율 스탯은 퍼센트로 표시합니다.
        /// </summary>
        private static string FormatStatValue(SWStat stat, float value)
        {
            return stat.IsPercentType
                ? (value * 100f).ToString("0.##") + "%"
                : value.ToString("0.###");
        }

        /// <summary>
        /// 연결한 자산을 Project 창에서 표시합니다. 연결이 없으면 아무것도 하지 않습니다.
        /// </summary>
        private void PingReference(string path)
        {
            UnityEngine.Object current = session?.Serialized.FindProperty(path)?.objectReferenceValue;
            if (current != null)
            {
                SW.EditorTools.Util.SWEditorUtils.PingAndSelect(current);
            }
        }

        /// <summary>
        /// 스탯 표의 머리글 줄을 만듭니다.
        /// </summary>
        private VisualElement BuildStatTableHeader()
        {
            var header = Element("project-data-stat-row");
            header.AddToClassList("project-data-stat-header");
            header.Add(Text("스탯", "project-data-stat-name"));
            header.Add(Text("정의 연결", "project-data-stat-link"));
            header.Add(Text("기본값", "project-data-stat-shared"));
            header.Add(Text("개별 기본값", "project-data-stat-override"));
            header.Add(Text("최종", "project-data-stat-result"));
            header.Add(Text(string.Empty, "project-data-stat-tools"));
            return header;
        }

        /// <summary>
        /// 등급 분류와 교체 가능한 효과 정의를 표시합니다. 변경은 적용 전까지 현재 초안에만 보관합니다.
        /// </summary>
        private VisualElement BuildEquipmentGradeEntry(SerializedProperty entry)
        {
            var container = Element("project-data-field");
            container.name = "field-" + entry.propertyPath;
            fieldElements[entry.propertyPath] = container;
            string entryPath = entry.propertyPath;
            var statistics = Element("project-data-grade-stats");
            container.Add(BuildEquipmentCategoryField(
                entry.FindPropertyRelative("performanceGrade"),
                "성능 등급",
                "장비 등급 탭에서 만든 분류를 연결하세요. 순서는 목록 화살표로 바꿉니다."));
            container.Add(BuildEquipmentEffectReference(entry.FindPropertyRelative("effect"), () =>
            {
                var current = session?.Serialized.FindProperty(entryPath);
                if (current != null)
                {
                    statistics.Clear();
                    statistics.Add(BuildGradeStatOverrides(current));
                }
            }));
            container.Add(BuildField(entry.FindPropertyRelative("selectionWeight"), "추첨 가중치",
                "실제 확률은 이 값 ÷ 전체 등급 가중치 합계입니다. 0이면 추첨에서 제외합니다."));
            var probability = Text(string.Empty, "project-data-description");
            Action refreshProbability = () =>
            {
                var weight = session?.Serialized.FindProperty(entryPath + ".selectionWeight");
                double total = (session?.Draft as EquipmentData)?.GetTotalSelectionWeight() ?? 0d;
                probability.text = total > 0d && weight != null
                    ? "실제 등급 확률: " + (weight.floatValue / total * 100d).ToString("0.###") + "%"
                    : "양수 가중치를 설정하면 실제 등급 확률이 표시됩니다.";
            };
            refreshProbability();
            probability.schedule.Execute(refreshProbability).Every(400);
            container.Add(probability);
            statistics.Add(BuildGradeStatOverrides(entry));
            container.Add(statistics);
            container.Add(BuildArray(entry.FindPropertyRelative("additionalEffects"), "추가 효과", "장비 능력 등 교체 가능한 효과를 추가로 연결합니다."));
            return container;
        }

        /// <summary>
        /// 선택기를 유지한 채 등급 효과를 초안에 즉시 반영합니다. 다른 편집 세션이나 사라진 속성이면 변경하지 않습니다.
        /// </summary>
        private VisualElement BuildEquipmentEffectReference(SerializedProperty property, Action changed)
        {
            string path = property.propertyPath;
            DataEditSession owner = session;
            var container = Element("project-data-field");
            container.name = "field-" + path;
            fieldElements[path] = container;
            var field = new ObjectField("등급 효과")
            {
                objectType = typeof(EquipmentEffectData),
                allowSceneObjects = false,
                value = property.objectReferenceValue,
                tooltip = "장비 효과를 선택하거나 끌어 놓으세요. 연결 후 아래에 등급별 스탯이 표시됩니다."
            };
            field.RegisterValueChangedCallback(change =>
            {
                if (session != owner || owner.Draft == null)
                {
                    return;
                }

                owner.Serialized.ApplyModifiedProperties();
                owner.Serialized.Update();
                var target = owner.Serialized.FindProperty(path);
                if (target == null)
                {
                    return;
                }
                if (target.objectReferenceValue == change.newValue)
                {
                    return;
                }

                target.objectReferenceValue = change.newValue;
                owner.Serialized.ApplyModifiedProperties();
                StoreDraft();
                RefreshStatus();
                changed();
            });
            container.Add(field);
            return container;
        }

        /// <summary>
        /// 공유 효과의 스탯만 체크 목록으로 표시합니다. 조회만으로 초안에 재정의 항목을 추가하지 않습니다.
        /// </summary>
        private VisualElement BuildGradeStatOverrides(SerializedProperty entry)
        {
            var container = new Foldout
            {
                text = "등급별 스탯",
                value = true,
                viewDataKey = "grade-stats-" + entry.propertyPath
            };
            string path = entry.propertyPath + ".statOverrides";
            var settings = entry.FindPropertyRelative("statOverrides");
            var template = entry.FindPropertyRelative("effect").objectReferenceValue as EquipmentStatEffectData;
            var definitions = new HashSet<SWStat>();
            var table = Element("project-data-stat-table");
            var header = Element("project-data-stat-row");
            header.AddToClassList("project-data-stat-header");
            header.Add(Text("사용", "project-data-stat-use"));
            header.Add(Text("스탯", "project-data-stat-name"));
            header.Add(Text("기본값", "project-data-stat-shared"));
            header.Add(Text("값 변경", "project-data-stat-override"));
            header.Add(Text("최종", "project-data-stat-result"));
            table.Add(header);
            if (template?.StatBonuses != null)
            {
                foreach (EquipmentStatBonus bonus in template.StatBonuses)
                {
                    if (bonus?.Stat == null || !definitions.Add(bonus.Stat))
                    {
                        continue;
                    }

                    table.Add(BuildGradeStatRow(path, bonus, FindGradeStatProperty(settings, bonus.Stat)));
                }
            }

            if (definitions.Count == 0)
            {
                container.Add(Text("스탯 효과를 연결하고 공유 효과의 스탯 목록을 설정하세요.", "project-data-description"));
            }
            else
            {
                container.Add(table);
                container.Add(Text("사용 해제: 스탯 제외 · 값 변경 해제: 공유 효과의 기본값 사용", "project-data-description"));
            }

            var configured = new HashSet<SWStat>();
            for (int index = 0; index < settings.arraySize; index++)
            {
                var setting = settings.GetArrayElementAtIndex(index);
                var stat = setting.FindPropertyRelative("stat").objectReferenceValue as SWStat;
                if (stat != null && definitions.Contains(stat) && configured.Add(stat))
                {
                    continue;
                }

                int position = index;
                var row = Element("project-data-row");
                row.Add(Text("공유 효과에 없거나 중복된 설정: " + (stat != null ? stat.DisplayName : "스탯 누락"), "project-data-description"));
                row.Add(ActionButton("설정 제거", () => ChangeArray(path, position, 0)));
                container.Add(row);
            }

            return container;
        }

        /// <summary>
        /// 포함 여부·공유값·개별값·최종값을 한 행에서 편집합니다. 변경 실패 시 입력을 되돌리고 초안을 유지합니다.
        /// </summary>
        private VisualElement BuildGradeStatRow(string path, EquipmentStatBonus bonus, SerializedProperty setting)
        {
            bool included = setting?.FindPropertyRelative("enabled").boolValue ?? true;
            bool overridden = setting?.FindPropertyRelative("useAmountOverride").boolValue ?? false;
            float amount = setting?.FindPropertyRelative("amount").floatValue ?? bonus.Amount;
            DataEditSession owner = session;
            SWStat stat = bonus.Stat;
            var row = Element("project-data-stat-row");
            var include = new Toggle { value = included, tooltip = stat.DisplayName + " 사용" };
            include.AddToClassList("project-data-stat-use");
            var name = Text(stat.DisplayName, "project-data-stat-name");
            name.tooltip = stat.DisplayName;
            var shared = Text(FormatGradeStatAmount(stat, bonus.Amount), "project-data-stat-shared");
            var controls = Element("project-data-stat-override");
            var overrideToggle = new Toggle { value = overridden, tooltip = "이 등급의 증가량으로 기본값 대체" };
            overrideToggle.AddToClassList("project-data-stat-toggle");
            var amountField = new FloatField { isDelayed = true, tooltip = "개별 증가량입니다. 백분율 스탯은 0.05가 5%p입니다." };
            amountField.AddToClassList("project-data-stat-amount");
            var result = Text(string.Empty, "project-data-stat-result");
            Action refresh = () =>
            {
                include.SetValueWithoutNotify(included);
                overrideToggle.SetValueWithoutNotify(overridden);
                overrideToggle.SetEnabled(included);
                amountField.SetValueWithoutNotify(overridden ? amount : bonus.Amount);
                amountField.SetEnabled(included && overridden);
                result.text = included ? FormatGradeStatAmount(stat, overridden ? amount : bonus.Amount) : "제외";
                row.EnableInClassList("project-data-stat-excluded", !included);
            };
            include.RegisterValueChangedCallback(change =>
            {
                if (session == owner && ChangeGradeStat(path, stat, bonus.Amount,
                    property => property.FindPropertyRelative("enabled").boolValue = change.newValue))
                {
                    included = change.newValue;
                }
                refresh();
            });
            overrideToggle.RegisterValueChangedCallback(change =>
            {
                if (session == owner && ChangeGradeStat(path, stat, bonus.Amount,
                    property => property.FindPropertyRelative("useAmountOverride").boolValue = change.newValue))
                {
                    overridden = change.newValue;
                }
                refresh();
            });
            amountField.RegisterValueChangedCallback(change =>
            {
                if (session == owner && ChangeGradeStat(path, stat, bonus.Amount,
                    property => property.FindPropertyRelative("amount").floatValue = change.newValue))
                {
                    amount = change.newValue;
                }
                refresh();
            });
            controls.Add(overrideToggle);
            controls.Add(amountField);
            row.Add(include);
            row.Add(name);
            row.Add(shared);
            row.Add(controls);
            row.Add(result);
            refresh();
            return row;
        }

        /// <summary>
        /// 일반 수치와 백분율 스탯의 증가량을 표시합니다. 원본 수치는 변경하지 않습니다.
        /// </summary>
        private static string FormatGradeStatAmount(SWStat stat, float amount)
        {
            float value = stat.IsPercentType ? amount * 100f : amount;
            return value.ToString("+0.###;-0.###;0") + (stat.IsPercentType ? "%p" : string.Empty);
        }

        /// <summary>
        /// 해당 스탯의 직렬화 항목을 찾습니다. 별도 설정이 없으면 null입니다.
        /// </summary>
        private static SerializedProperty FindGradeStatProperty(SerializedProperty settings, SWStat stat)
        {
            for (int index = 0; index < settings.arraySize; index++)
            {
                var entry = settings.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("stat").objectReferenceValue == stat)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// 사용자 입력 시에만 재정의 항목을 만들고 초안에 적용합니다. 필수 속성 누락 시 false이며 화면 전체를 재생성하지 않습니다.
        /// </summary>
        private bool ChangeGradeStat(string path, SWStat stat, float sharedAmount, Action<SerializedProperty> change)
        {
            if (session == null || stat == null)
            {
                return false;
            }

            session.Serialized.Update();
            var settings = session.Serialized.FindProperty(path);
            if (settings == null)
            {
                return false;
            }

            var property = FindGradeStatProperty(settings, stat);
            bool added = property == null;
            if (added)
            {
                settings.InsertArrayElementAtIndex(settings.arraySize);
                property = settings.GetArrayElementAtIndex(settings.arraySize - 1);
            }

            foreach (string name in new[] { "stat", "enabled", "useAmountOverride", "amount" })
            {
                if (property.FindPropertyRelative(name) == null)
                {
                    session.Serialized.Update();
                    SW.Util.SWLog.LogWarning("[DataEditorWindow] 등급 스탯 편집 실패: 필수 속성이 없습니다.");
                    return false;
                }
            }

            if (added)
            {
                property.FindPropertyRelative("stat").objectReferenceValue = stat;
                property.FindPropertyRelative("enabled").boolValue = true;
                property.FindPropertyRelative("useAmountOverride").boolValue = false;
                property.FindPropertyRelative("amount").floatValue = sharedAmount;
            }

            change(property);
            session.Serialized.ApplyModifiedProperties();
            StoreDraft();
            RefreshStatus();
            return true;
        }

        /// <summary>
        /// 장비 분류 폴더에 있는 자산만 연결합니다. 새 분류의 생성과 수정은 장비 등급 탭에서 처리합니다.
        /// </summary>
        private VisualElement BuildEquipmentCategoryField(SerializedProperty property, string title, string description)
        {
            return BuildFilteredReference(property, title, description, () => DataCatalog.Find(DataKind.EquipmentCategory));
        }

        /// <summary>
        /// 스탯 정의와 증가량을 한 행에서 편집합니다. 스탯 목록은 통합 창에서 관리합니다.
        /// </summary>
        private VisualElement BuildStatBonusEntry(SerializedProperty entry)
        {
            var container = Element("project-data-field");
            container.Add(BuildFilteredReference(entry.FindPropertyRelative("stat"), "스탯", "스탯 탭에서 정의한 능력치를 선택하세요.",
                () => DataCatalog.Find(DataKind.Stat)));
            container.Add(BuildField(entry.FindPropertyRelative("amount"), "증가량", "선택한 스탯의 단위입니다. 백분율 표시 스탯에서 0.05는 5%p입니다."));
            return container;
        }

        /// <summary>
        /// 지정된 범위의 자산만 선택 목록으로 제공합니다. 범위 밖 기존 참조는 보존하고 표시합니다.
        /// </summary>
        private VisualElement BuildFilteredReference(SerializedProperty property, string title, string description, Func<List<ScriptableObject>> findChoices)
        {
            string path = property.propertyPath;
            var container = Element("project-data-field");
            fieldElements[path] = container;
            var row = Element("project-data-row");
            var dropdown = new DropdownField(title) { tooltip = description };
            var choices = new List<ScriptableObject>();
            Action refresh = () =>
            {
                var current = session?.Serialized.FindProperty(path)?.objectReferenceValue as ScriptableObject;
                choices = findChoices().Distinct().ToList();
                var names = choices.Select(asset => DataCatalog.GetDisplayName(asset) + " [" + asset.name + "]").ToList();
                choices.Insert(0, null);
                names.Insert(0, "없음");
                int selected = choices.FindIndex(asset => asset == current);
                if (current != null && selected < 0)
                {
                    choices.Add(current);
                    names.Add("범위 밖 연결: " + current.name);
                    selected = choices.Count - 1;
                }
                dropdown.choices = names;
                dropdown.SetValueWithoutNotify(names[Math.Max(0, selected)]);
            };
            refresh();
            dropdown.RegisterCallback<PointerDownEvent>(change => refresh(), TrickleDown.TrickleDown);
            dropdown.RegisterCallback<FocusInEvent>(change => refresh());
            dropdown.RegisterValueChangedCallback(change =>
            {
                int selected = dropdown.choices.IndexOf(change.newValue);
                if (selected < 0 || selected >= choices.Count || session == null)
                {
                    return;
                }
                session.Serialized.Update();
                var target = session.Serialized.FindProperty(path);
                if (target == null)
                {
                    return;
                }
                target.objectReferenceValue = choices[selected];
                session.Serialized.ApplyModifiedProperties();
                StoreDraft();
                RefreshStatus();
            });
            row.Add(dropdown);
            row.Add(ActionButton("Ping", () =>
            {
                var current = session?.Serialized.FindProperty(path)?.objectReferenceValue;
                if (current != null)
                {
                    SW.EditorTools.Util.SWEditorUtils.PingAndSelect(current);
                }
            }));
            row.Add(ActionButton("편집", () => OpenReference(path)));
            container.Add(row);
            return container;
        }

        /// <summary>
        /// 아이템이 연결한 장비에 등록된 등급 중 장비 분류 폴더의 항목만 반환합니다. 장비가 없으면 빈 목록입니다.
        /// </summary>
        private List<ScriptableObject> FindItemGradeChoices()
        {
            var item = session?.Draft as ItemData;
            if (item?.Equipment?.PerformanceGrades == null)
            {
                return new List<ScriptableObject>();
            }

            var categories = DataCatalog.Find(DataKind.EquipmentCategory);
            return item.Equipment.PerformanceGrades.Where(grade => grade?.PerformanceGrade != null)
                .Select(grade => (ScriptableObject)grade.PerformanceGrade).Where(categories.Contains).Distinct().ToList();
        }

        #endregion // 장비 입력
    }
}
