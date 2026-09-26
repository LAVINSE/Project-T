using System;
using System.Collections.Generic;

using SW.SkillTree;
using SW.Util;

using ProjectT.Progression;

namespace ProjectT.Research
{
    /// <summary>
    /// SWSkillTree의 재화 요청을 영구 소울 지갑에 연결합니다. 소울 외 재화는 잔액 0이며 거래를 거절합니다.
    /// </summary>
    public sealed class ResearchSoulWallet : ISWSkillTreeWallet, IDisposable
    {
        #region 필드
        private readonly SoulWallet souls;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 소울 잔액이 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 불러오기에 성공한 소울 지갑을 연결합니다.
        /// </summary>
        public ResearchSoulWallet(SoulWallet souls)
        {
            this.souls = souls;
            this.souls.Changed += OnSoulsChanged;
        }

        #endregion // 초기화

        #region 거래
        /// <summary>
        /// 소울이면 저장된 잔액, 다른 재화이면 0입니다.
        /// </summary>
        public double GetBalance(string currency)
        {
            return currency == ProjectDefine.Research.SoulCurrency ? souls.Balance : 0d;
        }

        /// <summary>
        /// 소울 합계를 한 번에 차감하거나 돌려줍니다. 다른 재화가 섞였거나 저장에 실패하면 잔액을 바꾸지 않고 false입니다.
        /// </summary>
        public bool TryExchange(IReadOnlyList<SWSkillTreeAmount> amounts, bool credit)
        {
            if (amounts == null)
            {
                return false;
            }

            double total = 0d;
            foreach (SWSkillTreeAmount amount in amounts)
            {
                if (!amount.IsValid || amount.currency != ProjectDefine.Research.SoulCurrency)
                {
                    SWLog.LogWarning("[ResearchSoulWallet] 거래 거절: 연구 비용은 소울만 사용할 수 있습니다.");
                    return false;
                }

                total += amount.value;
            }

            string reason;
            bool success = credit ? souls.TryRefund(total, out reason) : souls.TrySpend(total, out reason);
            if (!success)
            {
                SWLog.LogWarning("[ResearchSoulWallet] 거래 실패: " + reason);
            }

            return success;
        }

        #endregion // 거래

        #region 정리
        /// <summary>
        /// 소울 지갑의 알림을 그대로 전달합니다.
        /// </summary>
        private void OnSoulsChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// 소울 지갑 구독을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            souls.Changed -= OnSoulsChanged;
            Changed = null;
        }

        #endregion // 정리
    }
}
