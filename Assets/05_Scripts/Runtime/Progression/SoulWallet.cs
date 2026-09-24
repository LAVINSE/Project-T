using System;
using System.Collections.Generic;

using SW.Util;

namespace ProjectT.Progression
{
    /// <summary>
    /// 저장에 성공한 소울만 잔액에 반영합니다. 전투 승패와 무관하며 같은 지급 식별자는 한 번만 반영합니다.
    /// </summary>
    public sealed class SoulWallet
    {
        #region 필드
        private readonly SoulSaveStore store;
        private readonly HashSet<string> grantedRewards;
        private SoulSaveData data;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 마지막 저장에 성공한 잔액입니다.
        /// </summary>
        public double Balance => data.Balance;

        /// <summary>
        /// 저장과 잔액 반영이 모두 끝난 뒤 발생합니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 불러오기에 성공한 상태와 저장소를 연결합니다.
        /// </summary>
        private SoulWallet(SoulSaveStore store, SoulSaveData data)
        {
            this.store = store;
            this.data = data;
            grantedRewards = new HashSet<string>(data.GrantedRewards, StringComparer.Ordinal);
        }

        /// <summary>
        /// 저장 상태를 불러와 지갑을 만듭니다. 실패 시 null과 원인을 반환하며 파일을 변경하지 않습니다.
        /// </summary>
        public static SoulWallet Create(SoulSaveStore store, out string reason)
        {
            if (store == null)
            {
                reason = "소울 저장소가 연결되지 않았습니다.";
                SWLog.LogWarning("[SoulWallet] 생성 실패: " + reason);
                return null;
            }

            if (!store.TryLoad(out SoulSaveData data, out reason))
            {
                SWLog.LogWarning("[SoulWallet] 생성 실패: " + reason);
                return null;
            }

            return new SoulWallet(store, data);
        }

        #endregion // 초기화

        #region 지급
        /// <summary>
        /// 지급 식별자와 금액을 저장한 뒤 잔액을 반영합니다. 중복은 성공으로 끝내며 저장 실패 시 잔액을 유지합니다.
        /// </summary>
        public bool TryCredit(string identifier, double amount, out string reason)
        {
            reason = string.Empty;
            if (!Guid.TryParseExact(identifier, "N", out _) || !amount.ExIsNonNegative())
            {
                reason = "소울 지급 식별자 또는 수량이 잘못되었습니다.";
                return false;
            }

            if (grantedRewards.Contains(identifier) || amount == 0d)
            {
                return true;
            }

            SoulSaveData candidate = data.CreateCredit(identifier, amount);
            if (candidate == null)
            {
                reason = "소울 지급 후 잔액이 계산 가능한 범위를 벗어났습니다.";
                return false;
            }

            if (!store.TrySave(candidate))
            {
                reason = "소울 저장 실패로 보상 지급을 보류했습니다. 저장 공간과 파일 접근 권한을 확인하세요.";
                return false;
            }

            data = candidate;
            grantedRewards.Add(identifier);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 화면 구독자의 실패가 저장 완료 결과를 바꾸지 않도록 각 알림을 분리합니다.
        /// </summary>
        private void NotifyChanged()
        {
            if (Changed == null)
            {
                return;
            }

            foreach (Action subscriber in Changed.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    SWLog.LogError("[SoulWallet] 잔액 표시 알림 실패: " + exception);
                }
            }
        }

        #endregion // 지급
    }
}
