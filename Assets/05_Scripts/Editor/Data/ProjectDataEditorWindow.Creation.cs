using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 독립 데이터 창의 템플릿 생성·참조 복제·시험 전투 연결을 담당합니다.
    /// </summary>
    public partial class ProjectDataEditorWindow
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

            var candidates = ProjectDataCatalog.Find(DataKind);
            var template = candidates.FirstOrDefault(asset => ProjectDataValidation.Validate(asset).Count == 0);
            if (template == null && (DataKind == 4 || DataKind == 5))
            {
                template = ScriptableObject.CreateInstance(ProjectDataCatalog.SupportedTypes[DataKind]);
                template.name = DataKind == 4 ? "CurrencyData" : "ItemData";
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

            var reference = session.Serialized.FindProperty(path)?.objectReferenceValue as ScriptableObject;
            if (ProjectDataCatalog.GetKind(reference) < 0)
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
                "저장 위치: " + ProjectDataCatalog.GetFolder(template.GetType()) + "\n이름이 겹치면 번호를 붙입니다. 프리팹과 그림 참조는 공유합니다. 이름 끝에는 Data를 붙입니다. 생성 후 편집 화면에서 표시 이름과 수치를 조정하세요.",
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
                if (!ProjectDataAssetService.TryCreate(
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
                    SelectAsset(created);
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

        #region 시험
        /// <summary>
        /// 원본을 유지한 시험 전투의 기준과 연결 대상을 선택합니다.
        /// </summary>
        private void BuildTrial()
        {
            detail.Add(Text("시험 전투", "project-data-heading"));
            detail.Add(Text(
                "현재 데이터는 적용·저장한 값으로 시험합니다. 기준 장면과 전투 데이터 전체를 독립 복제하고 그림·프리팹은 읽기 전용으로 공유합니다. 새 클래스는 구매 목록에 추가됩니다.",
                "project-data-description"));
            var baseStage = new ObjectField("기준 스테이지")
            {
                objectType = typeof(StageData),
                allowSceneObjects = false,
                value = session.Source as StageData ?? AssetDatabase.LoadAssetAtPath<StageData>("Assets/02_Res/Data/Stage/Stage01Data.asset")
            };
            var baseScene = new ObjectField("기준 전투 장면")
            {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false,
                value = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/01_Scenes/Stage01_Grassland.unity")
            };
            detail.Add(baseStage);
            detail.Add(baseScene);
            detail.Add(Text(
                "기준 장면의 지형·공방 위치·배치 영역은 유지됩니다. 경로의 마지막 점과 공방 위치가 맞는지 시험에서 확인하세요. 복사본은 Assets/Temp/ProjectDataTrials에 보관합니다.",
                "project-data-description"));
            var create = ActionButton(
                "시험 복사본 만들기",
                () =>
            {
                if (session.HasChanges || session.HasExternalChanges)
                {
                    notice = "현재 변경을 적용하고 원본 상태를 확인한 뒤 시험 복사본을 만드세요.";
                    return;
                }

                bool success = ProjectDataTrialService.TryCreate(
                    baseStage.value as StageData,
                    session.Source,
                    AssetDatabase.GetAssetPath(baseScene.value),
                    out string path,
                    out notice);
                if (success)
                {
                    trialScenePath = path;
                    RebuildDetail();
                }
            },
                "createDataTrial");
            create.SetEnabled(!session.HasChanges && !session.HasExternalChanges && !EditorApplication.isPlayingOrWillChangePlaymode);
            detail.Add(create);
            if (!string.IsNullOrEmpty(trialScenePath))
            {
                detail.Add(Text(trialScenePath, "project-data-description"));
                detail.Add(ActionButton(
                    "시험 장면 열기",
                    () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        notice = "실행 중인 전투를 먼저 정지하세요.";
                        return;
                    }

                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(trialScenePath, OpenSceneMode.Single);
                        notice = "시험 장면을 열었습니다. Unity 재생 버튼으로 전투를 확인하세요.";
                    }
                }));
            }
        }

        #endregion // 시험
    }
}
