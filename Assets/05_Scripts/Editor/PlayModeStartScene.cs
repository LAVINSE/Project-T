using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ProjectT.Editor
{
    /// <summary>
    /// 편집기 Play를 항상 Main 장면에서 시작하게 합니다. Play 직전에 열려 있던 장면을 기록해 MainSceneEntry가 그 장면으로 이동합니다.
    /// 빌드와 같은 초기화 순서를 보장하므로 관리자를 Resources나 실행 순서 지정 없이 Main에 배치할 수 있습니다.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        #region 초기화
        /// <summary>
        /// 편집기가 로드될 때 시작 장면과 Play 상태 알림을 연결합니다.
        /// </summary>
        static PlayModeStartScene()
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ProjectDefine.Scene.MainPath);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// Play에 들어가기 직전에 현재 활성 장면 경로를 기록합니다.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SessionState.SetString(ProjectDefine.Scene.EditorReturnSceneKey, SceneManager.GetActiveScene().path);
            }
        }

        #endregion // 함수
    }
}
