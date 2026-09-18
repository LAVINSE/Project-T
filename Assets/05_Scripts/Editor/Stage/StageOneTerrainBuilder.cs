using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 초원 데모의 룰타일과 장식 구성을 사용해 스테이지 1 지형을 작성합니다. 원본 자산은 변경하지 않습니다.
    /// </summary>
    public static class StageOneTerrainBuilder
    {
        #region 필드
        private const string Collection = "Assets/RafaelMatos/ERW-Grassland 2.0/";
        private const string Rules = Collection + "Tilesets/Rule Tile/";
        private const string Props = Collection + "Props/Static props/sheet1-sprites/";
        private const string RootName = "TilemapGrid";
        private const float DisplayScale = 100f / 32f;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 현재 스테이지의 에이전트 생성 지형만 교체하고 수정 결과를 저장합니다.
        /// </summary>
        public static string Improve()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != StageOneSceneBuilder.ScenePath)
            {
                SWLog.LogWarning("[StageOneTerrainBuilder] 작업 중단: " + "스테이지 1을 편집 모드로 열어 주세요.");
                return "실패: " + "스테이지 1을 편집 모드로 열어 주세요.";
            }

            var definition = AssetDatabase.LoadAssetAtPath<EnemyRouteDefinition>("Assets/02_Res/Data/Navigation/Stage01EnemyRoute.asset");
            if (definition == null)
            {
                SWLog.LogWarning("[StageOneTerrainBuilder] 작업 중단: 경로 데이터가 없습니다.");
                return "실패: 경로 데이터가 없습니다.";
            }

            if (!definition.TryCreateRoute(out FixedRoute route, out string reason))
            {
                SWLog.LogWarning("[StageOneTerrainBuilder] 작업 중단: " + reason);
                return "실패: " + reason;
            }

            TileBase baseGrass = Tile("full tile base grass.asset");
            TileBase shadedGrass = Tile("grass extra shade to grass.asset");
            TileBase dirt = Tile("dirt1 to grass - transparency.asset");
            TileBase gravelTile = Tile("gravel to grass - transparency.asset");
            TileBase water = Tile("anim rule tiles/water to grass(transparency) - river orientation-spritesheet.asset");
            TileBase longGrass = Tile("full tile tall grass darker.asset");
            TileBase fenceTile = Tile("fence-straight.asset");
            if (new[]
            {
                baseGrass,
                shadedGrass,
                dirt,
                gravelTile,
                water,
                longGrass,
                fenceTile
            }.Any(value => value == null))
            {
                return "실패: 필요한 룰타일이 없어 기존 지형을 유지합니다.";
            }

            var oldRoots = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(value => value.name == "InitialTerrain"
                || value.name == "InitialScenery"
                || value.name == RootName
                || value.name == "EnvironmentScenery").Select(value => value.gameObject).ToArray();
            var rootObject = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "초원 룰타일 개선");
            var gridObject = new GameObject("Grid", typeof(Grid));
            gridObject.transform.SetParent(rootObject.transform);
            gridObject.transform.localScale = Vector3.one * DisplayScale;
            gridObject.GetComponent<Grid>().cellSize = new Vector3(0.32f, 0.32f, 0);
            var scenery = new GameObject("EnvironmentScenery");
            scenery.transform.SetParent(rootObject.transform);
            var blocked = new List<Rect>();
            Tilemap grass = Layer("BaseGrass", -2000);
            Tilemap shade = Layer("ShadedGrass", -1990);
            Tilemap path = Layer("EnemyDirtPath", -1980);
            Tilemap gravel = Layer("OutpostGravel", -1970);
            Tilemap pond = Layer("WesternPond", -1960);
            Tilemap tallGrass = Layer("ForestTallGrass", -1950);
            Tilemap fence = Layer("OutpostFence", -1940);
            for (int horizontal = -26; horizontal < 26; horizontal++)
            {
                for (int vertical = -16; vertical < 16; vertical++)
                {
                    var cell = new Vector3Int(horizontal, vertical, 0);
                    Vector2 center = new Vector2(horizontal + 0.5f, vertical + 0.5f);
                    float distance = DistanceToRoute(center, route);
                    grass.SetTile(cell, baseGrass);
                    float noise = Mathf.PerlinNoise(horizontal * 0.23f + 11f, vertical * 0.23f + 14f);
                    bool forest = vertical > 8
                        || vertical < -9
                        || horizontal < -22
                        || Ellipse(center, new Vector2(-11, -0.5f), new Vector2(3.2f, 3.4f))
                        || Ellipse(center, new Vector2(8.5f, 1.5f), new Vector2(3, 3.4f));
                    bool shaded = forest || (noise > 0.75f && distance > 4.5f);
                    if (shaded)
                    {
                        shade.SetTile(cell, shadedGrass);
                    }

                    if (distance < 1.5f)
                    {
                        path.SetTile(cell, dirt);
                    }

                    bool workshopGround = Ellipse(center, new Vector2(18, 5.5f), new Vector2(5, 4));
                    if (workshopGround || Ellipse(center, new Vector2(19, -1), new Vector2(4, 2.5f)))
                    {
                        gravel.SetTile(cell, gravelTile);
                    }

                    bool isWater = Ellipse(center, new Vector2(-21, -2), new Vector2(2.8f, 3.5f))
                        || Ellipse(center, new Vector2(-23, 1), new Vector2(1.6f, 1.6f));
                    if (isWater && distance > 2.2f)
                    {
                        pond.SetTile(cell, water);
                        blocked.Add(new Rect(horizontal - 0.12f, vertical - 0.12f, 1.24f, 1.24f));
                    }

                    if (forest && !isWater && !workshopGround && distance > 4f && noise > 0.69f)
                    {
                        tallGrass.SetTile(cell, longGrass);
                    }

                    if (vertical == 9 && horizontal >= 15 && horizontal <= 22)
                    {
                        fence.SetTile(cell, fenceTile);
                        blocked.Add(new Rect(horizontal, vertical + 0.2f, 1, 0.5f));
                    }
                }
            }

            foreach (Tilemap tilemap in gridObject.GetComponentsInChildren<Tilemap>())
            {
                tilemap.RefreshAllTiles();
            }

            // 큰 빈 잔디판 대신 숲 군집 사이에 세 전투 구간과 이동 공간을 만듭니다.
            for (int index = 0; index < 21; index++)
            {
                float horizontal = -25 + index * 2.1f;
                AddProp(
                    "NorthernForest",
                    Props + "tree - color scheme 1 - " + (index % 2 + 1) + ".png",
                    new Vector2(horizontal, 9.2f + (index % 3) * 0.5f),
                    new Vector2(0.65f, 0.4f),
                    0.78f);
                AddProp(
                    "SouthernForest",
                    Props + "tree - color scheme 1 - " + (index % 2 + 1) + ".png",
                    new Vector2(horizontal, -13f + (index % 3) * 0.5f),
                    new Vector2(0.65f, 0.4f),
                    0.78f);
            }

            Vector2[] groveCenters =
            {
                new Vector2(-11, -0.5f),
                new Vector2(8.5f, 1.5f),
                new Vector2(-23, -8)
            };
            Vector2[] groveOffsets =
            {
                new Vector2(-1.5f, 0.8f),
                new Vector2(0.4f, 1.7f),
                new Vector2(1.7f, 0.4f),
                new Vector2(-0.4f, -1)
            };
            foreach (Vector2 center in groveCenters)
            {
                foreach (Vector2 offset in groveOffsets)
                {
                    AddProp(
                        "ForestGrove",
                        Props + "tree - color scheme 1 - 1.png",
                        center + offset,
                        new Vector2(0.65f, 0.4f),
                        0.78f);
                }
            }

            foreach (Vector2 position in new[]
            {
                new Vector2(-23, 3),
                new Vector2(-25, 0),
                new Vector2(-24, -4),
                new Vector2(22, -7),
                new Vector2(24, -4),
                new Vector2(23, 0)
            })
            {
                AddProp("BoundaryTree", Props + "tree - color scheme 3 - 1.png", position, new Vector2(0.65f, 0.4f), 0.78f);
            }

            AddProp("RoadsideRock", Props + "rocks - color scheme 1 - 1.png", new Vector2(-2.5f, -1), new Vector2(0.9f, 0.6f));
            AddProp("PondRock", Props + "rocks - color scheme 1 - 2.png", new Vector2(-19, -6), new Vector2(0.7f, 0.5f));
            AddProp("EasternRock", Props + "rocks - color scheme 1 - 4.png", new Vector2(10, -7.5f), new Vector2(0.65f, 0.45f));
            foreach (Vector2 position in new[]
            {
                new Vector2(-12, 3.5f),
                new Vector2(-10, -3.5f),
                new Vector2(-1, -1.5f),
                new Vector2(7, 5),
                new Vector2(10.5f, 0),
                new Vector2(17, -7),
                new Vector2(-20, -7),
                new Vector2(-20, 3)
            })
            {
                AddProp("FlowerBush", Props + "bush 1.png", position, new Vector2(0.4f, 0.3f));
            }

            // 공방의 몸체와 바퀴 아래는 아군 통행에서 제외하고 경로 끝의 접근 공간은 비워 둡니다.
            blocked.Add(new Rect(15, 5, 6, 3));
            // 데모에서 함께 구성한 망루와 야영지 소품의 상대 배치를 보존합니다.
            Scene preview = EditorSceneManager.OpenPreviewScene("Assets/RafaelMatos/Scenes/Standard/StandardSampleSceneGL2.unity");
            try
            {
                CopyGroup("watchtower - front", "DefenseWatchtower", new Vector2(9.5f, 4f), new Vector2(1.25f, 0.8f));
                CopyGroup("carriage", "SupplyCarriage", new Vector2(21f, -1f), new Vector2(1.2f, 0.7f));
                CopyGroup("tent2", "AllyCampTent", new Vector2(16.5f, -0.75f), new Vector2(1.1f, 0.65f));
                CopyGroup("campfire", "Campfire", new Vector2(19f, -1.5f), new Vector2(0.45f, 0.3f));
                CopyGroup("sign post", "EntranceSignpost", new Vector2(-20f, 8f), new Vector2(0.25f, 0.2f));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            var terrain = scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<WalkableBattlefield>()).Single();
            terrain.Configure(StageOneBattlefieldLayout.WalkableArea, 0.5f, blocked.ToArray());
            foreach (GameObject oldRoot in oldRoots)
            {
                if (oldRoot != null)
                {
                    Undo.DestroyObjectImmediate(oldRoot);
                }
            }

            if (UnityEngine.Object.FindFirstObjectByType<ProjectT.Battle.BattleSession>() != null)
            {
                ProjectSceneSetup.ConfigureStage();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "룰타일 " + gridObject.GetComponentsInChildren<Tilemap>().Length + "개 레이어와 데모 소품으로 초원 개선 완료";
            Tilemap Layer(string name, int order)
            {
                var instance = new GameObject(name + "_Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
                instance.transform.SetParent(gridObject.transform, false);
                instance.GetComponent<TilemapRenderer>().sortingOrder = order;
                return instance.GetComponent<Tilemap>();
            }

            void AddProp(string name, string assetPath, Vector2 feet, Vector2 footprint, float displaySize = 1f)
            {
                Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().First();
                var instance = new GameObject(name, typeof(SpriteRenderer));
                instance.transform.SetParent(scenery.transform);
                float scale = sprite.pixelsPerUnit / 32f * displaySize;
                instance.transform.localScale = Vector3.one * scale;
                instance.transform.position = feet - new Vector2(0, SpriteVisibleBounds.Read(sprite).min.y * scale);
                var renderer = instance.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = Mathf.RoundToInt(-feet.y * 10f);
                instance.AddComponent<WorldDepthSorting>().Configure(feet);
                blocked.Add(new Rect(feet - footprint, footprint * 2));
            }

            void CopyGroup(string sourceName, string name, Vector2 feet, Vector2 footprint)
            {
                GameObject source = preview.GetRootGameObjects().First(value => value.name == sourceName);
                var group = new GameObject(name);
                group.transform.SetParent(scenery.transform);
                SpriteRenderer[] originals = source.GetComponentsInChildren<SpriteRenderer>(true).Where(value => value.sprite != null).ToArray();
                float bottom = float.PositiveInfinity;
                foreach (SpriteRenderer original in originals)
                {
                    float compensation = original.sprite.pixelsPerUnit / 100f;
                    var instance = new GameObject(EnglishObjectName(original.name), typeof(SpriteRenderer));
                    instance.transform.SetParent(group.transform);
                    instance.transform.localPosition = original.transform.position - source.transform.position;
                    instance.transform.localScale = original.transform.lossyScale * compensation;
                    instance.transform.rotation = original.transform.rotation;
                    var renderer = instance.GetComponent<SpriteRenderer>();
                    renderer.sprite = original.sprite;
                    renderer.color = original.color;
                    renderer.flipX = original.flipX;
                    renderer.flipY = original.flipY;
                    renderer.sortingOrder = Mathf.RoundToInt(-feet.y * 10f) + original.sortingOrder;
                    Bounds visible = SpriteVisibleBounds.Read(renderer.sprite);
                    bottom = Mathf.Min(
                        bottom,
                        renderer.transform.TransformPoint(visible.min).y,
                        renderer.transform.TransformPoint(visible.max).y);
                }

                group.transform.localScale = Vector3.one * DisplayScale;
                group.transform.position = feet - new Vector2(0, bottom * DisplayScale);
                group.AddComponent<WorldDepthSorting>().Configure(feet);
                blocked.Add(new Rect(feet - footprint, footprint * 2));
            }
        }

        /// <summary>
        /// 지형 장식 종류를 하이어라키에서 사용할 이름으로 변환합니다.
        /// </summary>
        private static string EnglishObjectName(string sourceName)
        {
            return string.Concat(System.Text.RegularExpressions.Regex.Matches(sourceName, "[A-Za-z0-9]+").Cast<System.Text.RegularExpressions.Match>().Select(match => char.ToUpperInvariant(match.Value[0]) + match.Value.Substring(1)));
        }

        /// <summary>
        /// 타일 자산을 읽고 누락된 경로는 경고로 알립니다.
        /// </summary>
        private static TileBase Tile(string name)
        {
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(Rules + name);
            if (tile == null)
            {
                SWLog.LogWarning("[StageOneTerrainBuilder] 룰타일 조회 실패: " + name);
            }

            return tile;
        }

        /// <summary>
        /// 타원 영역에 포함되는 타일 좌표를 계산합니다.
        /// </summary>
        private static bool Ellipse(Vector2 point, Vector2 center, Vector2 radius)
        {
            Vector2 offset = point - center;
            return offset.x * offset.x / (radius.x * radius.x) + offset.y * offset.y / (radius.y * radius.y) <= 1f;
        }

        /// <summary>
        /// 위치에서 적 경로까지 가장 가까운 거리를 계산합니다.
        /// </summary>
        private static float DistanceToRoute(Vector2 point, FixedRoute route)
        {
            float distance = float.PositiveInfinity;
            for (int index = 1; index < route.PointCount; index++)
            {
                Vector2 start = route.GetPoint(index - 1);
                Vector2 direction = route.GetPoint(index) - start;
                float ratio = Mathf.Clamp01(Vector2.Dot(point - start, direction) / direction.sqrMagnitude);
                distance = Mathf.Min(distance, Vector2.Distance(point, start + direction * ratio));
            }

            return distance;
        }

        #endregion // 함수
    }
}
