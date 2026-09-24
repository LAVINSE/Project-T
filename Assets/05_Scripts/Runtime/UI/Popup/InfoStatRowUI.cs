using UnityEngine;

using TMPro;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 사용자 제작 능력치 행의 이름과 값을 표시합니다. 게임 규칙은 계산하지 않습니다.
    /// </summary>
    public sealed class InfoStatRowUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text valueText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 전달받은 이름과 값을 표시합니다.
        /// </summary>
        public void Present(string title, string value)
        {
            titleText.text = title ?? string.Empty;
            valueText.text = value ?? string.Empty;
        }

        #endregion // 표시
    }
}
