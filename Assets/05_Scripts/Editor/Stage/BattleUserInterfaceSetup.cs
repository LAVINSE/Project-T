using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using SW.Util;

using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 각 스테이지에 같은 전투 화면 프리팹을 연결합니다. 기존 화면이 있으면 보존합니다.
    /// </summary>
    public static class BattleUserInterfaceSetup
    {
        #region 필드
        public const string PrefabPath = "Assets/04_Prefabs/UserInterface/BattleScreen.prefab";

        #endregion // 필드

        #region 생성
        /// <summary>
        /// 현재 장면에 공통 화면과 입력 시스템을 준비합니다. 프리팹이 없으면 변경 없이 실패합니다.
        /// </summary>
        public static bool CreateForStage()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || prefab == null || !scene.IsValid())
            {
                SWLog.LogWarning("[BattleUserInterfaceSetup] 생성 실패: 편집 중인 장면과 공통 화면 프리팹을 확인해 주세요.");
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Any(root => root.GetComponentInChildren<BattleScreen>(true) != null))
            {
                return true;
            }

            GameObject parent = roots.FirstOrDefault(root => root.name == "UIs");
            if (parent == null)
            {
                parent = new GameObject("UIs");
                SceneManager.MoveGameObjectToScene(parent, scene);
            }

            Transform canvasTransform = parent.transform.Find("Canvas");
            if (canvasTransform != null && canvasTransform.GetComponent<Canvas>() == null)
            {
                SWLog.LogWarning("[BattleUserInterfaceSetup] 생성 실패: 기존 Canvas 객체에 Canvas 컴포넌트가 없습니다.");
                return false;
            }

            if (canvasTransform == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasTransform = canvasObject.transform;
                canvasTransform.SetParent(parent.transform, false);
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                Undo.RegisterCreatedObjectUndo(canvasObject, "장면 Canvas 추가");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasTransform);
            Undo.RegisterCreatedObjectUndo(instance, "공통 전투 화면 추가");
            if (!roots.Any(root => root.GetComponentInChildren<EventSystem>(true) != null))
            {
                var input = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                input.transform.SetParent(parent.transform, false);
                input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                Undo.RegisterCreatedObjectUndo(input, "화면 입력 시스템 추가");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        #endregion // 생성
    }
}
