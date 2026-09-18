using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Base;

namespace ProjectT.Initialization
{
    /// <summary>
    /// Main의 공통 초기화 이후 첫 전투 장면으로 진입합니다.
    /// </summary>
    public sealed class MainSceneEntry : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private string firstStageScene = "Stage01_Grassland";

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 공통 관리자 초기화 이후 첫 스테이지를 불러옵니다.
        /// </summary>
        private void Start()
        {
            SceneManager.LoadSceneAsync(firstStageScene);
        }

        #endregion // 초기화
    }
}
