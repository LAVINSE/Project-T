using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using TMPro;

using SW.Pooling;
using SW.Popup;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Presentation;
using ProjectT.Timing;
using ProjectT.Units;

namespace ProjectT.Editor
{
    /// <summary>
    /// 확정한 첫 스테이지의 데이터·프리팹·마우스 화면을 편집기에서 연결합니다.
    /// </summary>
    public static class StageOneGameplayBuilder
    {
        #region 필드
        private const string Prefabs = "Assets/04_Prefabs/Units/";
        private static readonly Color Ink = new Color32(21, 34, 33, 248);
        private static readonly Color Paper = new Color32(238, 226, 194, 255);
        private static readonly Color Muted = new Color32(174, 187, 168, 255);
        private static readonly Color Accent = new Color32(225, 178, 90, 255);
        private static TMP_FontAsset font;
        private static Material lineMaterial;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 실행 가능한 스테이지 1을 현재 초원 지형에 연결하고 빌드 진입점으로 등록합니다.
        /// </summary>
        public static string Create()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != StageOneSceneBuilder.ScenePath)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 작업 중단: " + "스테이지 1을 편집 모드에서 열어 주세요.");
                return "실패: " + "스테이지 1을 편집 모드에서 열어 주세요.";
            }

            if (UnityEngine.Object.FindFirstObjectByType<BattleSession>() != null)
            {
                return "이미 연결된 전투를 보존했습니다.";
            }

            StageOneSceneBuilder.EnsureFolder(Prefabs.TrimEnd('/'));
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/07_Fonts/TMP/NotoSansKR-Regular SDF.asset");
            if (font == null)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 작업 중단: " + "한국어 글꼴을 찾지 못했습니다.");
                return "실패: " + "한국어 글꼴을 찾지 못했습니다.";
            }

            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(lineMaterial, ProjectAssetPaths.BattleLine);
            string warriorFolder = "Assets/RafaelMatos/ERW-Grass Land/Characters/warrior/";
            UnitAppearance warriorAppearance = Appearance(
                "WarriorAppearance",
                warriorFolder + "warrior-idle.png",
                warriorFolder + "warrior-run.png",
                warriorFolder + "warrior-single swing 1.png",
                warriorFolder + "warrior-death.png");
            string magePrefix = "Assets/RafaelMatos/ERW-Grassland 2.0/Characters/orc mage/orc1/orc mage - with hand fx-";
            UnitAppearance mageAppearance = Appearance(
                "MageAppearance",
                magePrefix + "idle.png",
                magePrefix + "walk.png",
                magePrefix + "atk1.png",
                magePrefix + "death.png");
            string enemyPrefix = "Assets/RafaelMatos/ERW-Crypt/Characters/Skeleton/skeleton-variation1-";
            UnitAppearance enemyAppearance = Appearance(
                "SkeletonAppearance",
                enemyPrefix + "idle.png",
                enemyPrefix + "walk.png",
                enemyPrefix + "attack.png",
                enemyPrefix + "death.png");
            if (warriorAppearance == null || mageAppearance == null || enemyAppearance == null)
            {
                return "실패: 유닛 외형의 필수 스프라이트를 확인해 주세요.";
            }

            var warrior = Asset<AllyClassDefinition>("Warrior");
            Set(
                warrior,
                "displayName",
                "전사",
                "deploymentCost",
                30d,
                "maximumHealth",
                150f,
                "moveSpeed",
                2.8f,
                "attackDamage",
                18f,
                "attackRange",
                1.05f,
                "attackInterval",
                0.8f,
                "blockCapacity",
                1,
                "revivalSeconds",
                12f,
                "appearance",
                warriorAppearance);
            var mage = Asset<AllyClassDefinition>("Mage");
            Set(
                mage,
                "displayName",
                "마법사",
                "deploymentCost",
                40d,
                "maximumHealth",
                80f,
                "moveSpeed",
                2.5f,
                "attackDamage",
                24f,
                "attackRange",
                4.5f,
                "attackInterval",
                1.35f,
                "blockCapacity",
                0,
                "revivalSeconds",
                16f,
                "appearance",
                mageAppearance);
            var enemy = Asset<EnemyDefinition>("Skeleton");
            Set(
                enemy,
                "maximumHealth",
                72f,
                "moveSpeed",
                1.2f,
                "attackDamage",
                10f,
                "attackRange",
                1.05f,
                "attackInterval",
                1.4f,
                "workshopAttackDamage",
                10f,
                "workshopAttackInterval",
                1.4f,
                "killReward",
                10d,
                "appearance",
                enemyAppearance);
            var stage = Asset<StageDefinition>("Stage01");
            Set(
                stage,
                "displayName",
                "초원 경계",
                "startingCurrency",
                100d,
                "workshopMaximumHealth",
                300f,
                "spawnInterval",
                2.8f,
                "enemiesPerRound",
                new[] { 6, 9, 12 },
                "enemyRoute",
                AssetDatabase.LoadAssetAtPath<EnemyRouteDefinition>(ProjectAssetPaths.EnemyRoute),
                "enemy",
                enemy,
                "classes",
                new UnityEngine.Object[] { warrior, mage });
            AllyUnit allyPrefab = UnitPrefab(true).GetComponent<AllyUnit>();
            EnemyUnit enemyPrefab = UnitPrefab(false).GetComponent<EnemyUnit>();
            var traceObject = new GameObject("AttackTrace", typeof(LineRenderer), typeof(AttackTrace));
            ConfigureLine(traceObject.GetComponent<LineRenderer>(), Color.white, 0.07f, 3000, 2);
            AttackTrace tracePrefab = PrefabUtility.SaveAsPrefabAsset(traceObject, "Assets/04_Prefabs/Effects/AttackTrace.prefab").GetComponent<AttackTrace>();
            UnityEngine.Object.DestroyImmediate(traceObject);
            var battleRoot = new GameObject("BattleController");
            var pause = battleRoot.AddComponent<BattlePauseController>();
            var units = new GameObject("BattleUnits").transform;
            var session = battleRoot.AddComponent<BattleSession>();
            var terrain = UnityEngine.Object.FindFirstObjectByType<WalkableBattlefield>();
            Set(
                session,
                "definition",
                stage,
                "battlefield",
                terrain,
                "pauseController",
                pause,
                "allyPrefab",
                allyPrefab,
                "enemyPrefab",
                enemyPrefab,
                "unitParent",
                units);
            var command = battleRoot.AddComponent<BattleMouseCommand>();
            Set(command, "session", session);
            var attacks = battleRoot.AddComponent<BattleAttackPresentation>();
            Set(attacks, "session", session, "tracePrefab", tracePrefab);
            BuildScreen(session, command, pause);
            StageOneInteractionBuilder.Apply();
            ProjectSceneSetup.ConfigureStage();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "스테이지 1 연결 완료: 전사·마법사, 3라운드, 무료 부활, 마우스 조작";
        }

        /// <summary>
        /// 기존 외형 자산의 프레임 순서와 타격 시점을 새로 연결합니다.
        /// </summary>
        public static UnitAppearance Appearance(string name, string idle, string move, string attack, string death)
        {
            Sprite[] idleFrames = StageOneSceneBuilder.LoadSprites(idle);
            Sprite[] moveFrames = StageOneSceneBuilder.LoadSprites(move);
            Sprite[] attackFrames = StageOneSceneBuilder.LoadSprites(attack);
            Sprite[] deathFrames = StageOneSceneBuilder.LoadSprites(death);
            if (idleFrames.Length == 0 || moveFrames.Length == 0 || attackFrames.Length == 0 || deathFrames.Length == 0)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 외형 설정 실패: 필수 스프라이트가 없습니다. " + name);
                return null;
            }

            var appearance = AssetDatabase.LoadAssetAtPath<UnitAppearance>(ProjectAssetPaths.Data(typeof(UnitAppearance), name)) ?? Asset<UnitAppearance>(name);
            Sprite first = idleFrames[0];
            Bounds visible = SpriteVisibleBounds.Read(first);
            float scale = first.pixelsPerUnit / 32f;
            bool configured = Set(
                appearance,
                "idleFrames",
                idleFrames,
                "moveFrames",
                moveFrames,
                "attackFrames",
                attackFrames,
                "deathFrames",
                deathFrames,
                "feetOffset",
                -visible.min.y * scale,
                "healthBarHeight",
                visible.size.y * scale + 0.2f,
                "attackImpactFrame",
                name == "WarriorAppearance" ? 2 : name == "MageAppearance" ? 9 : 11);
            return configured ? appearance : null;
        }

        /// <summary>
        /// 아군 또는 적의 수명·외형·체력바를 연결한 프리팹을 저장합니다.
        /// </summary>
        private static GameObject UnitPrefab(bool ally)
        {
            var instance = new GameObject(ally ? "AllyUnit" : "EnemyUnit");
            if (ally)
            {
                instance.AddComponent<AllyUnit>();
            }
            else
            {
                instance.AddComponent<EnemyUnit>();
            }

            var presentation = instance.AddComponent<UnitPresentation>();
            var character = new GameObject("CharacterVisual", typeof(SpriteRenderer));
            character.transform.SetParent(instance.transform);
            HealthBarPresentation healthBar = HealthBarPrefabSetup.ConfigureUnit(instance);
            LineRenderer ring = Line("SelectionRing", instance.transform, new Color(1f, 0.86f, 0.4f), 0.06f, 1000, 32);
            ring.loop = true;
            ring.enabled = false;
            LineRenderer arrow = Line("NewUnitArrow", instance.transform, new Color(1f, 0.86f, 0.4f), 0.1f, 2002, 5);
            arrow.enabled = false;
            Set(
                presentation,
                "characterRenderer",
                character.GetComponent<SpriteRenderer>(),
                "healthBar",
                healthBar,
                "selectionRing",
                ring,
                "newUnitArrow",
                arrow);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, Prefabs + (ally ? "AllyUnit" : "EnemyUnit") + ".prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>
        /// 전투 화면과 배치·일시 정지 조작을 구성합니다.
        /// </summary>
        private static void BuildScreen(BattleSession session, BattleMouseCommand command, BattlePauseController pause)
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var screen = canvasObject.AddComponent<BattleScreen>();
            Transform header = Panel(
                "BattleHeader",
                canvasObject.transform,
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, -94),
                Vector2.zero,
                Ink);
            header.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(36, 36, 12, 12);
            headerLayout.spacing = 30;
            headerLayout.childControlHeight = true;
            headerLayout.childControlWidth = true;
            TMP_Text title = Text("StageLabel", header, "01  /  초원 경계", 30, Paper);
            Width(title, 440);
            TMP_Text rounds = Text("RoundLabel", header, "전투 준비", 25, Paper);
            Width(rounds, 310);
            TMP_Text defense = Text("DefenseLabel", header, "방어 여유  10 / 10", 25, Paper);
            Width(defense, 300);
            TMP_Text currency = Text("CurrencyLabel", header, "배치 재화  100", 27, Accent);
            Width(currency, 280);
            var menuButton = Button("MenuButton", header, "메뉴", 145, 58, false);
            Transform footer = Panel(
                "CommandPanel",
                canvasObject.transform,
                Vector2.zero,
                new Vector2(1, 0),
                Vector2.zero,
                new Vector2(0, 200),
                Ink);
            footer.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.padding = new RectOffset(36, 36, 16, 16);
            footerLayout.spacing = 14;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            TMP_Text hint = Text("CommandHint", footer, "클래스 구매 → 화살표의 아군을 왼쪽 클릭 → 오른쪽 클릭으로 이동", 22, Muted);
            Height(hint, 32);
            Transform controls = Panel("CommandControls", footer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear);
            controls.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            var controlsLayout = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 22;
            controlsLayout.childControlWidth = true;
            controlsLayout.childControlHeight = true;
            var warriorButton = Button("PurchaseWarriorButton", controls, "전사   30\n<size=19>근접 공격 · 적 1명 저지</size>", 330, 98, false);
            var mageButton = Button("PurchaseMageButton", controls, "마법사   40\n<size=19>원거리 공격 · 저지 없음</size>", 330, 98, false);
            TMP_Text selected = Text("SelectedUnitLabel", controls, "아군을 클릭해 선택하세요.", 23, Paper);
            Width(selected, 630);
            selected.GetComponent<LayoutElement>().flexibleWidth = 1;
            var startButton = Button("StartBattleButton", controls, "전투 시작", 250, 98, true);
            Transform rosterFrame = Panel(
                "AllyRoster",
                canvasObject.transform,
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(36, -162),
                new Vector2(-36, -102),
                new Color32(21, 34, 33, 210));
            var scroll = rosterFrame.gameObject.AddComponent<ScrollRect>();
            var mask = rosterFrame.gameObject.AddComponent<RectMask2D>();
            Transform roster = Panel(
                "AllyRosterContent",
                rosterFrame,
                new Vector2(0, 0),
                new Vector2(0, 1),
                Vector2.zero,
                Vector2.zero,
                Color.clear);
            var rosterRect = (RectTransform)roster;
            rosterRect.pivot = new Vector2(0, 0.5f);
            var rosterLayout = roster.gameObject.AddComponent<HorizontalLayoutGroup>();
            rosterLayout.spacing = 8;
            rosterLayout.childControlWidth = true;
            rosterLayout.childControlHeight = true;
            rosterLayout.childForceExpandWidth = false;
            rosterLayout.padding = new RectOffset(8, 8, 4, 4);
            roster.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = rosterRect;
            scroll.viewport = (RectTransform)rosterFrame;
            scroll.horizontal = true;
            scroll.vertical = false;
            var rosterTemplate = Button("AllyRosterButtonTemplate", roster, "전사", 230, 50, false);
            TMP_Text rosterLabel = rosterTemplate.GetComponentInChildren<TMP_Text>();
            rosterLabel.fontSize = 22;
            rosterLabel.textWrappingMode = TextWrappingModes.NoWrap;
            rosterTemplate.gameObject.SetActive(false);
            SWPopupBase menu = Popup("PauseMenuPopup", "잠시 작전을 정리하세요", canvasObject.transform, pause, out Transform menuContent);
            Text("PauseDescription", menuContent, "이동 · 공격 · 부활 시간이 멈춰 있습니다.", 25, Muted);
            var resume = Button("ResumeButton", menuContent, "계속하기", 560, 70, true);
            var restart = Button("RestartButton", menuContent, "스테이지 다시 시작", 560, 65, false);
            menu.gameObject.SetActive(false);
            SWPopupBase result = Popup("BattleResultPopup", "", canvasObject.transform, pause, out Transform resultContent);
            TMP_Text resultTitle = resultContent.Find("Title").GetComponent<TMP_Text>();
            TMP_Text resultDescription = Text("ResultDescription", resultContent, "", 28, Paper);
            var retry = Button("RetryButton", resultContent, "다시 도전", 560, 75, true);
            result.gameObject.SetActive(false);
            Set(
                screen,
                "session",
                session,
                "commands",
                command,
                "stageLabel",
                title,
                "currencyLabel",
                currency,
                "roundLabel",
                rounds,
                "defenseLabel",
                defense,
                "commandLabel",
                hint,
                "selectedLabel",
                selected,
                "purchaseButtons",
                new UnityEngine.Object[] { warriorButton, mageButton },
                "startButton",
                startButton,
                "startLabel",
                startButton.GetComponentInChildren<TMP_Text>(),
                "menuButton",
                menuButton,
                "menuPopup",
                menu,
                "resumeButton",
                resume,
                "restartButton",
                restart,
                "resultPopup",
                result,
                "resultTitle",
                resultTitle,
                "resultDescription",
                resultDescription,
                "retryButton",
                retry,
                "rosterParent",
                roster,
                "rosterButtonPrefab",
                rosterTemplate);
        }

        /// <summary>
        /// 제목과 본문을 표시하는 팝업을 구성합니다.
        /// </summary>
        private static SWPopupBase Popup(
            string name,
            string title,
            Transform canvas,
            BattlePauseController pause,
            out Transform content)
        {
            Transform overlay = Panel(
                name,
                canvas,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.025f, 0.045f, 0.04f, 0.82f));
            overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var popup = overlay.gameObject.AddComponent<SWPopupBase>();
            popup.SetShowEffect(null, false);
            Set(overlay.gameObject.AddComponent<BattlePopupPause>(), "controller", pause);
            content = Panel(
                "Content",
                overlay,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-350, -210),
                new Vector2(350, 210),
                Ink);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 36, 36);
            layout.spacing = 22;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            TMP_Text heading = Text("Title", content, title, 36, Accent);
            Height(heading, 65);
            return popup;
        }

        /// <summary>
        /// 부모 아래에 배경 패널을 생성합니다.
        /// </summary>
        private static Transform Panel(
            string name,
            Transform parent,
            Vector2 minimumAnchor,
            Vector2 maximumAnchor,
            Vector2 minimumOffset,
            Vector2 maximumOffset,
            Color color)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rectangle = instance.GetComponent<RectTransform>();
            rectangle.SetParent(parent, false);
            rectangle.anchorMin = minimumAnchor;
            rectangle.anchorMax = maximumAnchor;
            rectangle.offsetMin = minimumOffset;
            rectangle.offsetMax = maximumOffset;
            instance.GetComponent<UnityEngine.UI.Image>().color = color;
            instance.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            return rectangle;
        }

        /// <summary>
        /// 전투 화면에 사용할 글자 객체와 글꼴·색상을 설정합니다.
        /// </summary>
        private static TMP_Text Text(string name, Transform parent, string value, float size, Color color)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            instance.transform.SetParent(parent, false);
            var text = instance.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 버튼 배경과 문구를 구성하고 강조 색상을 적용합니다.
        /// </summary>
        private static UnityEngine.UI.Button Button(
            string name,
            Transform parent,
            string label,
            float width,
            float height,
            bool highlighted)
        {
            Transform body = Panel(
                name,
                parent,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                highlighted ? Accent : new Color32(48, 69, 61, 255));
            body.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var layout = body.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            var button = body.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = body.GetComponent<UnityEngine.UI.Image>();
            TMP_Text caption = Text("Label", body, label, 27, highlighted ? Ink : Paper);
            caption.alignment = TextAlignmentOptions.Center;
            var rectangle = caption.GetComponent<RectTransform>();
            rectangle.anchorMin = Vector2.zero;
            rectangle.anchorMax = Vector2.one;
            rectangle.offsetMin = new Vector2(12, 4);
            rectangle.offsetMax = new Vector2(-12, -4);
            return button;
        }

        /// <summary>
        /// 레이아웃 요소의 기준 가로 크기를 설정합니다.
        /// </summary>
        private static void Width(TMP_Text text, float value)
        {
            text.GetComponent<LayoutElement>().preferredWidth = value;
        }

        /// <summary>
        /// 레이아웃 요소의 기준 세로 크기를 설정합니다.
        /// </summary>
        private static void Height(TMP_Text text, float value)
        {
            text.GetComponent<LayoutElement>().preferredHeight = value;
        }

        /// <summary>
        /// 표시용 선 객체를 생성하고 렌더러를 설정합니다.
        /// </summary>
        private static LineRenderer Line(string name, Transform parent, Color color, float width, int order, int count)
        {
            var instance = new GameObject(name, typeof(LineRenderer));
            instance.transform.SetParent(parent);
            var line = instance.GetComponent<LineRenderer>();
            ConfigureLine(line, color, width, order, count);
            return line;
        }

        /// <summary>
        /// 선 렌더러의 색상·너비·정렬 순서·꼭짓점 수를 설정합니다.
        /// </summary>
        private static void ConfigureLine(LineRenderer line, Color color, float width, int order, int count)
        {
            line.sharedMaterial = lineMaterial;
            line.positionCount = count;
            line.useWorldSpace = true;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.sortingOrder = order;
        }

        /// <summary>
        /// 데이터 유형에 맞는 폴더에서 자산을 재사용하거나 생성합니다.
        /// </summary>
        private static T Asset<T>(string name)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(ProjectAssetPaths.Data(typeof(T), name));
            if (existing != null)
            {
                return existing;
            }

            var value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, ProjectAssetPaths.Data(typeof(T), name));
            return value;
        }

        /// <summary>
        /// 편집기 직렬화 속성으로 데이터와 장면 참조를 기록합니다.
        /// </summary>
        public static bool Set(UnityEngine.Object target, params object[] values)
        {
            if (target == null || values == null || values.Length % 2 != 0)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 속성 설정 실패: 대상과 이름·값 쌍을 확인해 주세요.");
                return false;
            }

            var serialized = new SerializedObject(target);
            for (int index = 0; index < values.Length; index += 2)
            {
                if (!(values[index] is string propertyName))
                {
                    SWLog.LogWarning("[StageOneGameplayBuilder] 속성 설정 실패: 속성 이름은 문자열이어야 합니다.");
                    return false;
                }

                var property = serialized.FindProperty(propertyName);
                if (property == null)
                {
                    SWLog.LogWarning("[StageOneGameplayBuilder] 속성 설정 실패: 속성이 없습니다. " + propertyName);
                    return false;
                }

                object value = values[index + 1];
                if (!CanAssign(property, value))
                {
                    SWLog.LogWarning("[StageOneGameplayBuilder] 속성 설정 실패: 값의 형식이 일치하지 않습니다. " + propertyName);
                    return false;
                }

                if (value == null && property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    property.objectReferenceValue = null;
                }
                else if (value is UnityEngine.Object unityObject)
                {
                    property.objectReferenceValue = unityObject;
                }
                else if (value is string text)
                {
                    property.stringValue = text;
                }
                else if (value is float single)
                {
                    property.floatValue = single;
                }
                else if (value is double amount)
                {
                    property.doubleValue = amount;
                }
                else if (value is bool flag)
                {
                    property.boolValue = flag;
                }
                else if (value is int integer)
                {
                    property.intValue = integer;
                }
                else if (value is int[] integers)
                {
                    property.arraySize = integers.Length;
                    for (int item = 0; item < integers.Length; item++)
                    {
                        property.GetArrayElementAtIndex(item).intValue = integers[item];
                    }
                }
                else if (value is UnityEngine.Object[] references)
                {
                    property.arraySize = references.Length;
                    for (int item = 0; item < references.Length; item++)
                    {
                        property.GetArrayElementAtIndex(item).objectReferenceValue = references[item];
                    }
                }
                else
                {
                    SWLog.LogWarning("[StageOneGameplayBuilder] 속성 설정 실패: 지원하지 않는 값입니다. " + propertyName);
                    return false;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        /// <summary>
        /// 직렬화 속성에 값을 대입할 수 있는지 실제 속성 형식으로 검사합니다.
        /// </summary>
        private static bool CanAssign(SerializedProperty property, object value)
        {
            if (value == null || value is UnityEngine.Object)
            {
                return property.propertyType == SerializedPropertyType.ObjectReference;
            }

            if (value is string)
            {
                return property.propertyType == SerializedPropertyType.String;
            }

            if (value is float || value is double)
            {
                return property.propertyType == SerializedPropertyType.Float;
            }

            if (value is bool)
            {
                return property.propertyType == SerializedPropertyType.Boolean;
            }

            if (value is int)
            {
                return property.propertyType == SerializedPropertyType.Integer
                    || property.propertyType == SerializedPropertyType.Enum;
            }

            if (value is int[])
            {
                return property.isArray && property.arrayElementType == "int";
            }

            if (value is UnityEngine.Object[])
            {
                return property.isArray && property.arrayElementType.StartsWith("PPtr<", StringComparison.Ordinal);
            }

            return false;
        }

        #endregion // 함수
    }
}
