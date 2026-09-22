using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Base;

namespace ProjectT.Initialization
{
    /// <summary>
    /// Main 장면의 공통 관리자가 준비된 뒤 스테이지로 진입합니다.
    /// 편집기에서는 Play 직전에 열려 있던 장면으로 돌아가므로 어느 장면에서 Play해도 Main을 거쳐 시작합니다.
    /// </summary>
    public sealed class MainSceneEntry : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private string firstStageScene = "Stage01_Grassland";

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 공통 관리자 초기화 이후 편집기 복귀 장면 또는 첫 스테이지를 불러옵니다.
        /// </summary>
        private void Start()
        {
#if UNITY_EDITOR
            string returnScene = UnityEditor.SessionState.GetString(ProjectDefine.Scene.EditorReturnSceneKey, string.Empty);
            if (!string.IsNullOrEmpty(returnScene) && returnScene != gameObject.scene.path)
            {
                var parameters = new LoadSceneParameters(LoadSceneMode.Single);
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(returnScene, parameters);
                return;
            }
#endif
            SceneManager.LoadSceneAsync(firstStageScene);
        }

        #endregion // 초기화
    }
}
