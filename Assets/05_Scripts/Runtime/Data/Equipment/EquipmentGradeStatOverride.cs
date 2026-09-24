using System;
using UnityEngine;

using SW.Stat;

namespace ProjectT.Data
{
    /// <summary>
    /// 공유 효과에 있는 스탯의 등급별 포함 여부와 증가량을 재정의합니다. 원본 효과는 변경하지 않습니다.
    /// </summary>
    [Serializable]
    public sealed class EquipmentGradeStatOverride
    {
        #region 필드
        [SerializeField] private SWStat stat;
        [SerializeField] private bool enabled = true;
        [SerializeField] private bool useAmountOverride;
        [SerializeField, Min(0f)] private float amount;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 공유 효과 안에서 재정의할 스탯입니다. 미연결은 유효하지 않습니다.
        /// </summary>
        public SWStat Stat => stat;

        /// <summary>
        /// 이 등급에서 해당 스탯을 포함할지 반환합니다.
        /// </summary>
        public bool Enabled => enabled;

        /// <summary>
        /// 공유 증가량 대신 이 항목의 증가량을 사용할지 반환합니다.
        /// </summary>
        public bool UseAmountOverride => useAmountOverride;

        /// <summary>
        /// 공유값에 더하지 않고 대체하는 증가량입니다.
        /// </summary>
        public float Amount => amount;

        #endregion // 프로퍼티
    }
}
