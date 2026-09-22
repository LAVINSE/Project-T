using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 스테이지 1의 초원 전장을 편집기에서 작성합니다. 기존 장면이 있으면 덮어쓰지 않습니다.
    /// </summary>
    public static class StageOneSceneBuilder
    {
        #region 필드
        /// <summary>
        /// 스테이지 1의 저장 경로입니다.
        /// </summary>
        public const string ScenePath = "Assets/01_Scenes/Stage01_Grassland.unity";
        private const string Grassland = "Assets/RafaelMatos/ERW-Grassland 2.0/";
        private const string Props = Grassland + "Props/Static props/sheet1-sprites/";
        private const string Data = "Assets/02_Res/Data/Navigation";
        private static readonly Vector2[] RoutePoints =
        {
            new Vector2(-15.5f, 3.5f),
            new Vector2(-9.5f, 3.5f),
            new Vector2(-9.5f, -2.5f),
            new Vector2(-2.5f, -2.5f),
            new Vector2(-2.5f, 3.5f),
            new Vector2(5.5f, 3.5f),
            new Vector2(5.5f, -0.5f),
            new Vector2(15.5f, -0.5f)
        };

        #endregion // 필드

        #region 함수
        /// <summary>
        /// Unity CLI에서 호출하여 장면·타일·장애물과 고정 경로를 작성합니다.
        /// </summary>
        public static string CreateEnvironment()
        {
            if (EditorApplication.isPlaying)
            {
                SWLog.LogWarning("[StageOneSceneBuilder] 작업 중단: " + "플레이를 종료한 뒤 장면을 작성해 주세요.");
                return "실패: " + "플레이를 종료한 뒤 장면을 작성해 주세요.";
            }

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                SWLog.LogWarning("[StageOneSceneBuilder] 작업 중단: " + "열린 장면의 변경 내용을 먼저 저장해 주세요.");
                return "실패: " + "열린 장면의 변경 내용을 먼저 저장해 주세요.";
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return "기존 스테이지 장면을 보존했습니다: " + ScenePath;
            }

            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                SWLog.LogWarning("[StageOneSceneBuilder] 작업 중단: " + "이 제작기는 Universal Render Pipeline 전용입니다.");
                return "실패: " + "이 제작기는 Universal Render Pipeline 전용입니다.";
            }

            var route = FixedRoute.Create(RoutePoints);
            Sprite[] terrainSprites = LoadSprites(Grassland + "Tilesets/dirt1 to grass.png");
            Sprite[] grassSprites = LoadSprites("Assets/RafaelMatos/ERW-Grass Land/Tilesets/grass1 to transp.png");
            Sprite grass = At(grassSprites, 64, 64);
            string[] decorationPaths =
            {
                Props + "tree - color scheme 1 - 1.png",
                Props + "tree - color scheme 1 - 2.png",
                Props + "rocks - color scheme 1 - 1.png",
                Props + "rocks - color scheme 1 - 2.png",
                Props + "bush 1.png",
                Props + "crate 1.png",
                Props + "barrel 1.png",
                Grassland + "Props/Static props/sheet2-sprites/standard - flag.png",
                Grassland + "Props/Static props/items-flowers-mushrooms-sprites/items-flowers1_0.png"
            };
            Sprite[] decorations = decorationPaths.Select(path => LoadSprites(path).FirstOrDefault()).ToArray();
            var roadSprites = new Sprite[3, 3];
            for (int column = 0; column < 3; column++)
            {
                for (int row = 0; row < 3; row++)
                {
                    roadSprites[column, row] = At(terrainSprites, column * 32, 256 - row * 32);
                }
            }

            if (route == null
                || grass == null
                || decorations.Any(sprite => sprite == null)
                || roadSprites.Cast<Sprite>().Any(sprite => sprite == null))
            {
                SWLog.LogWarning("[StageOneSceneBuilder] 장면 생성 실패: 필수 경로 또는 스프라이트가 없습니다.");
                return "실패: 필수 경로 또는 스프라이트가 없습니다. 기존 장면을 유지합니다.";
            }

            EnsureFolder(Data);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var ground = new GameObject("InitialTerrain");
            var scenery = new GameObject("InitialScenery");
            var obstacleAreas = new List<Rect>();
            var road = new HashSet<Vector2Int>();
            for (int horizontal = -17; horizontal <= 16; horizontal++)
            {
                for (int vertical = -10; vertical <= 9; vertical++)
                {
                    if (DistanceToRoute(new Vector2(horizontal + 0.5f, vertical + 0.5f)) < 1.35f)
                    {
                        road.Add(new Vector2Int(horizontal, vertical));
                    }
                }
            }

            for (int horizontal = -17; horizontal <= 16; horizontal++)
            {
                for (int vertical = -10; vertical <= 9; vertical++)
                {
                    Vector2Int cell = new Vector2Int(horizontal, vertical);
                    Sprite sprite = grass;
                    if (road.Contains(cell))
                    {
                        int column = !road.Contains(cell + Vector2Int.left) ? 0 : !road.Contains(cell + Vector2Int.right) ? 2 : 1;
                        int row = !road.Contains(cell + Vector2Int.up) ? 0 : !road.Contains(cell + Vector2Int.down) ? 2 : 1;
                        sprite = roadSprites[column, row];
                    }

                    SpriteObject(
                        "TerrainTile " + horizontal + "," + vertical,
                        sprite,
                        new Vector2(horizontal + 0.5f, vertical + 0.5f),
                        -2000,
                        ground.transform);
                }
            }

            Sprite tree = decorations[0];
            Sprite secondTree = decorations[1];
            for (int index = 0; index < 11; index++)
            {
                AddProp(
                    "ForestBorder",
                    index % 2 == 0 ? tree : secondTree,
                    new Vector2(-17f + index * 3.3f, 6.4f + (index % 2) * 0.5f),
                    new Vector2(1.1f, 0.65f));
            }

            AddProp("WesternForest", secondTree, new Vector2(-14f, -2.7f), new Vector2(1.2f, 0.8f));
            AddProp("WesternForest", tree, new Vector2(-16.8f, -4.2f), new Vector2(1.2f, 0.8f));
            AddProp("CentralTree", secondTree, new Vector2(0.8f, -2.8f), new Vector2(1.1f, 0.7f));
            AddProp("WesternRock", decorations[2], new Vector2(-6f, 1.7f), new Vector2(1.0f, 0.65f));
            AddProp("CampRock", decorations[3], new Vector2(8.2f, 3.4f), new Vector2(0.85f, 0.6f));
            Sprite bush = decorations[4];
            foreach (Vector2 position in new[]
            {
                new Vector2(-12, 5.8f),
                new Vector2(-6.5f, -4.3f),
                new Vector2(2.8f, 5.7f),
                new Vector2(7.7f, -3.5f)
            })
            {
                AddProp("Bush", bush, position, new Vector2(0.5f, 0.35f));
            }

            AddProp("SupplyCrate", decorations[5], new Vector2(13.2f, -5.5f), new Vector2(0.5f, 0.4f));
            AddProp("SupplyBarrel", decorations[6], new Vector2(14.4f, -4.8f), new Vector2(0.4f, 0.4f));
            AddProp("RallyFlag", decorations[7], new Vector2(10.3f, -4.8f), new Vector2(0.2f, 0.2f));
            var random = new System.Random(1701);
            Sprite flower = decorations[8];
            for (int index = 0; index < 90; index++)
            {
                Vector2 position = new Vector2((float)random.NextDouble() * 32f - 16f, (float)random.NextDouble() * 13f - 6f);
                if (DistanceToRoute(position) < 1.8f || obstacleAreas.Any(area => area.Contains(position)))
                {
                    continue;
                }

                SpriteObject("MeadowFlower", flower, position, -1900, scenery.transform);
            }

            var navigation = new GameObject("WalkableBattlefield").AddComponent<WalkableBattlefield>();
            navigation.Configure(new Rect(-16, -6.2f, 32, 13.5f), 0.5f, obstacleAreas.ToArray());
            var routeAsset = ScriptableObject.CreateInstance<EnemyRouteData>();
            var serializedRoute = new SerializedObject(routeAsset);
            SerializedProperty routeProperty = serializedRoute.FindProperty("points");
            routeProperty.arraySize = RoutePoints.Length;
            for (int index = 0; index < RoutePoints.Length; index++)
            {
                routeProperty.GetArrayElementAtIndex(index).vector2Value = RoutePoints[index];
            }

            serializedRoute.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(routeAsset, ProjectAssetPaths.EnemyRoute);
            new GameObject("EnemyEntrance").transform.position = RoutePoints[0];
            new GameObject("WorkshopAttackPoint").transform.position = RoutePoints[RoutePoints.Length - 1];
            var camera = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 9;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(22, 35, 31, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = new Vector3(0, 1, 0);
            var additional = camera.GetUniversalAdditionalCameraData();
            additional.renderPostProcessing = false;
            additional.antialiasing = AntialiasingMode.None;
            StageOneBattlefieldLayout.ConfigureCamera(camera);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return ScenePath + " — 지형 " + ground.transform.childCount + "개, 장애물 " + obstacleAreas.Count + "개";
            void AddProp(string name, Sprite sprite, Vector2 feet, Vector2 footprint)
            {
                // 원본 스프라이트의 중심 피벗과 발 위치를 맞춥니다.
                Vector2 offset = new Vector2(0f, sprite.bounds.size.y * 0.5f - 0.15f);
                SpriteObject(name, sprite, feet + offset, Mathf.RoundToInt(-feet.y * 10f), scenery.transform);
                obstacleAreas.Add(new Rect(feet - footprint, footprint * 2f));
            }
        }

        /// <summary>
        /// 원본 임포트 설정을 변경하지 않고 기존 스프라이트 분할을 읽습니다.
        /// </summary>
        public static Sprite[] LoadSprites(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                SWLog.LogWarning("[StageOneSceneBuilder] 스프라이트 조회 실패: 이미지를 찾을 수 없습니다. " + path);
                return Array.Empty<Sprite>();
            }

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(sprite => sprite.rect.y).ThenBy(sprite => sprite.rect.x).ToArray();
        }

        /// <summary>
        /// 원본 이미지 좌표에 해당하는 분할 스프라이트를 찾습니다.
        /// </summary>
        private static Sprite At(Sprite[] sprites, int horizontal, int vertical)
        {
            Sprite result = sprites.FirstOrDefault(sprite => Mathf.RoundToInt(sprite.rect.x) == horizontal && Mathf.RoundToInt(sprite.rect.y) == vertical);
            if (result == null)
            {
                SWLog.LogWarning($"[StageOneSceneBuilder] 스프라이트 조회 실패: 좌표 ({horizontal}, {vertical})를 찾을 수 없습니다.");
            }

            return result;
        }

        /// <summary>
        /// 장식용 스프라이트 객체에 위치·정렬 순서·부모를 설정합니다.
        /// </summary>
        private static GameObject SpriteObject(
            string name,
            Sprite sprite,
            Vector2 position,
            int order,
            Transform parent)
        {
            var instance = new GameObject(name, typeof(SpriteRenderer));
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            var renderer = instance.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return instance;
        }

        /// <summary>
        /// 위치에서 고정 경로의 각 구간까지 최단 거리를 계산합니다.
        /// </summary>
        private static float DistanceToRoute(Vector2 point)
        {
            float closest = float.PositiveInfinity;
            for (int index = 1; index < RoutePoints.Length; index++)
            {
                Vector2 start = RoutePoints[index - 1];
                Vector2 direction = RoutePoints[index] - start;
                float ratio = Mathf.Clamp01(Vector2.Dot(point - start, direction) / direction.sqrMagnitude);
                closest = Mathf.Min(closest, Vector2.Distance(point, start + direction * ratio));
            }

            return closest;
        }

        /// <summary>
        /// 편집기 자산 데이터베이스로 필요한 폴더만 생성합니다.
        /// </summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        #endregion // 함수
    }
}
