using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using SW.Base;
using SW.Util;

namespace ProjectT.UI
{
    /// <summary>
    /// 타이틀 화면의 게임 시작·종료 요청을 처리합니다. 저장은 하나뿐이므로 게임 시작은 항상 같은 진행을 이어갑니다.
    /// </summary>
    public sealed class TitleUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 버튼 알림을 연결합니다.
        /// </summary>
        private void Start()
        {
            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(QuitGame);
        }

        #endregion // 초기화

        #region 입력
        /// <summary>
        /// 거점으로 이동합니다. 전환 중 반복 입력을 막습니다.
        /// </summary>
        private void StartGame()
        {
            startButton.interactable = false;
            quitButton.interactable = false;
            SceneManager.LoadSceneAsync(ProjectDefine.Scene.HubPath);
        }

        /// <summary>
        /// 게임을 종료합니다. 편집기에서는 Play를 멈추지 않고 안내만 남깁니다.
        /// </summary>
        private void QuitGame()
        {
#if UNITY_EDITOR
            SWLog.Log("[TitleUI] 게임 종료 요청: 편집기에서는 Play를 유지합니다.");
#else
            Application.Quit();
#endif
        }

        #endregion // 입력

        #region 정리
        /// <summary>
        /// 버튼 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(StartGame);
            quitButton.onClick.RemoveListener(QuitGame);
        }

        #endregion // 정리
    }
}
