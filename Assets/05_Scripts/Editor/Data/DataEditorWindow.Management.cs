using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.Base;
using SW.Util;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 모든 프로젝트 데이터 창의 에셋 관리 기능과 아이템·장비 작업 탭을 제공합니다.
    /// </summary>
    public sealed partial class DataEditorWindow
    {
        #region 필드
        [SerializeField] private DataLabelMode listLabel = DataLabelMode.FileName;
        [SerializeField] private DataSortMode listSort = DataSortMode.FileName;

        /// <summary>
        /// 아이템·장비 창의 탭 순서입니다.
        /// </summary>
        private static readonly DataKind[] WorkspaceKinds =
        {
            DataKind.Item, DataKind.Equipment, DataKind.EquipmentStatEffect, DataKind.EquipmentCategory, DataKind.Stat,
            DataKind.CraftingRecipe, DataKind.CraftingCost
        };

        #endregion // 필드

        #region 작업 공간
        /// <summary>
        /// 아이템·장비 창에서 함께 관리하는 종류인지 반환합니다.
        /// </summary>
        private static bool IsEquipmentWorkspace(DataKind value)
        {
            return Array.IndexOf(WorkspaceKinds, value) >= 0;
        }

        /// <summary>
        /// 장비 관련 종류를 한 창에서 전환하는 탭을 만듭니다.
        /// </summary>
        private VisualElement BuildWorkspaceTabs()
        {
            var tabs = Element("project-data-toolbar");
            foreach (DataKind tabKind in WorkspaceKinds)
            {
                DataKind targetKind = tabKind;
                var button = ActionButton(DataCatalog.GetName(tabKind), () => SwitchKind(targetKind));
                button.EnableInClassList("project-data-primary", kind == tabKind);
                tabs.Add(button);
            }

            return tabs;
        }

        /// <summary>
        /// 미적용 변경을 먼저 처리하고 종류별 초안을 복원합니다. 이동을 취소하면 기존 화면을 유지합니다.
        /// </summary>
        private bool SwitchKind(DataKind targetKind)
        {
            if (kind == targetKind)
            {
                return true;
            }
            if (!CanLeaveDraft())
            {
                return false;
            }
            if (session != null && session.HasChanges)
            {
                session.Reload();
            }

            StoreDraft();
            detail?.Unbind();
            session?.Dispose();
            session = null;
            ClearCreationTemplate();
            kind = targetKind;
            search = string.Empty;
            notice = string.Empty;
            issues.Clear();
            currentPage = "편집";
            CreateGUI();
            return true;
        }

        #endregion // 작업 공간

        #region 목록
        /// <summary>
        /// 파일명·표시명·코드명·식별 번호 정렬과 목록 표시 기준을 제공합니다.
        /// </summary>
        private VisualElement BuildListOptions()
        {
            var options = Element("project-data-list-options");
            options.Add(BuildModeField<DataLabelMode>("표시", listLabel, DataCatalog.GetName, value => listLabel = value));
            options.Add(BuildModeField<DataSortMode>("정렬", listSort, DataCatalog.GetName, value => listSort = value));
            return options;
        }

        /// <summary>
        /// 열거형 값을 한글 이름으로 고르는 선택 상자를 만듭니다. 고르면 목록을 다시 읽습니다.
        /// </summary>
        private DropdownField BuildModeField<TMode>(
            string title,
            TMode current,
            Func<TMode, string> getName,
            Action<TMode> apply)
            where TMode : struct, Enum
        {
            TMode[] modes = (TMode[])Enum.GetValues(typeof(TMode));
            var names = new List<string>(modes.Length);
            foreach (TMode mode in modes)
            {
                names.Add(getName(mode));
            }

            var field = new DropdownField(title, names, Array.IndexOf(modes, current));
            field.RegisterValueChangedCallback(change =>
            {
                apply(modes[names.IndexOf(change.newValue)]);
                RefreshCatalog();
            });
            return field;
        }

        /// <summary>
        /// 선택한 정렬 기준을 적용합니다. 코드명·식별 번호가 없는 데이터는 이름을 보조 기준으로 사용합니다.
        /// </summary>
        private List<ScriptableObject> SortAssets(List<ScriptableObject> values)
        {
            if (listSort == DataSortMode.Identifier)
            {
                return values.OrderBy(asset => (asset as SWIdentifiedObject)?.ID ?? 0).ThenBy(asset => asset.name).ToList();
            }

            DataLabelMode mode = listSort == DataSortMode.DisplayName ? DataLabelMode.DisplayName
                : listSort == DataSortMode.CodeName ? DataLabelMode.CodeName
                : DataLabelMode.FileName;
            return values.OrderBy(asset => GetAssetLabel(asset, mode), StringComparer.OrdinalIgnoreCase)
                .ThenBy(asset => asset.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 현재 목록 표시 기준의 이름입니다. 없는 코드명은 파일 이름으로 대신합니다.
        /// </summary>
        private string GetListLabel(ScriptableObject asset)
        {
            return GetAssetLabel(asset, listLabel);
        }

        /// <summary>
        /// 지정한 기준으로 에셋 이름을 읽습니다. 코드명이 없는 자산은 파일 이름입니다.
        /// </summary>
        private static string GetAssetLabel(ScriptableObject asset, DataLabelMode mode)
        {
            if (mode == DataLabelMode.DisplayName)
            {
                return DataCatalog.GetDisplayName(asset);
            }

            if (mode == DataLabelMode.CodeName && asset is SWIdentifiedObject identified)
            {
                return identified.CodeName;
            }

            return asset.name;
        }

        #endregion // 목록

        #region 에셋 관리
        /// <summary>
        /// 원본 경로와 파일 이름 변경·삭제 기능을 표시합니다. 입력값 편집과 파일 작업을 구분합니다.
        /// </summary>
        private void BuildAssetManagement()
        {
            var section = new Foldout { text = "에셋 파일 관리", value = true };
            section.AddToClassList("project-data-section");
            var row = Element("project-data-row");
            var fileName = new TextField("파일 이름") { value = session.Source.name };
            row.Add(fileName);
            row.Add(ActionButton("이름 변경", () => RenameCurrent(fileName.value)));
            row.Add(ActionButton("삭제", DeleteCurrent));
            section.Add(row);
            section.Add(Text(AssetDatabase.GetAssetPath(session.Source), "project-data-description"));
            section.Add(Text("파일 이름 변경은 즉시 저장됩니다. 표시 이름·코드명은 아래에서 수정한 뒤 저장하세요.", "project-data-description"));
            detail.Add(section);
        }

        /// <summary>
        /// 파일 작업 전에 초안·외부 변경·잠금·실행 상태를 검사합니다. 실패하면 안내하고 false입니다.
        /// </summary>
        private bool CanManageCurrentAsset()
        {
            if (session?.Source == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                notice = "편집 모드에서 원본 에셋을 선택하세요.";
                RefreshStatus();
                return false;
            }
            if (session.HasChanges || session.HasExternalChanges || !AssetDatabase.IsOpenForEdit(session.Source))
            {
                notice = "미적용 값은 저장하거나 되돌린 뒤 진행하세요. 외부 변경과 파일 잠금도 확인하세요.";
                RefreshStatus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// GUID와 참조를 보존하며 파일 이름을 바꿉니다. 잘못된 이름·중복·저장 실패는 원본을 유지합니다.
        /// </summary>
        private void RenameCurrent(string value)
        {
            if (!CanManageCurrentAsset())
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim()
                || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.EndsWith("."))
            {
                notice = "경로와 확장자를 제외한 올바른 파일 이름을 입력하세요.";
                RefreshStatus();
                return;
            }

            string fileName = value.EndsWith("Data", StringComparison.Ordinal) ? value : value + "Data";
            string path = AssetDatabase.GetAssetPath(session.Source);
            string destination = Path.GetDirectoryName(path).Replace('\\', '/') + "/" + fileName + ".asset";
            if (fileName == session.Source.name)
            {
                return;
            }
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(destination)))
            {
                notice = "같은 폴더에 해당 파일 이름이 이미 있습니다.";
                RefreshStatus();
                return;
            }

            string error = AssetDatabase.RenameAsset(path, fileName);
            if (!string.IsNullOrEmpty(error))
            {
                notice = "이름 변경 실패: " + error;
                SWLog.LogWarning("[DataEditorWindow] " + notice);
                RefreshStatus();
                return;
            }

            AssetDatabase.SaveAssetIfDirty(session.Source);
            detail.Unbind();
            session.Reload();
            StoreDraft();
            notice = "파일 이름을 변경했습니다. 기존 참조는 유지됩니다.";
            RebuildDetail();
            RefreshCatalog();
        }

        /// <summary>
        /// 프로젝트 사용처가 없는 에셋을 확인 후 휴지통으로 보냅니다. 참조·초안이 있거나 취소하면 유지합니다.
        /// </summary>
        private void DeleteCurrent()
        {
            if (!CanManageCurrentAsset())
            {
                return;
            }
            string path = AssetDatabase.GetAssetPath(session.Source);
            var usages = DataCatalog.FindUsages(session.Source);
            if (usages.Count > 0)
            {
                notice = "참조하는 자산이 " + usages.Count + "개 있습니다. 참조·사용처에서 연결을 먼저 정리하세요.";
                RefreshStatus();
                return;
            }
            if (!EditorUtility.DisplayDialog("에셋 삭제", path + "\n이 에셋을 휴지통으로 보낼까요?", "휴지통으로 이동", "취소"))
            {
                return;
            }
            if (!AssetDatabase.MoveAssetToTrash(path))
            {
                notice = "휴지통으로 이동하지 못했습니다. 파일 접근 상태를 확인하세요.";
                RefreshStatus();
                return;
            }

            detail.Unbind();
            session.Dispose();
            session = null;
            SessionState.EraseString(StateKey + "source");
            SessionState.EraseString(StateKey + "draft");
            SessionState.EraseString(StateKey + "snapshot");
            notice = "에셋을 휴지통으로 이동했습니다.";
            RefreshCatalog();
            RebuildDetail();
        }

        /// <summary>
        /// 새 분류·스탯에 독립적인 코드명을 부여합니다. 필수 속성이 없으면 false이며 일반 데이터는 변경하지 않습니다.
        /// </summary>
        private static bool PrepareNewIdentity(ScriptableObject asset)
        {
            if (!(asset is SWIdentifiedObject))
            {
                return true;
            }

            using (var serialized = new SerializedObject(asset))
            {
                var code = serialized.FindProperty("codeName");
                var identifier = serialized.FindProperty("id");
                var displayName = serialized.FindProperty("displayName");
                if (code == null || identifier == null || displayName == null)
                {
                    SWLog.LogWarning("[DataEditorWindow] 생성 준비 실패: 식별 속성이 없습니다.");
                    return false;
                }

                code.stringValue = asset.GetType().Name + "_" + Guid.NewGuid().ToString("N");
                identifier.intValue = 0;
                if (string.IsNullOrWhiteSpace(displayName.stringValue))
                {
                    displayName.stringValue = "새 " + (asset is SWCategory ? "장비 등급" : "스탯");
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return true;
            }
        }

        #endregion // 에셋 관리
    }
}
