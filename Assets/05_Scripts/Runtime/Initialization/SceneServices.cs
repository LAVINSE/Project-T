using UnityEngine;

using SW.Base;
using SW.Pooling;
using SW.Popup;

using ProjectT.Data;

namespace ProjectT.Initialization
{
    /// <summary>
    /// Main 또는 개별 장면 직접 실행에서 공통 관리자 프리팹을 한 번만 준비합니다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SceneServices : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private DataManager dataManagerPrefab;
        [SerializeField] private SWPool poolPrefab;
        [SerializeField] private SWPopupManager popupManagerPrefab;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 이미 존재하는 관리자를 보존하면서 누락된 공통 관리자를 생성합니다.
        /// </summary>
        private void Awake()
        {
            if (!DataManager.HasInstance && FindAnyObjectByType<DataManager>() == null)
            {
                Instantiate(dataManagerPrefab).name = "DataManager";
            }

            if (!SWPool.HasInstance && FindAnyObjectByType<SWPool>() == null)
            {
                Instantiate(poolPrefab).name = "SWPool";
            }

            if (!SWPopupManager.HasInstance && FindAnyObjectByType<SWPopupManager>() == null)
            {
                Instantiate(popupManagerPrefab).name = "SWPopupManager";
            }
        }

        #endregion // 초기화
    }
}
