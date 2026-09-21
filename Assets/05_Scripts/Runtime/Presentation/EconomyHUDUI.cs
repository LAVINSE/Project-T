using UnityEngine;

using TMPro;

using SW.Base;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 라운드·전체 적 처치 진행과 재화 잔액을 표시합니다. 미연결 재화는 대시입니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT", "ProjectT.Runtime", "EconomyHUDUI")]
    public sealed class EconomyHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text killLabel;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private TMP_Text fragmentLabel;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 표시에 필요한 참조가 모두 연결되어 있는지 반환합니다.
        /// </summary>
        public bool HasRequiredReferences => roundLabel != null && killLabel != null && coinLabel != null && fragmentLabel != null;

        #endregion // 프로퍼티

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
            coinLabel.text = CurrencyPresentation.Format(coins);
            fragmentLabel.text = CurrencyPresentation.Format(fragments);
        }

        #endregion // 표시
    }
}
