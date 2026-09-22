using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// Project T 메뉴에서 여는 데이터 종류별 제작 창입니다. 미적용 초안은 같은 편집기 세션에서 창을 다시 열어도 복원합니다.
    /// </summary>
    public sealed partial class DataEditorWindow : EditorWindow
    {
        #region 필드
        private const string StylePath = "Assets/05_Scripts/Editor/Data/DataEditorWindow.uss";
        [SerializeField] private DataKind kind;
        private DataEditSession session;
        private List<ProjectData> assets = new List<ProjectData>();
        private ListView assetList;
        private VisualElement detail;
        private ScrollView detailScroll;
        private Label status;
        private Label heading;
        private Button applyButton;
        private Button cancelButton;
        private Button inspectorButton;
        private Label assetCount;
        private Label headingType;
        private Image headingIcon;
        private Button createButton;
        private Button duplicateButton;
        private bool creatingData;
        private ScriptableObject temporaryCreationTemplate;
        private string search = string.Empty;
        private string currentPage = "편집";
        private string notice = string.Empty;
        private List<DataIssue> issues = new List<DataIssue>();
        private readonly Dictionary<string, VisualElement> fieldElements = new Dictionary<string, VisualElement>();
        private string trialScenePath = string.Empty;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 창과 메뉴에 표시하는 종류 이름입니다.
        /// </summary>
        private string KindName => DataCatalog.GetName(kind);

        /// <summary>
        /// 다른 종류의 창과 초안이 섞이지 않도록 세션 저장 공간을 구분합니다.
        /// </summary>
        private string StateKey => "ProjectT.DataEditor." + (int)kind + ".";

        #endregion // 프로퍼티

        #region 진입
        /// <summary>
        /// 클래스 데이터를 제작하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/클래스 편집기", priority = 0)]
        private static void OpenClasses()
        {
            Open(DataKind.Class);
        }

        /// <summary>
        /// 적 데이터를 제작하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/적 편집기", priority = 1)]
        private static void OpenEnemies()
        {
            Open(DataKind.Enemy);
        }

        /// <summary>
        /// 스테이지 데이터를 제작하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/스테이지 편집기", priority = 2)]
        private static void OpenStages()
        {
            Open(DataKind.Stage);
        }

        /// <summary>
        /// 적 이동 경로를 제작하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/경로 편집기", priority = 3)]
        private static void OpenRoutes()
        {
            Open(DataKind.Route);
        }

        /// <summary>
        /// 재화의 이름·아이콘·기본 수량을 관리하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/재화 편집기", priority = 5)]
        private static void OpenCurrencies()
        {
            Open(DataKind.Currency);
        }

        /// <summary>
        /// 아이템의 이름·아이콘·기본 수량을 관리하는 창을 엽니다.
        /// </summary>
        [MenuItem("Project T/데이터/아이템 편집기", priority = 6)]
        private static void OpenItems()
        {
            Open(DataKind.Item);
        }

        /// <summary>
        /// 종류별 창을 열어 반환합니다. 같은 종류의 창이 이미 있으면 그 창을 앞으로 가져옵니다.
        /// </summary>
        public static DataEditorWindow Open(DataKind dataKind)
        {
            DataEditorWindow window = Resources.FindObjectsOfTypeAll<DataEditorWindow>().FirstOrDefault(candidate => candidate.kind == dataKind);
            if (window == null)
            {
                window = CreateInstance<DataEditorWindow>();
                window.kind = dataKind;
                window.titleContent = new GUIContent("Project T · " + window.KindName);
                window.minSize = new Vector2(800, 560);
            }

            window.Show();
            window.Focus();
            return window;
        }

        /// <summary>
        /// 원본 변경과 실행 취소 알림을 연결합니다.
        /// </summary>
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        /// <summary>
        /// 초안을 세션에 보관하고 메모리 복사본과 이벤트를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            StoreDraft();
            ClearCreationTemplate();
            detail?.Unbind();
            session?.Dispose();
            session = null;
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        /// <summary>
        /// SWUtils 공통 테마와 별도 탐색·편집 화면을 구성합니다.
        /// </summary>
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            SWEditorTheme.Apply(rootVisualElement);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null)
            {
                rootVisualElement.styleSheets.Add(style);
            }

            rootVisualElement.AddToClassList("project-data-root");
            var toolbar = Element("project-data-toolbar");
            createButton = ActionButton("+ 새로 만들기", BeginCreation, "createData");
            createButton.tooltip = KindName + " 데이터를 템플릿으로 생성합니다.";
            toolbar.Add(createButton);
            duplicateButton = ActionButton("복제", BeginDuplicate, "duplicateData");
            duplicateButton.tooltip = "선택한 데이터를 별도 자산으로 복제합니다.";
            toolbar.Add(duplicateButton);
            toolbar.Add(Element("project-data-spacer"));
            toolbar.Add(CreateToolsMenu());
            rootVisualElement.Add(toolbar);
            var body = Element("project-data-body");
            var sidebar = Element("project-data-sidebar");
            var searchRow = Element("project-data-search");
            var searchField = new TextField
            {
                value = search,
                name = "dataSearch",
                tooltip = "자산 이름 또는 표시 이름으로 검색합니다."
            };
            searchField.textEdition.placeholder = "이름 검색";
            searchField.RegisterValueChangedCallback(change =>
            {
                search = change.newValue;
                RefreshCatalog();
            });
            searchRow.Add(searchField);
            var refresh = ActionButton("새로 고침", RefreshCatalog);
            searchRow.Add(refresh);
            sidebar.Add(searchRow);
            assetList = new ListView
            {
                name = "dataAssetList",
                fixedItemHeight = 43,
                selectionType = SelectionType.Single,
                makeItem = MakeListRow,
                bindItem = BindListRow
            };
            assetList.AddToClassList("project-data-list");
            assetList.selectionChanged += selected =>
            {
                var asset = selected.FirstOrDefault() as ProjectData;
                if (asset != null)
                {
                    SelectAsset(asset);
                }
            };
            sidebar.Add(assetList);
            assetCount = Text(string.Empty, "project-data-count");
            sidebar.Add(assetCount);
            body.Add(sidebar);
            var workspace = Element("project-data-detail");
            var header = Element("project-data-detail-header");
            headingIcon = new Image
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            headingIcon.AddToClassList("project-data-asset-icon");
            header.Add(headingIcon);
            var identity = Element("project-data-identity");
            heading = Text("데이터를 선택하세요", "project-data-asset-title");
            headingType = Text(string.Empty, "project-data-asset-subtitle");
            identity.Add(heading);
            identity.Add(headingType);
            header.Add(identity);
            inspectorButton = ActionButton("인스펙터", () => ShowPage("편집"), "showInspector");
            inspectorButton.tooltip = "데이터 입력 화면으로 돌아갑니다.";
            header.Add(inspectorButton);
            cancelButton = ActionButton("되돌리기", CancelCurrent, "cancelData");
            cancelButton.tooltip = "미적용 변경을 버리고 저장된 원본을 다시 읽습니다.";
            header.Add(cancelButton);
            applyButton = ActionButton("적용·저장", ApplyCurrent, "applyData");
            applyButton.tooltip = "편집값을 검사한 뒤 원본에 적용하고 저장합니다.";
            applyButton.AddToClassList("project-data-primary");
            header.Add(applyButton);
            workspace.Add(header);
            detailScroll = new ScrollView(ScrollViewMode.Vertical);
            detailScroll.AddToClassList("project-data-scroll");
            detail = Element("project-data-content");
            SWEditorTheme.ApplyEmbeddedInspector(detail);
            detailScroll.Add(detail);
            workspace.Add(detailScroll);
            body.Add(workspace);
            rootVisualElement.Add(body);
            status = Text(string.Empty, "project-data-status");
            rootVisualElement.Add(status);
            RestoreDraft();
            RefreshCatalog();
            if (session == null && assets.Count > 0)
            {
                SelectAsset(assets[0]);
            }
            else
            {
                RebuildDetail();
            }

            rootVisualElement.schedule.Execute(RefreshStatus).Every(400);
        }

        /// <summary>
        /// 검사·미리보기·참조·시험과 실행 취소를 한 메뉴에 모읍니다. 사용할 수 없는 작업은 비활성화합니다.
        /// </summary>
        private ToolbarMenu CreateToolsMenu()
        {
            var menu = new ToolbarMenu
            {
                text = "도구",
                name = "dataTools"
            };
            menu.AddToClassList("project-data-menu");
            foreach (string title in new[]
            {
                "편집",
                "검사",
                "미리보기",
                "참조·사용처",
                "시험 전투"
            })
            {
                if (title == "시험 전투" && (kind == DataKind.Currency || kind == DataKind.Item))
                {
                    continue;
                }

                string page = title;
                menu.menu.AppendAction(
                    title == "편집" ? "인스펙터" : title,
                    action => ShowPage(page),
                    action =>
                {
                    if (session?.Source == null
                        || creatingData
                        || (page == "시험 전투" && EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        return DropdownMenuAction.Status.Disabled;
                    }

                    return currentPage == page ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });
            }

            menu.menu.AppendSeparator();
            menu.menu.AppendAction("실행 취소", action => Undo.PerformUndo());
            menu.menu.AppendAction("다시 실행", action => Undo.PerformRedo());
            menu.menu.AppendSeparator();
            menu.menu.AppendAction(
                "Project 창에서 선택",
                action => SW.EditorTools.Util.SWEditorUtils.PingAndSelect(session.Source),
                action => session?.Source != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            return menu;
        }

        #endregion // 진입

        #region 탐색과 상태
        /// <summary>
        /// 현재 필터로 목록을 갱신하며 편집 중인 데이터는 유지합니다.
        /// </summary>
        private void RefreshCatalog()
        {
            if (assetList == null)
            {
                return;
            }

            assets = DataCatalog.Find(kind, search);
            assetCount.text = KindName + " · " + assets.Count + "개";
            assetList.itemsSource = assets;
            assetList.Rebuild();
            assetList.SetSelectionWithoutNotify(new[] { session == null ? -1 : assets.IndexOf(session.Source) });
        }

        /// <summary>
        /// SWUtils 목록과 같은 아이콘·자산 이름·타입 순서의 재사용 행을 만듭니다.
        /// </summary>
        private VisualElement MakeListRow()
        {
            var row = Element("project-data-list-row");
            var icon = new Image
            {
                name = "assetIcon",
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.AddToClassList("project-data-asset-icon");
            row.Add(icon);
            var assetName = Text(string.Empty, "project-data-asset-name");
            assetName.name = "assetName";
            row.Add(assetName);
            var assetType = Text(string.Empty, "project-data-asset-type");
            assetType.name = "assetType";
            row.Add(assetType);
            row.RegisterCallback<PointerDownEvent>(change =>
            {
                if (change.button == 0 && change.clickCount == 2 && row.userData is ProjectData asset)
                {
                    EditorGUIUtility.PingObject(asset);
                }
            });
            return row;
        }

        /// <summary>
        /// 실제 자산 이름과 타입을 표시하고 툴팁에서 표시 이름과 전체 경로를 제공합니다.
        /// </summary>
        private void BindListRow(VisualElement row, int index)
        {
            var asset = assets[index];
            string typeName = ObjectNames.NicifyVariableName(asset.GetType().Name);
            row.userData = asset;
            row.Q<Image>("assetIcon").image = AssetPreview.GetMiniThumbnail(asset);
            row.Q<Label>("assetName").text = asset.name;
            row.Q<Label>("assetType").text = typeName;
            row.tooltip = asset.name + " · " + DataCatalog.GetDisplayName(asset) + "\n" + typeName + "\n" + AssetDatabase.GetAssetPath(asset);
            row.EnableInClassList("project-data-selected", session?.Source == asset);
        }

        /// <summary>
        /// 다른 자산으로 이동합니다. 미적용 변경의 적용·취소 선택이 취소되면 기존 편집을 유지합니다.
        /// </summary>
        public bool SelectAsset(ProjectData asset)
        {
            if (!DataCatalog.TryGetKind(asset, out DataKind assetKind) || assetKind != kind)
            {
                notice = "이 창에서는 " + KindName + " 데이터만 편집할 수 있습니다. 해당 분류 창을 열어 주세요.";
                return false;
            }

            if (session != null && session.Source == asset)
            {
                if (creatingData)
                {
                    currentPage = "편집";
                    RebuildDetail();
                }

                return true;
            }

            if (!CanLeaveDraft())
            {
                assetList?.SetSelectionWithoutNotify(new[] { assets.IndexOf(session.Source) });
                return false;
            }

            var next = DataEditSession.Create(asset);
            if (next == null)
            {
                return false;
            }

            detail?.Unbind();
            session?.Dispose();
            session = next;
            assetList?.SetSelectionWithoutNotify(new[] { assets.IndexOf(asset) });
            assetList?.RefreshItems();
            issues.Clear();
            notice = string.Empty;
            currentPage = "편집";
            detailScroll.scrollOffset = Vector2.zero;
            RebuildDetail();
            StoreDraft();
            return true;
        }

        /// <summary>
        /// 변경을 다른 데이터로 잘못 넘기지 않도록 이동 전에 확인합니다.
        /// </summary>
        private bool CanLeaveDraft()
        {
            if (session == null || !session.HasChanges)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex("미적용 변경", "현재 변경을 적용하고 이동할까요?", "검사 후 적용", "이동 취소", "수정 버리기");
            if (choice == 0)
            {
                return ApplyCurrentData();
            }

            return choice == 2;
        }

        /// <summary>
        /// 탐색 페이지를 바꿉니다. 동일 데이터의 미적용 값은 유지합니다.
        /// </summary>
        private void ShowPage(string page)
        {
            currentPage = page;
            detailScroll.scrollOffset = Vector2.zero;
            RebuildDetail();
        }

        /// <summary>
        /// 선택한 페이지의 내용을 다시 만들고 이전 직렬화 연결을 해제합니다.
        /// </summary>
        private void RebuildDetail()
        {
            if (detail == null)
            {
                return;
            }

            creatingData = false;
            ClearCreationTemplate();
            detail.Unbind();
            detail.Clear();
            fieldElements.Clear();
            if (session?.Source == null)
            {
                heading.text = KindName + " 편집기";
                headingType.text = "ScriptableObject";
                headingIcon.image = null;
                detail.Add(Text("새로 만들기를 누르거나 목록에서 데이터를 선택하세요.", "project-data-description"));
                RefreshStatus();
                return;
            }

            heading.text = session.Source.name;
            heading.tooltip = AssetDatabase.GetAssetPath(session.Source);
            headingType.text = currentPage == "편집" ? ObjectNames.NicifyVariableName(session.Source.GetType().Name) : currentPage;
            headingIcon.image = AssetPreview.GetMiniThumbnail(session.Source);
            switch (currentPage)
            {
                case "검사":
                    BuildValidation();
                    break;
                case "미리보기":
                    detail.Add(DataPreview.Create(session.Draft));
                    break;
                case "참조·사용처":
                    BuildReferences();
                    break;
                case "시험 전투":
                    BuildTrial();
                    break;
                default:
                    BuildFields();
                    break;
            }

            RefreshStatus();
        }

        /// <summary>
        /// 편집 상태와 원본 충돌, 실행 중 작업 제한을 표시합니다.
        /// </summary>
        private void RefreshStatus()
        {
            if (status == null)
            {
                return;
            }

            bool active = session?.Source != null;
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            applyButton.SetEnabled(active && !creatingData && !playing && session.HasChanges && !session.HasExternalChanges);
            createButton.SetEnabled(!playing);
            duplicateButton.SetEnabled(active && !playing);
            cancelButton.SetEnabled(active && !creatingData && (session.HasChanges || session.HasExternalChanges));
            inspectorButton.EnableInClassList("project-data-hidden", !creatingData && currentPage == "편집");
            string state = !active
                ? "선택된 데이터 없음"
                : session.HasExternalChanges
                    ? "원본 외부 변경 · 다시 읽기 필요"
                    : session.HasChanges
                        ? "미적용 변경 있음 · 원본 유지 중"
                        : "저장된 데이터";
            status.text = (playing ? "전투 실행 중 · 생성과 적용 제한\n" : string.Empty) + state + (string.IsNullOrEmpty(notice) ? string.Empty : "\n" + notice);
        }

        /// <summary>
        /// 외부 파일 변경 이후 목록을 갱신하되 초안은 덮어쓰지 않습니다.
        /// </summary>
        private void OnProjectChanged()
        {
            RefreshCatalog();
            RefreshStatus();
        }

        /// <summary>
        /// 실행 취소된 원본을 저장하고 깨끗한 편집 사본에 반영합니다. 초안이 있으면 충돌을 안내합니다.
        /// </summary>
        private void OnUndoRedo()
        {
            if (session?.Source != null && session.HasExternalChanges && !session.HasChanges)
            {
                AssetDatabase.SaveAssetIfDirty(session.Source);
                session.Reload();
            }

            RebuildDetail();
            RefreshCatalog();
        }

        /// <summary>
        /// 창 닫기·재컴파일에 대비해 현재 편집기의 세션 저장소에 초안을 보관합니다.
        /// </summary>
        private void StoreDraft()
        {
            if (session?.Source == null)
            {
                return;
            }

            SessionState.SetString(StateKey + "source", AssetDatabase.GetAssetPath(session.Source));
            SessionState.SetString(StateKey + "draft", session.HasChanges ? EditorJsonUtility.ToJson(session.Draft) : string.Empty);
            SessionState.SetString(StateKey + "snapshot", session.SourceSnapshot);
        }

        /// <summary>
        /// 같은 Unity 세션에서 마지막으로 열었던 데이터와 초안을 복원합니다.
        /// </summary>
        private void RestoreDraft()
        {
            if (session != null)
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ProjectData>(SessionState.GetString(StateKey + "source", string.Empty));
            if (!DataCatalog.TryGetKind(asset, out DataKind assetKind) || assetKind != kind)
            {
                return;
            }

            session = DataEditSession.Create(asset);
            session?.RestoreDraft(
                SessionState.GetString(StateKey + "draft", string.Empty),
                SessionState.GetString(StateKey + "snapshot", string.Empty));
        }

        #endregion // 탐색과 상태

        #region 공통 화면 요소
        /// <summary>
        /// 공통 스타일을 가진 컨테이너를 만듭니다.
        /// </summary>
        private static VisualElement Element(string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            return element;
        }

        /// <summary>
        /// 지정한 역할의 텍스트를 만듭니다.
        /// </summary>
        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        /// <summary>
        /// 확정된 편집 작업을 실행하는 버튼을 만듭니다.
        /// </summary>
        private static Button ActionButton(string label, Action action, string name = null)
        {
            return new Button(action)
            {
                text = label,
                name = name ?? label
            };
        }

        #endregion // 공통 화면 요소
    }
}
