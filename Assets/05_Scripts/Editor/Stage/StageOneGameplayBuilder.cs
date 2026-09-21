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
        private static Material lineMaterial;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 실행 가능한 스테이지 1을 현재 초원 지형에 연결하고 빌드 진입점으로 등록합니다.
        /// </summary>
        public static string Create()
        {
            var deploymentCurrency = AssetDatabase.LoadAssetAtPath<CurrencyDefinition>("Assets/02_Res/Data/Currency/BattleCoin.asset");
            if (deploymentCurrency == null)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 생성 실패: 배치 재화 데이터가 없습니다.");
                return string.Empty;
            }
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
                0d,
                "appearance",
                enemyAppearance);
            using (var serialized = new SerializedObject(enemy))
            {
                var rewards = serialized.FindProperty("rewards");
                rewards.arraySize = 1;
                var reward = rewards.GetArrayElementAtIndex(0);
                reward.FindPropertyRelative("definition").objectReferenceValue = deploymentCurrency;
                reward.FindPropertyRelative("useAmountOverride").boolValue = true;
                reward.FindPropertyRelative("overrideAmount").doubleValue = 10d;
                reward.FindPropertyRelative("acquisitionProbability").floatValue = 100f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var stage = Asset<StageDefinition>("Stage01");
            Set(
                stage,
                "displayName",
                "초원 경계",
                "startingCurrency",
                100d,
                "deploymentCurrency",
                deploymentCurrency,
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
            BattleUserInterfaceSetup.CreateForStage();
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
