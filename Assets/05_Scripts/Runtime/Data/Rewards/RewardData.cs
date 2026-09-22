using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    /// <summary>
    /// 재화와 아이템의 공통 정의입니다. 소유 수량을 보관하지 않고 보상 기본값만 제공합니다.
    /// </summary>
    public abstract class RewardData : ProjectData
    {
        #region 필드
        [SerializeField] private string displayName = "새 보상";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private double defaultAmount = 1d;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 보상 목록과 게임 화면에 사용할 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 선택적으로 표시할 보상 그림입니다. 없으면 기본 자산 아이콘을 사용합니다.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// 보상 항목에서 수량을 덮어쓰지 않을 때 사용하는 기본 수량입니다.
        /// </summary>
        public double DefaultAmount => defaultAmount;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 표시 이름과 기본 수량을 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckName(displayName, nameof(displayName), issues);
            valid &= CheckNonNegative(defaultAmount, nameof(defaultAmount), issues);
            return valid;
        }

        #endregion // 검사
    }
}
