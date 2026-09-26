using UnityEngine.SceneManagement;

using SW.Base;

namespace ProjectT.Initialization
{
    /// <summary>
    /// Bootstrap 장면의 공통 관리자가 준비된 뒤 타이틀로 진입합니다. Bootstrap은 화면 없이 관리자 준비만 담당합니다.
    /// 편집기에서는 Play 직전에 열려 있던 장면으로 돌아가므로 어느 장면에서 Play해도 Bootstrap을 거쳐 시작합니다.
    /// </summary>
    public sealed class BootstrapEntry : SWMonoBehaviour
    {
        #region 초기화
        /// <summary>
        /// 공통 관리자 초기화 이후 편집기 복귀 장면 또는 타이틀을 불러옵니다.
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
            SceneManager.LoadSceneAsync(ProjectDefine.Scene.TitlePath);
        }

        #endregion // 초기화
    }
}
