using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Battle;

namespace ProjectT.UI
{
    /// <summary>
    /// 개체별 통계 한 줄을 표시하고 선택한 기록을 전달합니다. 화면을 다시 사용하면 이전 선택 연결을 교체합니다.
    /// </summary>
    public sealed class CharacterDPSRowUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Button selectButton;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private Image damageFillImage;
        private CharacterBattleStatistics record;
        private Action<CharacterBattleStatistics> selected;
        private bool subscribed;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 전달받은 기록을 표시하고 선택 알림을 연결합니다.
        /// </summary>
        public void Present(CharacterBattleStatistics statistics, Action<CharacterBattleStatistics> onSelected, double highestDamage = 0d)
        {
            record = statistics;
            selected = onSelected;
            portraitImage.sprite = record.Portrait;
            portraitImage.enabled = record.Portrait != null;
            damageFillImage.fillAmount = highestDamage > 0d
                ? Mathf.Clamp01((float)(record.DamageDealt / highestDamage))
                : 0f;
            damageText.text = record.DamageDealt.ToString("N0");
            killsText.text = record.KillCount.ToString("N0");
            if (!subscribed)
            {
                selectButton.onClick.AddListener(RequestSelection);
                subscribed = true;
            }
        }

        /// <summary>
        /// 현재 줄에 연결한 기록만 선택 알림으로 전달합니다.
        /// </summary>
        private void RequestSelection()
        {
            if (record != null)
            {
                selected?.Invoke(record);
            }
        }

        #endregion // 표시

        #region 정리
        /// <summary>
        /// 버튼 구독과 선택 콜백을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (subscribed && selectButton != null)
            {
                selectButton.onClick.RemoveListener(RequestSelection);
            }

            selected = null;
            record = null;
        }

        #endregion // 정리
    }
}
