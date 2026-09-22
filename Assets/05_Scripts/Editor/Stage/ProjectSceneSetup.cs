using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

using SW.Pooling;
using SW.Popup;
using SW.Util;

using ProjectT.Data;
using ProjectT.Initialization;
using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// Main의 공통 관리자와 스테이지의 장면별 참조를 구성합니다.
    /// </summary>
    public static class ProjectSceneSetup
    {
        #region 필드
        private const string MainScene = "Assets/01_Scenes/Main.unity";
        private const string Prefabs = "Assets/04_Prefabs/";
        private const string Catalogs = "Assets/02_Res/Data/Pooling/";

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 기존 자산을 유지하며 관리자·체력바·공방·장면 구조를 갱신하고 저장합니다.
        /// </summary>
        public static string Apply()
        {
            if (EditorApplication.isPlaying
                || Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty))
            {
                SWLog.LogWarning("[ProjectSceneSetup] 작업 중단: " + "저장된 편집 모드 장면에서 실행해야 합니다.");
                return "실패: " + "저장된 편집 모드 장면에서 실행해야 합니다.";
            }

            ConfigureAssets();
            Scene main = EditorSceneManager.OpenScene(MainScene);
            EnsureInstance("Pooling/SWPool.prefab", "SWPool");
            EnsureInstance("Popup/SWPopupManager.prefab", "SWPopupManager");
            EnsureInstance("Data/DataManager.prefab", "DataManager");
            var entry = EnsureInstance("Initialization/SceneServices.prefab", "SceneServices");
            if (entry.GetComponent<MainSceneEntry>() == null)
            {
                entry.AddComponent<MainSceneEntry>();
            }

            SetCatalog("MainPoolData");
            EditorSceneManager.SaveScene(main);
            Scene stage = EditorSceneManager.OpenScene(StageOneSceneBuilder.ScenePath);
            ConfigureStage();
            EditorSceneManager.SaveScene(stage);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScene, true),
                new EditorBuildSettingsScene(StageOneSceneBuilder.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MainScene);
            return "Main 초기화 → Stage01, 공통 관리자, 씬별 풀 목록, 공방·체력바 및 계층 연결 완료";
        }

        /// <summary>
        /// 공통 관리자와 장면별 풀 목록에 필요한 자산을 준비합니다.
        /// </summary>
        private static void ConfigureAssets()
        {
            foreach (string folder in new[]
            {
                Prefabs + "Data",
                Prefabs + "Initialization",
                Catalogs.TrimEnd('/')
            })
            {
                StageOneSceneBuilder.EnsureFolder(folder);
            }

            HealthBarPrefabSetup.ConfigurePrefabs();
            foreach (string name in new[]
            {
                "CharacterUnit",
                "EnemyUnit"
            })
            {
                string path = Prefabs + "Units/" + name + ".prefab";
                EditPrefab(path, root => HealthBarPrefabSetup.ConfigureUnit(root));
            }

            ArcaneWorkshopBuilder.ConfigurePrefab();
            foreach (string path in new[]
            {
                "Pooling/SWPool.prefab",
                "Pooling/SWPoolRegistry.prefab",
                "Popup/SWPopupManager.prefab"
            })
            {
                EditPrefab(Prefabs + path, root => root.name = System.IO.Path.GetFileNameWithoutExtension(path));
            }

            CreateConfiguredPrefab<DataManager>(
                "Data/DataManager.prefab",
                manager => StageOneGameplayBuilder.Set(
                manager,
                "colorData",
                AssetDatabase.LoadAssetAtPath<ColorData>("Assets/02_Res/Data/Common/ColorData.asset"),
                "spriteData",
                AssetDatabase.LoadAssetAtPath<SpriteData>("Assets/02_Res/Data/Common/SpriteData.asset")));
            CreateConfiguredPrefab<SceneServices>(
                "Initialization/SceneServices.prefab",
                services => StageOneGameplayBuilder.Set(
                services,
                "dataManagerPrefab",
                LoadComponent<DataManager>("Data/DataManager.prefab"),
                "poolPrefab",
                LoadComponent<SWPool>("Pooling/SWPool.prefab"),
                "popupManagerPrefab",
                LoadComponent<SWPopupManager>("Popup/SWPopupManager.prefab")));
            CreateCatalog("MainPoolData", Array.Empty<string>(), Array.Empty<int>());
            CreateCatalog(
                "Stage01PoolData",
                new[] { "Units/Character/OrcWarriorRedUnit.prefab", "Units/Character/OrcMageRedUnit.prefab", "Units/Enemy/SkeletonBasicUnit.prefab", "Effects/AttackTrace.prefab" },
                new[] { 2, 2, 12, 4 });
        }

        /// <summary>
        /// 현재 스테이지를 공통 초기화와 종류별 계층에 연결합니다.
        /// </summary>
        public static void ConfigureStage()
        {
            if (SceneManager.GetActiveScene().path != StageOneSceneBuilder.ScenePath)
            {
                SWLog.LogWarning("[ProjectSceneSetup] 작업 중단: " + "Stage01을 열어 주세요.");
                return;
            }

            GameObject oldPool = GameObject.Find("BattleObjectPool");
            if (oldPool != null)
            {
                UnityEngine.Object.DestroyImmediate(oldPool);
            }

            GameObject oldWorkshop = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(value => value.name == "MobileArcaneWorkshop")?.gameObject;
            ArcaneWorkshopBuilder.Apply();
            if (oldWorkshop != null)
            {
                UnityEngine.Object.DestroyImmediate(oldWorkshop);
            }

            EnsureInstance("Initialization/SceneServices.prefab", "SceneServices");
            SetCatalog("Stage01PoolData");
            Transform objects = Root("Objects");
            Transform stationary = Child(objects, "StaticObject");
            Transform dynamic = Child(objects, "DynamicObject");
            Transform interfaces = Root("UIs");
            RenameAndParent("Stage01Terrain", "TilemapGrid", dynamic);
            RenameAndParent("TilemapGrid", "TilemapGrid", dynamic);
            RenameAndParent("TerrainGrid", "Grid", GameObject.Find("TilemapGrid").transform);
            foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (!tilemap.name.EndsWith("_Tilemap", StringComparison.Ordinal))
                {
                    tilemap.name += "_Tilemap";
                }
            }

            foreach (string name in new[]
            {
                "EnvironmentScenery",
                "ArcaneWorkshop",
                "WalkableBattlefield",
                "EnemyEntrance"
            })
            {
                RenameAndParent(name, name, stationary);
            }

            RenameAndParent("DefenseExit", "WorkshopAttackPoint", stationary);
            RenameAndParent("WorkshopAttackPoint", "WorkshopAttackPoint", stationary);
            RenameAndParent("BattleUnits", "BattleUnits", dynamic);
            RenameAndParent("BattleCanvas", "Canvas", interfaces);
            RenameAndParent("EventSystem", "EventSystem", interfaces);
            RenameAndParent("Stage01Battle", "BattleController", null);
            GameObject unusedSpawn = GameObject.Find("AllySpawnPoint");
            if (unusedSpawn != null)
            {
                UnityEngine.Object.DestroyImmediate(unusedSpawn);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// 프리팹을 열어 설정을 저장한 뒤 임시 내용을 반드시 해제합니다.
        /// </summary>
        private static void EditPrefab(string path, Action<GameObject> action)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                action(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 공통 프리팹에서 지정한 컴포넌트를 읽습니다.
        /// </summary>
        private static T LoadComponent<T>(string path)
            where T : Component
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + path).GetComponent<T>();
        }

        /// <summary>
        /// 관리자 프리팹을 생성하거나 기존 프리팹의 설정을 갱신합니다.
        /// </summary>
        private static void CreateConfiguredPrefab<T>(string path, Action<T> configure)
            where T : Component
        {
            string fullPath = Prefabs + path;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fullPath) != null)
            {
                EditPrefab(fullPath, root => configure(root.GetComponent<T>()));
                return;
            }

            GameObject instance = new GameObject(typeof(T).Name);
            try
            {
                configure(instance.AddComponent<T>());
                PrefabUtility.SaveAsPrefabAsset(instance, fullPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// 장면의 기존 프리팹 인스턴스를 재사용하거나 새로 배치합니다.
        /// </summary>
        private static GameObject EnsureInstance(string path, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + path);
            GameObject existing = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(value => value.name == name || PrefabUtility.GetCorrespondingObjectFromSource(value) == prefab);
            if (existing == null)
            {
                existing = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }

            existing.name = name;
            return existing;
        }

        /// <summary>
        /// 장면에서 사용할 프리팹과 예열 수량을 풀 목록으로 저장합니다.
        /// </summary>
        private static void CreateCatalog(string name, string[] paths, int[] counts)
        {
            string path = Catalogs + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<SWPoolCatalog>(path) != null)
            {
                return;
            }

            var catalog = ScriptableObject.CreateInstance<SWPoolCatalog>();
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("poolEntries");
            entries.arraySize = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("poolName").stringValue = System.IO.Path.GetFileNameWithoutExtension(paths[index]);
                entry.FindPropertyRelative("groupName").stringValue = paths[index].StartsWith("Units/") ? "Units" : "Effects";
                entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + paths[index]);
                entry.FindPropertyRelative("prewarmCount").intValue = counts[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, path);
        }

        /// <summary>
        /// 장면의 풀 등록기에 해당 장면의 풀 목록을 연결합니다.
        /// </summary>
        private static void SetCatalog(string name)
        {
            var registry = EnsureInstance("Pooling/SWPoolRegistry.prefab", "SWPoolRegistry").GetComponent<SWPoolRegistry>();
            StageOneGameplayBuilder.Set(registry, "poolCatalog", AssetDatabase.LoadAssetAtPath<SWPoolCatalog>(Catalogs + name + ".asset"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(registry);
        }

        /// <summary>
        /// 이름이 일치하는 루트 객체를 찾거나 생성합니다.
        /// </summary>
        private static Transform Root(string name)
        {
            return GameObject.Find(name)?.transform ?? new GameObject(name).transform;
        }

        /// <summary>
        /// 지정 부모 아래의 자식을 찾거나 생성합니다.
        /// </summary>
        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child;
            }

            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        /// <summary>
        /// 기존 객체의 이름과 부모를 변경하면서 월드 위치를 유지합니다.
        /// </summary>
        private static void RenameAndParent(string currentName, string name, Transform parent)
        {
            GameObject target = GameObject.Find(currentName);
            if (target == null)
            {
                return;
            }

            target.name = name;
            target.transform.SetParent(parent, true);
        }

        #endregion // 함수
    }
}
