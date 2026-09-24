using UnityEngine;

using TMPro;

using SW.Base;

namespace ProjectT.UI
{
    /// <summary>
    /// 전투 조작 안내를 독립적으로 표시합니다. 진행 버튼과 전투 상태는 직접 변경하지 않습니다.
    /// </summary>
    public sealed class BattleMessageUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text messageText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 최신 조작 안내를 표시합니다. null은 빈 문구로 처리합니다.
        /// </summary>
        public void PresentMessage(string message)
        {
            messageText.text = message ?? string.Empty;
        }

        #endregion // 표시
    }
}
