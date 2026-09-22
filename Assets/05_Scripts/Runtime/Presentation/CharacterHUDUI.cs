using UnityEngine;

using TMPro;

using SW.Base;

using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 선택한 아군의 외형·체력·부활 상태를 표시하며 선택이 없으면 숨깁니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT", "ProjectT.Runtime", "CharacterHUDUI")]
    public sealed class CharacterHUDUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private UnityEngine.UI.Image portrait;
        [SerializeField] private UnityEngine.UI.Image healthFill;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private UnityEngine.UI.Image experienceFill;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private UnityEngine.UI.Button[] skillButtons;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 표시에 필요한 필수 참조입니다.
        /// </summary>
        public bool HasRequiredReferences => visibility != null
            && portrait != null
            && healthFill != null
            && healthLabel != null
            && experienceFill != null
            && levelLabel != null;

        #endregion // 프로퍼티

        #region 표시
        /// <summary>
        /// 개체 상태를 표시하며 미구현 성장·스킬에 임시 진행 수치를 넣지 않습니다.
        /// </summary>
        public void Present(CharacterUnit unit)
        {
            bool visible = unit != null && unit.Health != null;
            visibility.alpha = visible ? 1f : 0f;
            visibility.blocksRaycasts = visible;
            visibility.interactable = visible;
            experienceFill.fillAmount = 0f;
            levelLabel.text = "-";
            foreach (UnityEngine.UI.Button button in skillButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }

            if (!visible)
            {
                return;
            }

            portrait.sprite = unit.Definition.Portrait;
            portrait.enabled = portrait.sprite != null;
            healthFill.fillAmount = unit.Health.Maximum > 0f ? unit.Health.Current / unit.Health.Maximum : 0f;
            healthLabel.text = unit.Health.IsAlive
                ? Mathf.CeilToInt(unit.Health.Current) + "/" + unit.Health.Maximum.ToString("0")
                : "부활 " + Mathf.CeilToInt(unit.RevivalRemaining) + "초";
        }

        #endregion // 표시
    }
}
