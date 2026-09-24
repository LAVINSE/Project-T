using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 공격별 DPS와 횟수를 표시합니다. 미등록 공격에는 가상의 기록을 표시하지 않습니다.
    /// </summary>
    public sealed class SkillInfoUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text damagePerSecondText;
        [SerializeField] private TMP_Text countTitleText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image iconImage;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 유효한 공격 기록을 표시합니다. DPS와 횟수의 미설정 값은 대시로 표시합니다.
        /// </summary>
        public void Present(string attackName, double? damagePerSecond, int? count, bool basicAttack, Sprite sprite)
        {
            nameText.text = attackName;
            damagePerSecondText.text = damagePerSecond.HasValue && damagePerSecond.Value.ExIsNonNegative()
                ? damagePerSecond.Value.ToString("N2")
                : "미설정";
            countTitleText.text = basicAttack ? "적중 횟수 :" : "사용 횟수 :";
            countText.text = count.HasValue ? count.Value.ToString("N0") : "-";
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        #endregion // 표시
    }
}
