using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectT.Data
{
    /// <summary>
    /// 제작에 소비할 아이템과 수량입니다. 수량 0은 필요 없음으로 취급합니다.
    /// </summary>
    [Serializable]
    public sealed class CraftingMaterial
    {
        #region 필드
        [SerializeField] private ItemData item;
        [SerializeField, Min(0)] private long count;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 소비할 아이템입니다. 미연결이면 검사를 통과하지 못합니다.
        /// </summary>
        public ItemData Item => item;

        /// <summary>
        /// 제작 1회에 소비할 수량입니다. 0이면 필요 없습니다.
        /// </summary>
        public long Count => count;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 참조와 수량을 검사합니다. 설계도·장비 아이템은 재료로 쓸 수 없습니다. 목록이 있으면 소유 데이터 기준 경로로 문제를 추가합니다.
        /// </summary>
        public bool Validate(ProjectData owner, string propertyPath, List<DataIssue> issues)
        {
            if (item == null)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(item), "재료 아이템을 연결하세요."));
                return false;
            }

            bool valid = true;
            if (item.IsBlueprint || item.Equipment != null)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(item), "설계도와 장비는 재료로 사용할 수 없습니다."));
                valid = false;
            }

            if (count < 0)
            {
                issues?.Add(new DataIssue(owner, propertyPath + "." + nameof(count), "재료 수량은 0 이상이어야 합니다."));
                valid = false;
            }

            return valid;
        }

        #endregion // 검사
    }
}
