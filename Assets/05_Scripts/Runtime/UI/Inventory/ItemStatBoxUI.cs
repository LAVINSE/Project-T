using UnityEngine;

using TMPro;

namespace ProjectT.UI
{
    /// <summary>
    /// 장비 효과 한 줄의 이름과 등급 색상 수치를 표시합니다.
    /// </summary>
    public sealed class ItemStatBoxUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private TMP_Text statNameText;
        [SerializeField] private TMP_Text statText;

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 계산된 효과 문자열을 표시하며 스탯 원본은 변경하지 않습니다.
        /// </summary>
        public void Present(string title, string value, Color color)
        {
            statNameText.text = title;
            statText.text = value;
            statText.color = color;
            gameObject.SetActive(true);
        }

        #endregion // 표시
    }
}
