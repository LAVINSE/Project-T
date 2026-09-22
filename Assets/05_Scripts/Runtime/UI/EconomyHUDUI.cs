using UnityEngine;

using TMPro;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 라운드·전체 적 처치 진행과 재화 잔액을 표시합니다. 미연결 재화는 대시입니다.
    /// </summary>
    public sealed class EconomyHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text killLabel;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private TMP_Text fragmentLabel;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 전달받은 진행과 잔액만 표시합니다. 재화를 지급하거나 보관하지 않습니다.
        /// </summary>
        public void Present(
            int currentRound,
            int totalRounds,
            int killed,
            int totalEnemies,
            double? coins,
            double? fragments)
        {
            roundLabel.text = currentRound + "/" + totalRounds;
            killLabel.text = killed + "/" + totalEnemies;
            coinLabel.text = coins.ExToCurrencyText();
            fragmentLabel.text = fragments.ExToCurrencyText();
        }

        #endregion // 표시
    }
}
