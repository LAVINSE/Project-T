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
        [SerializeField] private CanvasGroup visibilityGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Image experienceFillImage;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button[] skillButtons;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 선택 전의 성장 표시와 미등록 스킬 버튼을 초기화합니다.
        /// </summary>
        private void Awake()
        {
            experienceFillImage.fillAmount = 0f;
            levelText.text = "-";
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
            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.blocksRaycasts = visible;
            visibilityGroup.interactable = visible;
            if (!visible)
            {
                return;
            }

            Health health = unit.Health;
            portraitImage.sprite = unit.Definition.Portrait;
            portraitImage.enabled = portraitImage.sprite != null;
            levelText.text = unit.Definition.Level.ToString();
            experienceFillImage.fillAmount = unit.Definition.Experience.ExToExperienceFraction(unit.Definition.RequiredExperience);
            healthFillImage.fillAmount = health.Fraction;
            healthText.text = health.IsAlive
                ? Mathf.CeilToInt(health.Current) + "/" + health.Maximum.ToString("0")
                : "부활 " + Mathf.CeilToInt(unit.RevivalRemaining) + "초";
        }

        #endregion // 표시
    }
}
