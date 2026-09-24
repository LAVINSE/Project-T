using System;
using UnityEngine;

using SW.Stat;
using SW.Util;

namespace ProjectT.Data
{
    /// <summary>
    /// 원본 스탯 정의와 장비가 제공하는 증가량을 연결합니다. 실제 적용과 해제는 장착 모듈에서 담당합니다.
    /// </summary>
    [Serializable]
    public sealed class EquipmentStatBonus
    {
        #region 필드
        [SerializeField] private SWStat stat;
        [SerializeField, Min(0f)] private float amount;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 대상 스탯입니다. 미연결이면 효과 검사를 통과하지 못합니다.
        /// </summary>
        public SWStat Stat => stat;

        /// <summary>
        /// 스탯 단위의 고정 증가량입니다. 백분율 표시 스탯에서는 0.05가 5%p이며 0은 변화 없음입니다.
        /// </summary>
        public float Amount => amount;

        #endregion // 프로퍼티

        #region 생성
        /// <summary>
        /// 검증된 참조와 증가량을 보관합니다.
        /// </summary>
        private EquipmentStatBonus(SWStat stat, float amount)
        {
            this.stat = stat;
            this.amount = amount;
        }

        /// <summary>
        /// 등급별 결과를 독립 객체로 만듭니다. 참조 누락 또는 잘못된 증가량이면 null입니다.
        /// </summary>
        public static EquipmentStatBonus Create(SWStat stat, float amount)
        {
            if (stat == null || !amount.ExIsNonNegative())
            {
                SWLog.LogWarning("[EquipmentStatBonus] 생성 실패: 스탯과 0 이상 증가량이 필요합니다.");
                return null;
            }

            return new EquipmentStatBonus(stat, amount);
        }

        #endregion // 생성
    }
}
