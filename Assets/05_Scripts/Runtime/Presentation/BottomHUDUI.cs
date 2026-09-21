using System;
using UnityEngine;

using TMPro;

using SW.Base;
using SW.Util;

using ProjectT.Data;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 인벤토리·배속·정지 입력을 전달하고 현재 조작 상태를 표시합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT", "ProjectT.Runtime", "BottomHUDUI")]
    public sealed class BottomHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private UnityEngine.UI.Button inventoryButton;
        [SerializeField] private UnityEngine.UI.Button speedButton;
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private UnityEngine.UI.Image speedIcon;
        [SerializeField] private TMP_Text resumeLabel;
        [SerializeField] private UnityEngine.UI.Image pauseIcon;
        private SpriteData spriteData;
        private bool initialized;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 조작 연결에 필요한 필수 참조입니다.
        /// </summary>
        public bool HasRequiredReferences => inventoryButton != null
            && speedButton != null
            && pauseButton != null
            && speedIcon != null
            && resumeLabel != null
            && pauseIcon != null;

        /// <summary>
        /// 인벤토리 열기·닫기 요청입니다.
        /// </summary>
        public event Action InventoryRequested;

        /// <summary>
        /// 정지·재개 요청입니다.
        /// </summary>
        public event Action PauseRequested;

        /// <summary>
        /// 다음 배속 요청입니다.
        /// </summary>
        public event Action SpeedRequested;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 공통 아이콘과 버튼 이벤트를 한 번 연결합니다. 참조가 누락되면 기존 상태를 보존하고 실패합니다.
        /// </summary>
        public bool Initialize(SpriteData sharedSprites)
        {
            if (!HasRequiredReferences || sharedSprites == null || !sharedSprites.HasSpeedIcons)
            {
                SWLog.LogWarning("[BottomHUDUI] 초기화 실패: 버튼·이미지와 SpriteData의 배속 아이콘을 확인해 주세요.");
                return false;
            }

            if (initialized)
            {
                return true;
            }

            spriteData = sharedSprites;
            inventoryButton.onClick.AddListener(RequestInventory);
            pauseButton.onClick.AddListener(RequestPause);
            speedButton.onClick.AddListener(RequestSpeed);
            initialized = true;
            return true;
        }

        /// <summary>
        /// 선택 배속과 수동 정지를 표시합니다. 결과 확정 후 시간 조작을 막습니다.
        /// </summary>
        public void Present(bool manuallyPaused, int speedMultiplier, bool canControlTime)
        {
            if (!initialized)
            {
                return;
            }

            Sprite currentIcon = spriteData.GetSpeedIcon(speedMultiplier);
            if (currentIcon != null)
            {
                speedIcon.sprite = currentIcon;
            }

            resumeLabel.gameObject.SetActive(manuallyPaused);
            pauseIcon.enabled = !manuallyPaused;
            pauseButton.interactable = canControlTime;
            speedButton.interactable = canControlTime;
        }

        /// <summary>
        /// 인벤토리 요청을 화면 관리자에게 전달합니다.
        /// </summary>
        private void RequestInventory()
        {
            InventoryRequested?.Invoke();
        }

        /// <summary>
        /// 정지 요청을 화면 관리자에게 전달합니다.
        /// </summary>
        private void RequestPause()
        {
            PauseRequested?.Invoke();
        }

        /// <summary>
        /// 배속 요청을 화면 관리자에게 전달합니다.
        /// </summary>
        private void RequestSpeed()
        {
            SpeedRequested?.Invoke();
        }

        /// <summary>
        /// 버튼 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            inventoryButton.onClick.RemoveListener(RequestInventory);
            pauseButton.onClick.RemoveListener(RequestPause);
            speedButton.onClick.RemoveListener(RequestSpeed);
        }

        #endregion // 초기화
    }
}
