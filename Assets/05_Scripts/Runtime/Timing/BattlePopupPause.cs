using System;
using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectT.Timing
{
    /// <summary>
    /// 팝업의 활성 수명 동안 전투를 정지합니다. SWPopup의 숨김 연출이 끝나 비활성화될 때 해제됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Timing", "ProjectT.Defense.Runtime", "BattlePopupPause")]
    public sealed class BattlePopupPause : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("전투 정지 연결")]
        [SerializeField] private BattlePauseController controller;
        private IDisposable pauseLease;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 팝업이 사용할 전투 정지 관리자를 연결합니다. 활성 상태에서 재연결해도 이전 소유권을 정리합니다.
        /// </summary>
        public void Configure(BattlePauseController value)
        {
            if (controller == value && pauseLease != null)
            {
                return;
            }

            ReleasePause();
            controller = value;
            AcquirePause();
        }

        /// <summary>
        /// 팝업이 열릴 때 전투 정지를 요청합니다.
        /// </summary>
        private void OnEnable()
        {
            AcquirePause();
        }

        /// <summary>
        /// 팝업이 닫힐 때 이 팝업의 정지 요청을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            ReleasePause();
        }

        /// <summary>
        /// 팝업이 제거될 때 남아 있는 정지 요청을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            ReleasePause();
        }

        /// <summary>
        /// 전투 정지 소유권을 중복 없이 얻습니다.
        /// </summary>
        private void AcquirePause()
        {
            if (pauseLease == null && isActiveAndEnabled && controller != null && controller.isActiveAndEnabled)
            {
                pauseLease = controller.Pause();
            }
        }

        /// <summary>
        /// 보유한 정지 소유권을 반환하고 참조를 비웁니다.
        /// </summary>
        private void ReleasePause()
        {
            pauseLease?.Dispose();
            pauseLease = null;
        }

        #endregion // 함수
    }
}
