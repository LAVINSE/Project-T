using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using ProjectT.Data;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 정보·버리기 화면이 공유하는 아이템 머리글입니다. 보유 상태를 변경하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class InventoryItemHeader
    {
        #region 필드
        [SerializeField] private Image itemImage;
        [SerializeField] private Image gradeBoxImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemTypeText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 아이콘·이름·분류와 희귀도 외형을 표시합니다. 없는 아이콘은 빈칸입니다.
        /// </summary>
        public void Present(InventoryStack item, ColorData colors, SpriteData sprites)
        {
            var rarity = item.Definition?.Equipment?.Rarity;
            itemImage.sprite = item.Icon;
            itemImage.enabled = item.Icon != null;
            gradeBoxImage.sprite = sprites.GetGradeBox(rarity);
            itemNameText.text = item.DisplayName;
            itemNameText.color = colors.GetGradeColor(rarity);
            itemTypeText.text = item.CategoryName;
        }

        #endregion // 표시
    }
}
