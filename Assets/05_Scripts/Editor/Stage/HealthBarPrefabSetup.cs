using UnityEditor;
using UnityEngine;

using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 기존 체력바 그림과 크기를 유지하면서 런타임 체력 표시를 연결합니다.
    /// </summary>
    public static class HealthBarPrefabSetup
    {
        #region 함수
        /// <summary>
        /// 공통·공방 체력바 프리팹의 표시 컴포넌트를 설정합니다.
        /// </summary>
        public static void ConfigurePrefabs()
        {
            Configure(ProjectAssetPaths.CommonHealthBar, false);
            Configure(ProjectAssetPaths.ArcaneHealthBar, true);
        }

        /// <summary>
        /// 지정 체력바의 배경·채움 렌더러와 채움 방향을 저장합니다.
        /// </summary>
        private static void Configure(string path, bool vertical)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var presentation = root.GetComponent<HealthBarPresentation>() ?? root.AddComponent<HealthBarPresentation>();
                var background = root.transform.Find("HpBackground_Sprite").GetComponent<SpriteRenderer>();
                var fill = root.transform.Find("HpFill_Sprite").GetComponent<SpriteRenderer>();
                background.sortingOrder = 2000;
                fill.sortingOrder = 2001;
                StageOneGameplayBuilder.Set(presentation, "background", background, "fill", fill, "vertical", vertical);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 유닛 아래에 공통 체력바를 연결하며 기존 선 체력바만 제거합니다.
        /// </summary>
        public static HealthBarPresentation ConfigureUnit(GameObject unit)
        {
            foreach (string name in new[]
            {
                "HealthBackground",
                "HealthFill"
            })
            {
                Transform previous = unit.transform.Find(name);
                if (previous != null)
                {
                    Object.DestroyImmediate(previous.gameObject);
                }
            }

            Transform existing = unit.transform.Find("CommonHealthBar");
            GameObject bar = existing != null
                ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.CommonHealthBar), unit.transform);
            bar.name = "CommonHealthBar";
            bar.transform.localScale = new Vector3(0.2f, 0.5f, 1f);
            var presentation = bar.GetComponent<HealthBarPresentation>();
            StageOneGameplayBuilder.Set(unit.GetComponent<UnitPresentation>(), "healthBar", presentation);
            return presentation;
        }

        #endregion // 함수
    }
}
