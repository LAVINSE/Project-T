using SW.Util;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 재화의 소유자와 무관하게 잔액을 표시합니다. 미연결·빈 잔액은 대시입니다.
    /// </summary>
    public static class CurrencyPresentation
    {
        #region 함수
        /// <summary>
        /// 유효한 양수 잔액만 SWUtils 형식으로 표시하며 없는 재화는 대시를 반환합니다.
        /// </summary>
        public static string Format(double? balance)
        {
            if (!balance.HasValue || balance.Value <= 0d || double.IsNaN(balance.Value) || double.IsInfinity(balance.Value))
            {
                return "-";
            }

            return SWAmountFormat.Format(balance.Value, null);
        }

        #endregion // 함수
    }
}
