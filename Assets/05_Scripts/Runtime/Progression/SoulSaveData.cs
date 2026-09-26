using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Util;

namespace ProjectT.Progression
{
    /// <summary>
    /// 소울 잔액과 지급 완료 식별자를 함께 저장합니다. 정의 자산의 이름이나 경로에 의존하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class SoulSaveData : IProjectSaveData
    {
        #region 필드
        [SerializeField] private int version;
        [SerializeField] private double balance;
        [SerializeField] private List<string> grantedRewards;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 저장이 완료된 영구 소울 잔액입니다.
        /// </summary>
        public double Balance => balance;

        /// <summary>
        /// 중복 지급 방지를 위한 지급 완료 식별자입니다.
        /// </summary>
        public IReadOnlyList<string> GrantedRewards => grantedRewards;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 잔액과 지급 기록으로 저장 상태를 준비합니다.
        /// </summary>
        private SoulSaveData(double balance, List<string> grantedRewards)
        {
            version = ProjectDefine.Save.SoulVersion;
            this.balance = balance;
            this.grantedRewards = grantedRewards;
        }

        /// <summary>
        /// 저장 파일이 없는 신규 사용자의 빈 상태를 만듭니다. 정의의 기본 보상 수량을 시작 잔액으로 사용하지 않습니다.
        /// </summary>
        public static SoulSaveData CreateEmpty()
        {
            return new SoulSaveData(0d, new List<string>());
        }

        #endregion // 초기화

        #region 검사와 변경
        /// <summary>
        /// 버전·잔액·지급 기록을 검사합니다. 손상되거나 지원하지 않는 형식은 false이며 초기화하지 않습니다.
        /// </summary>
        public bool Validate(out string reason)
        {
            reason = string.Empty;
            if (version != ProjectDefine.Save.SoulVersion || !balance.ExIsNonNegative() || grantedRewards == null)
            {
                reason = "소울 저장 버전·잔액·지급 기록을 읽을 수 없습니다. 기존 파일을 보존합니다.";
                return false;
            }

            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (string identifier in grantedRewards)
            {
                if (!Guid.TryParseExact(identifier, "N", out _) || !identifiers.Add(identifier))
                {
                    reason = "소울 지급 기록이 잘못되었거나 중복되었습니다. 기존 파일을 보존합니다.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 기존 상태를 보존하면서 저장할 후보를 만듭니다. 잘못된 금액·식별자·중복 요청은 null입니다.
        /// </summary>
        internal SoulSaveData CreateCredit(string identifier, double amount)
        {
            if (!Guid.TryParseExact(identifier, "N", out _)
                || !amount.ExIsNonNegative()
                || !(balance + amount).ExIsFinite()
                || grantedRewards.Contains(identifier))
            {
                SWLog.LogWarning("[SoulSaveData] 지급 후보 생성 실패: 금액 또는 지급 식별자를 확인하세요.");
                return null;
            }

            var nextRewards = new List<string>(grantedRewards) { identifier };
            return new SoulSaveData(balance + amount, nextRewards);
        }

        /// <summary>
        /// 지급 기록 없이 잔액만 바꾼 후보를 만듭니다. 결과가 0 미만이거나 계산할 수 없으면 null입니다.
        /// </summary>
        internal SoulSaveData CreateAdjustment(double delta)
        {
            double next = balance + delta;
            if (!delta.ExIsFinite() || !next.ExIsNonNegative())
            {
                return null;
            }

            return new SoulSaveData(next, new List<string>(grantedRewards));
        }

        #endregion // 검사와 변경
    }
}
