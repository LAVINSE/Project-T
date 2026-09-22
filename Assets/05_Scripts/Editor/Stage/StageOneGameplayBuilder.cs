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
            var deploymentCurrency = AssetDatabase.LoadAssetAtPath<CurrencyData>("Assets/02_Res/Data/Currency/BattleCoinData.asset");
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

            var warrior = AssetDatabase.LoadAssetAtPath<UnitClassData>("Assets/02_Res/Data/Character/OrcWarriorRedData.asset");
            var mage = AssetDatabase.LoadAssetAtPath<UnitClassData>("Assets/02_Res/Data/Character/OrcMageRedData.asset");
            var enemy = AssetDatabase.LoadAssetAtPath<UnitEnemyData>("Assets/02_Res/Data/Enemy/SkeletonBasicData.asset");
            if (warrior == null || !warrior.IsValid || mage == null || !mage.IsValid || enemy == null || !enemy.IsValid)
            {
                SWLog.LogWarning("[StageOneGameplayBuilder] 생성 실패: 종류별 유닛 데이터와 프리팹을 먼저 설정하세요.");
                return "실패: 유닛 데이터와 프리팹을 확인하세요.";
            }

            StageOneSceneBuilder.EnsureFolder(Prefabs.TrimEnd('/'));
            lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(ProjectAssetPaths.BattleLine);
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(lineMaterial, ProjectAssetPaths.BattleLine);
            }

            var stage = Asset<StageData>("Stage01");
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
                AssetDatabase.LoadAssetAtPath<EnemyRouteData>(ProjectAssetPaths.EnemyRoute),
                "enemy",
                enemy,
                "classes",
                new UnityEngine.Object[] { warrior, mage });
            var traceObject = new GameObject("AttackTrace", typeof(LineRenderer), typeof(AttackTrace));
            ConfigureLine(traceObject.GetComponent<LineRenderer>(), Color.white, 0.07f, 3000, 2);
            AttackTrace tracePrefab = PrefabUtility.SaveAsPrefabAsset(traceObject, "Assets/04_Prefabs/Effects/AttackTrace.prefab").GetComponent<AttackTrace>();
            UnityEngine.Object.DestroyImmediate(traceObject);
            var battleRoot = new GameObject("BattleController");
            var pause = battleRoot.AddComponent<BattlePauseController>();
            var units = new GameObject("BattleUnits").transform;
            var session = battleRoot.AddComponent<BattleSession>();
            var terrain = UnityEngine.Object.FindFirstObjectByType<WalkableBattlefield>();
            Set(session, "definition", stage, "battlefield", terrain, "pauseController", pause, "unitParent", units);
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
