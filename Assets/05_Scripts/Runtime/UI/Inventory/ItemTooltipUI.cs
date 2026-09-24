using UnityEngine;

using TMPro;

using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 마우스를 올린 아이템의 이름·분류·수량·설명을 화면 안에 표시합니다. 입력은 가로채지 않습니다.
    /// </summary>
    public sealed class ItemTooltipUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text informationText;
        [SerializeField] private TMP_Text descriptionText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 아이템을 표시합니다. 비어 있는 설명은 안내 문구로 대신합니다.
        /// </summary>
        public void Present(InventoryStack item, Vector2 position, Camera eventCamera)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            titleText.text = item.DisplayName;
            informationText.text = item.CategoryName + "  ·  보유 " + item.Count.ToString("N0") + "개";
            descriptionText.text = string.IsNullOrWhiteSpace(item.Description) ? "등록된 설명이 없습니다." : item.Description;
            if (item.Definition?.Equipment != null
                && item.Definition.Equipment.TryGetPerformanceGrade(item.Definition.PerformanceGrade, out var grade))
            {
                informationText.text += "\n" + (item.Definition.Equipment.Rarity != null ? item.Definition.Equipment.Rarity.DisplayName : "희귀도 미설정")
                    + " · " + grade.PerformanceGrade.DisplayName;
                descriptionText.text = (string.IsNullOrWhiteSpace(item.Description) ? string.Empty : item.Description + "\n")
                    + grade.EffectDescription;
            }
            gameObject.SetActive(true);
            ((RectTransform)transform).ExPlaceInside(position, eventCamera, new Vector2(18f, -18f));
        }

        /// <summary>
        /// 상세 표시를 닫습니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion // 표시
    }
}
