using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.Util;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 독립 데이터 창의 템플릿 생성·참조 복제를 담당합니다.
    /// </summary>
    public sealed partial class DataEditorWindow
    {
        #region 생성
        /// <summary>
        /// 선택이나 검색 결과가 없어도 현재 종류의 유효한 템플릿으로 생성 화면을 엽니다.
        /// 템플릿이 없으면 안내하고 현재 편집을 유지합니다.
        /// </summary>
        private void BeginCreation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ScriptableObject template = DataCatalog.Find(kind).FirstOrDefault(asset => asset.IsValid);
            if (template == null && (kind == DataKind.Currency || kind == DataKind.Item))
            {
                Type type = DataCatalog.GetDataType(kind);
                template = CreateInstance(type);
                template.name = type.Name;
                template.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            }

            if (template == null)
            {
                notice = "현재 종류에 유효한 생성 템플릿이 없습니다. 기존 데이터의 오류를 먼저 수정하세요.";
                RefreshStatus();
                return;
            }

            if (!PrepareCreation())
            {
                if (!AssetDatabase.Contains(template))
                {
                    DestroyImmediate(template);
                }

                return;
            }

            ShowCreation(template, null, false);
        }

        /// <summary>
        /// 선택한 저장 데이터를 기준으로 복제 화면을 엽니다. 선택이 없으면 생성하지 않습니다.
        /// </summary>
        private void BeginDuplicate()
        {
            if (session?.Source == null || !PrepareCreation())
            {
                return;
            }

            ShowCreation(session.Source, null, true);
        }

        /// <summary>
        /// 생성 전에 기존 초안의 처리 결과를 반영합니다. 실행 중이거나 이동을 취소하면 false를 반환합니다.
        /// </summary>
        private bool PrepareCreation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !CanLeaveDraft())
            {
                return false;
            }

            if (session != null && session.HasChanges)
            {
                session.Reload();
                StoreDraft();
            }

            return true;
        }

        /// <summary>
        /// 참조 원본을 독립 복제하는 입력 화면을 엽니다. 생성 전까지 파일이나 참조를 변경하지 않습니다.
        /// </summary>
        private void BeginReferenceCopy(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || session == null)
            {
                return;
            }

            var reference = session.Serialized.FindProperty(path)?.objectReferenceValue as ProjectData;
            if (!DataCatalog.IsSupported(reference))
            {
                notice = "지원하는 데이터 자산을 연결한 뒤 복제하세요. 그림 파일은 Project 창에서 관리합니다.";
                return;
            }

            ShowCreation(reference, path, true);
        }

        /// <summary>
        /// 원본 템플릿, 생성 폴더와 공유 참조 정책을 표시한 뒤 생성합니다.
        /// </summary>
        private void ShowCreation(ScriptableObject template, string referencePath, bool duplicate)
        {
            ClearCreationTemplate();
            if (!AssetDatabase.Contains(template))
            {
                temporaryCreationTemplate = template;
            }

            creatingData = true;
            detail.Unbind();
            detail.Clear();
            heading.text = referencePath != null ? "참조 데이터 별도 복제" : duplicate ? "선택 데이터 복제" : KindName + " 새 데이터 만들기";
            headingType.text = ObjectNames.NicifyVariableName(template.GetType().Name);
            headingIcon.image = AssetPreview.GetMiniThumbnail(template);
            detailScroll.scrollOffset = Vector2.zero;
            detail.Add(Text(duplicate ? "선택한 원본을 유지하고 별도 파일로 복제합니다." : "기준 템플릿을 고르고 새 파일 이름을 입력하세요.", "project-data-description"));
            var templateField = new ObjectField("템플릿")
            {
                objectType = template.GetType(),
                allowSceneObjects = false,
                value = template,
                name = "creationTemplate"
            };
            var fileName = new TextField("새 파일 이름")
            {
                value = duplicate ? template.name + " Copy" : "New" + template.name,
                name = "newDataName"
            };
            templateField.SetEnabled(!duplicate);
            if (temporaryCreationTemplate == null)
            {
                detail.Add(templateField);
            }
            else
            {
                detail.Add(Text("기본 양식으로 첫 데이터를 만듭니다. 생성 후 이름·아이콘·기본 수량을 수정하세요.", "project-data-description"));
            }

            detail.Add(fileName);
            detail.Add(Text(
                "저장 위치: " + GetCreationFolder(template) + "\n이름이 겹치면 번호를 붙입니다. 프리팹과 그림 참조는 공유합니다. 이름 끝에는 Data를 붙입니다. 생성 후 편집 화면에서 표시 이름과 수치를 조정하세요.",
                "project-data-description"));
            if (referencePath != null)
            {
                detail.Add(new HelpBox(
                    "복제 자산은 생성 즉시 저장됩니다. 현재 데이터의 참조 연결은 편집값에만 바뀌며 적용 버튼을 눌러야 원본에 반영됩니다. 수정 취소 후에도 생성한 자산은 남습니다.",
                    HelpBoxMessageType.Info));
            }

            var message = Text(string.Empty, "project-data-description");
            detail.Add(message);
            var row = Element("project-data-row");
            row.Add(ActionButton(
                "검사 후 생성",
                () =>
            {
                if (!TryCreateAsset(
                    templateField.value as ScriptableObject,
                    fileName.value,
                    out ScriptableObject created,
                    out string reason))
                {
                    message.text = reason;
                    return;
                }

                if (referencePath != null)
                {
                    session.Serialized.Update();
                    var reference = session.Serialized.FindProperty(referencePath);
                    reference.objectReferenceValue = created;
                    session.Serialized.ApplyModifiedProperties();
                    notice = "참조 복사본을 생성하고 편집값에 연결했습니다. 원본 연결은 적용 버튼으로 반영하세요.";
                    currentPage = "편집";
                    RebuildDetail();
                    StoreDraft();
                }
                else
                {
                    rootVisualElement.Q<TextField>("dataSearch").value = string.Empty;
                    SelectAsset((ProjectData)created);
                    notice = reason;
                }

                RefreshCatalog();
            },
                "confirmCreateData"));
            row.Add(ActionButton("생성 취소", RebuildDetail));
            detail.Add(row);
            RefreshStatus();
        }

        /// <summary>
        /// 템플릿 종류의 저장 폴더를 반환합니다. 지원하지 않는 템플릿이면 빈 문자열입니다.
        /// </summary>
        private static string GetCreationFolder(ScriptableObject template)
        {
            return DataCatalog.TryGetKind(template, out DataKind templateKind) ? DataCatalog.GetFolder(templateKind) : string.Empty;
        }

        /// <summary>
        /// 검사한 템플릿을 종류별 폴더에 복제합니다. 연결된 자산은 공유하며 이름이 겹치면 번호를 붙입니다.
        /// </summary>
        private static bool TryCreateAsset(ScriptableObject template, string fileName, out ScriptableObject created, out string reason)
        {
            created = null;
            reason = string.Empty;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "전투를 정지한 뒤 데이터를 생성하세요.";
                return false;
            }

            if (!(template is ProjectData data) || !DataCatalog.IsSupported(data))
            {
                reason = "지원하는 템플릿을 선택하세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileName)
                || fileName != fileName.Trim()
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || fileName.EndsWith("."))
            {
                reason = "경로·확장자를 제외한 올바른 파일 이름을 입력하세요.";
                return false;
            }

            var issues = DataCatalog.CollectIssues(data);
            if (issues.Count > 0)
            {
                reason = "템플릿 검사 실패: " + issues[0].Message;
                return false;
            }

            string folder = GetCreationFolder(template);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                reason = "종류별 데이터 폴더를 찾지 못했습니다: " + folder;
                return false;
            }

            string stem = fileName.EndsWith("Data", StringComparison.Ordinal) ? fileName.Substring(0, fileName.Length - 4) : fileName;
            string path = folder + "/" + stem + "Data.asset";
            for (int suffix = 2; !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)); suffix++)
            {
                path = folder + "/" + stem + suffix + "Data.asset";
            }

            ScriptableObject copy = Instantiate(template);
            try
            {
                copy.name = Path.GetFileNameWithoutExtension(path);
                copy.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copy, path);
                Undo.IncrementCurrentGroup();
                Undo.RegisterCreatedObjectUndo(copy, "Project T 데이터 생성");
                AssetDatabase.SaveAssetIfDirty(copy);
                created = copy;
                reason = "데이터를 생성했습니다. 연결된 프리팹과 그림은 원본과 공유합니다.";
                return true;
            }
            catch (Exception exception)
            {
                if (AssetDatabase.GetAssetPath(copy) == path)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                else
                {
                    DestroyImmediate(copy);
                }

                reason = "생성하지 못했습니다. 이름과 파일 접근 상태를 확인하세요.";
                SWLog.LogError("[DataEditorWindow] 생성 실패: " + exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 첫 재화·아이템 생성에 사용한 메모리 양식만 정리합니다. 저장된 자산은 파괴하지 않습니다.
        /// </summary>
        private void ClearCreationTemplate()
        {
            if (temporaryCreationTemplate != null)
            {
                DestroyImmediate(temporaryCreationTemplate);
                temporaryCreationTemplate = null;
            }
        }

        #endregion // 생성
    }
}
