using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 라운드·전체 적 처치 진행과 재화 잔액을 표시합니다. 미연결 재화는 대시입니다.
    /// </summary>
    public sealed class EconomyHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text killText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text fragmentText;
        [SerializeField] private TMP_Text soulText;
        [SerializeField] private Image soulImage;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 소울 정의의 아이콘을 연결합니다. 미설정이면 기존 디자인을 유지하며 잔액을 만들지 않습니다.
        /// </summary>
        public void Initialize(CurrencyData soulCurrency)
        {
            if (soulCurrency != null && soulCurrency.Icon != null && soulImage != null)
            {
                soulImage.sprite = soulCurrency.Icon;
            }
        }

        /// <summary>
        /// 전달받은 진행과 잔액만 표시합니다. 재화를 지급하거나 보관하지 않습니다.
        /// </summary>
        public void Present(
            int currentRound,
            int totalRounds,
            int killed,
            int totalEnemies,
            double? coins,
            double? fragments,
            double? souls)
        {
            roundText.text = currentRound + "/" + totalRounds;
            killText.text = killed + "/" + totalEnemies;
            coinText.text = coins.ExToCurrencyText();
            fragmentText.text = fragments.ExToCurrencyText();
            if (soulText != null)
            {
                soulText.text = souls.ExToCurrencyText();
            }
        }

        #endregion // 표시
    }
}
