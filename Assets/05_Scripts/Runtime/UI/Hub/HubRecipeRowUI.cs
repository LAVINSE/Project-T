using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 배운 설계도 하나를 목록 줄로 표시하고 선택을 전달합니다.
    /// </summary>
    public sealed class HubRecipeRowUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private Button rowButton;
        [SerializeField] private Image resultImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image selectedImage;
        private ItemData blueprint;
        private Action<ItemData> selected;

        #endregion // 필드

        #region 표시와 입력
        /// <summary>
        /// 결과 장비 아이콘과 설계도 이름, 선택 강조를 표시합니다.
        /// </summary>
        public void Present(ItemData target, bool isSelected, Action<ItemData> onSelected)
        {
            blueprint = target;
            selected = onSelected;
            resultImage.sprite = target.Recipe.Result.Icon;
            resultImage.enabled = resultImage.sprite != null;
            nameText.text = target.DisplayName;
            selectedImage.enabled = isSelected;
            rowButton.onClick.RemoveListener(OnClicked);
            rowButton.onClick.AddListener(OnClicked);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 선택한 설계도를 전달합니다.
        /// </summary>
        private void OnClicked()
        {
            selected?.Invoke(blueprint);
        }

        /// <summary>
        /// 버튼 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            rowButton.onClick.RemoveListener(OnClicked);
        }

        #endregion // 표시와 입력
    }
}
