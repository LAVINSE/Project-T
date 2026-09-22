using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Units;

namespace ProjectT.UI
{
    /// <summary>
    /// 선택한 아군의 외형·체력·부활 상태를 표시하며 선택이 없으면 숨깁니다.
    /// </summary>
    public sealed class CharacterHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private Image portrait;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Image experienceFill;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Button[] skillButtons;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 아직 구현되지 않은 성장·스킬 표시를 비활성 상태로 둡니다.
        /// </summary>
        private void Awake()
        {
            experienceFill.fillAmount = 0f;
            levelLabel.text = "-";
            foreach (Button button in skillButtons)
            {
                button.interactable = false;
            }
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 아군의 초상화·체력·부활 남은 시간을 표시합니다. null이면 화면을 숨깁니다.
        /// </summary>
        public void Present(CharacterUnit unit)
        {
            bool visible = unit != null && unit.Health != null;
            visibility.alpha = visible ? 1f : 0f;
            visibility.blocksRaycasts = visible;
            visibility.interactable = visible;
            if (!visible)
            {
                return;
            }

            Health health = unit.Health;
            portrait.sprite = unit.Definition.Portrait;
            portrait.enabled = portrait.sprite != null;
            healthFill.fillAmount = health.Fraction;
            healthLabel.text = health.IsAlive
                ? Mathf.CeilToInt(health.Current) + "/" + health.Maximum.ToString("0")
                : "부활 " + Mathf.CeilToInt(unit.RevivalRemaining) + "초";
        }

        #endregion // 표시
    }
}
