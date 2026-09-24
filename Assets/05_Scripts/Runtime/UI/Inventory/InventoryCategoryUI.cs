using System;
using UnityEngine;
using UnityEngine.UI;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 분류 자산을 연결한 필터 버튼입니다. 분류가 없으면 전체를 표시합니다.
    /// </summary>
    public sealed class InventoryCategoryUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private Button button;
        [SerializeField] private SWCategory category;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectedSprite;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 필터 대상 분류입니다. null은 전체입니다.
        /// </summary>
        public SWCategory Category => category;

        /// <summary>
        /// 선택한 필터를 상위 화면에 알립니다.
        /// </summary>
        public event Action<InventoryCategoryUI> Selected;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 버튼 입력을 연결합니다.
        /// </summary>
        private void Awake()
        {
            button.onClick.AddListener(Select);
        }

        /// <summary>
        /// 버튼 입력 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            button.onClick.RemoveListener(Select);
        }

        #endregion // 초기화

        #region 선택
        /// <summary>
        /// 현재 선택 상태를 기존 필터 이미지로 표시합니다.
        /// </summary>
        public void Present(bool selected)
        {
            backgroundImage.sprite = selected ? selectedSprite : normalSprite;
        }

        /// <summary>
        /// 선택 요청을 전달합니다.
        /// </summary>
        private void Select()
        {
            Selected?.Invoke(this);
        }

        #endregion // 선택
    }
}
