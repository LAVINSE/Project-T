using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

using SW.Util;

using ProjectT.Navigation;
using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 스테이지 1의 확장 전장과 공격·구매 표시를 기존 전투 데이터에 적용합니다.
    /// </summary>
    public static class StageOnePresentationRevision
    {
        #region 함수
        /// <summary>
        /// Unity CLI에서 편집 중인 첫 스테이지에 확정된 표시 개선을 적용하고 저장합니다.
        /// </summary>
        public static string Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != StageOneSceneBuilder.ScenePath || scene.isDirty)
            {
                SWLog.LogWarning("[StageOnePresentationRevision] 작업 중단: " + "저장된 스테이지 1을 편집 모드로 열어 주세요.");
                return "실패: " + "저장된 스테이지 1을 편집 모드로 열어 주세요.";
            }

            string warrior = "Assets/RafaelMatos/ERW-Grass Land/Characters/warrior/warrior-";
            var warriorAppearance = StageOneGameplayBuilder.Appearance(
                "WarriorAppearance",
                warrior + "idle.png",
                warrior + "run.png",
                warrior + "single swing 1.png",
                warrior + "death.png");
            string mage = "Assets/RafaelMatos/ERW-Grassland 2.0/Characters/orc mage/orc1/orc mage - with hand fx-";
            var mageAppearance = StageOneGameplayBuilder.Appearance("MageAppearance", mage + "idle.png", mage + "walk.png", mage + "atk1.png", mage + "death.png");
            string skeleton = "Assets/RafaelMatos/ERW-Crypt/Characters/Skeleton/skeleton-variation1-";
            var skeletonAppearance = StageOneGameplayBuilder.Appearance(
                "SkeletonAppearance",
                skeleton + "idle.png",
                skeleton + "walk.png",
                skeleton + "attack.png",
                skeleton + "death.png");
            if (warriorAppearance == null || mageAppearance == null || skeletonAppearance == null)
            {
                return "실패: 유닛 외형의 필수 스프라이트를 확인해 주세요.";
            }

            foreach (string name in new[]
            {
                "AllyUnit",
                "EnemyUnit"
            })
            {
                string path = "Assets/04_Prefabs/Units/" + name + ".prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Transform child = contents.transform.Find("NewUnitArrow");
                    var arrow = child == null
                        ? new GameObject("NewUnitArrow", typeof(LineRenderer)).GetComponent<LineRenderer>()
                        : child.GetComponent<LineRenderer>();
                    arrow.transform.SetParent(contents.transform, false);
                    arrow.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ProjectAssetPaths.BattleLine);
                    arrow.positionCount = 5;
                    arrow.useWorldSpace = true;
                    arrow.startColor = arrow.endColor = new Color(1f, 0.86f, 0.4f);
                    arrow.widthMultiplier = 0.1f;
                    arrow.sortingOrder = 2002;
                    arrow.enabled = false;
                    StageOneGameplayBuilder.Set(contents.GetComponent<UnitPresentation>(), "newUnitArrow", arrow);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            StageOneBattlefieldLayout.Apply();
            PlayerSettings.runInBackground = true;
            string terrainResult = StageOneTerrainBuilder.Improve();
            if (terrainResult.StartsWith("실패:"))
            {
                return terrainResult;
            }

            StageOneCompactScreen.Apply();
            ArcaneWorkshopBuilder.Apply();
            string interactionResult = StageOneInteractionBuilder.Apply();
            if (interactionResult.StartsWith("실패:"))
            {
                return interactionResult;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "1920×1080 기준 전장과 확대된 표시 비율, 마법 공방과 명령 화면 적용 완료";
        }

        #endregion // 함수
    }
}
