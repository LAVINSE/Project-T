using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using ProjectT.Data;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 사용자 정보 팝업에 아이템과 등급별 효과를 표시하고 내용 높이에 맞춰 배치합니다.
    /// </summary>
    public sealed class InventorySlotInfoUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private InventoryItemHeader header;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image currencyImage;
        [SerializeField] private RectTransform popupRect;
        [SerializeField] private RectTransform statGroupRect;
        [SerializeField] private ItemStatBoxUI statBoxPrefab;
        private readonly List<ItemStatBoxUI> rows = new List<ItemStatBoxUI>();
        private ColorData colors;
        private SpriteData sprites;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 공통 등급 표시 데이터를 전달받습니다.
        /// </summary>
        public void Initialize(ColorData colorData, SpriteData spriteData)
        {
            colors = colorData;
            sprites = spriteData;
            rows.AddRange(statGroupRect.GetComponentsInChildren<ItemStatBoxUI>(true));
            Hide();
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 보유 아이템의 내용과 높이를 갱신한 뒤 화면 안에 표시합니다. 빈 슬롯이면 닫습니다.
        /// </summary>
        public void Present(InventoryStack item, Vector2 position, Camera eventCamera)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            header.Present(item, colors, sprites);
            var rarity = item.Definition?.Equipment?.Rarity;
            Color color = colors.GetGradeColor(rarity);
            gradeText.text = rarity != null ? rarity.DisplayName : string.Empty;
            gradeText.color = color;
            descriptionText.text = BuildDescription(item, color);
            PresentPrice(item.Definition);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            popupRect.ExPlaceInside(position, eventCamera, new Vector2(18f, -18f));
        }

        /// <summary>
        /// 판매 재화의 아이콘과 금액을 표시합니다. 그림이 없으면 재화 이름을 함께 표시합니다.
        /// </summary>
        private void PresentPrice(ItemData item)
        {
            CurrencyData currency = item?.SellCurrency;
            currencyImage.sprite = currency != null ? currency.Icon : null;
            currencyImage.enabled = currencyImage.sprite != null;
            priceText.text = (item?.SellPrice).ExToSellPriceText(currency, !currencyImage.enabled);
        }

        /// <summary>
        /// 포함된 스탯 행과 보유·성능 등급·설명을 표시합니다. 미완성 효과는 안내합니다.
        /// </summary>
        private string BuildDescription(InventoryStack item, Color color)
        {
            foreach (ItemStatBoxUI row in rows)
            {
                row.gameObject.SetActive(false);
            }

            var descriptions = new List<string>();
            descriptions.Add("보유 " + item.Count.ToString("N0") + "개");
            var equipment = item.Definition?.Equipment;
            statGroupRect.gameObject.SetActive(false);
            if (equipment != null)
            {
                if (equipment.TryGetPerformanceGrade(item.Definition.PerformanceGrade, out var grade)
                    && grade.TryResolveStatBonuses(out var bonuses))
                {
                    descriptions.Add("성능 등급: " + grade.PerformanceGrade.DisplayName);
                    PresentStats(bonuses, color);
                    AppendEffectDescriptions(grade, descriptions);
                }
                else
                {
                    descriptions.Add("등급 효과 설정을 확인하세요.");
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                descriptions.Add(item.Description);
            }

            return string.Join("\n", descriptions);
        }

        /// <summary>
        /// 해결된 증가량만 필요한 행 수만큼 표시합니다. 빈 목록은 스탯 영역을 숨깁니다.
        /// </summary>
        private void PresentStats(IReadOnlyList<EquipmentStatBonus> bonuses, Color color)
        {
            for (int index = 0; index < bonuses.Count; index++)
            {
                if (index == rows.Count)
                {
                    rows.Add(Instantiate(statBoxPrefab, statGroupRect));
                }

                EquipmentStatBonus bonus = bonuses[index];
                float value = bonus.Stat.IsPercentType ? bonus.Amount * 100f : bonus.Amount;
                rows[index].Present(bonus.Stat.DisplayName,
                    "+" + value.ToString("0.###") + (bonus.Stat.IsPercentType ? "%p" : string.Empty), color);
            }

            statGroupRect.gameObject.SetActive(bonuses.Count > 0);
        }

        /// <summary>
        /// 스탯 행과 중복되지 않는 능력 설명을 목록에 추가합니다.
        /// </summary>
        private void AppendEffectDescriptions(EquipmentPerformanceGrade grade, List<string> descriptions)
        {
            if (!(grade.Effect is EquipmentStatEffectData))
            {
                descriptions.Add(grade.Effect.EffectDescription);
            }

            if (grade.AdditionalEffects == null)
            {
                return;
            }

            foreach (EquipmentEffectData effect in grade.AdditionalEffects)
            {
                if (effect != null)
                {
                    descriptions.Add(effect.EffectDescription);
                }
            }
        }

        /// <summary>
        /// 슬롯 상세를 닫습니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion // 표시
    }
}
