using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 인벤토리·배속·정지 입력을 전달하고 현재 조작 상태를 표시합니다.
    /// </summary>
    public sealed class BottomHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image speedIcon;
        [SerializeField] private TMP_Text resumeLabel;
        [SerializeField] private Image pauseIcon;
        private SpriteData spriteData;

        #endregion // 필드

        #region 프로퍼티
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
        /// 배속 아이콘을 가져올 공통 아이콘 데이터를 연결합니다. 전투 화면이 호출합니다.
        /// </summary>
        public void Initialize(SpriteData sprites)
        {
            spriteData = sprites;
        }

        /// <summary>
        /// 버튼 클릭을 요청 알림에 연결합니다.
        /// </summary>
        private void Awake()
        {
            inventoryButton.onClick.AddListener(RequestInventory);
            pauseButton.onClick.AddListener(RequestPause);
            speedButton.onClick.AddListener(RequestSpeed);
        }

        /// <summary>
        /// 버튼 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            inventoryButton.onClick.RemoveListener(RequestInventory);
            pauseButton.onClick.RemoveListener(RequestPause);
            speedButton.onClick.RemoveListener(RequestSpeed);
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 선택 배속과 수동 정지를 표시합니다. 결과 확정 후에는 시간 조작을 막습니다.
        /// </summary>
        public void Present(bool manuallyPaused, int speedMultiplier, bool canControlTime)
        {
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

        #endregion // 표시

        #region 요청
        /// <summary>
        /// 인벤토리 요청을 전달합니다.
        /// </summary>
        private void RequestInventory()
        {
            InventoryRequested?.Invoke();
        }

        /// <summary>
        /// 정지 요청을 전달합니다.
        /// </summary>
        private void RequestPause()
        {
            PauseRequested?.Invoke();
        }

        /// <summary>
        /// 배속 요청을 전달합니다.
        /// </summary>
        private void RequestSpeed()
        {
            SpeedRequested?.Invoke();
        }

        #endregion // 요청
    }
}
