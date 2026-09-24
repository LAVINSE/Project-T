using UnityEngine;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.Inventory
{
    /// <summary>
    /// 한 종류의 보유 수량과 현재 표시 정의를 묶은 읽기 전용 값입니다. 정의가 사라져도 저장된 수량은 유지합니다.
    /// </summary>
    public sealed class InventoryStack
    {
        #region 프로퍼티
        /// <summary>
        /// 파괴 요청과 저장에 사용하는 고정 식별자입니다.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// 실제 보유 수량입니다.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// 현재 아이템 정의입니다. 자산이 삭제되었으면 null입니다.
        /// </summary>
        public ItemData Definition { get; }

        /// <summary>
        /// 표시 이름입니다. 정의가 없으면 복원할 수 없는 아이템임을 알립니다.
        /// </summary>
        public string DisplayName => Definition != null ? Definition.DisplayName : "정의가 없는 아이템";

        /// <summary>
        /// 설명입니다. 정의가 없으면 원본 데이터 누락을 안내합니다.
        /// </summary>
        public string Description => Definition != null ? Definition.Description : "원본 아이템 데이터를 찾을 수 없습니다. 보유 수량은 유지됩니다.";

        /// <summary>
        /// 아이템 그림입니다. 정의 또는 그림이 없으면 null입니다.
        /// </summary>
        public Sprite Icon => Definition != null ? Definition.Icon : null;

        /// <summary>
        /// 분류입니다. 미연결 정의는 기타로 표시합니다.
        /// </summary>
        public SWCategory Category => Definition != null ? Definition.Category : null;

        /// <summary>
        /// 상세에 표시할 분류 이름입니다.
        /// </summary>
        public string CategoryName => Category != null ? Category.DisplayName : "기타";

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 저장 검사에 성공한 수량과 대응하는 정의를 연결합니다.
        /// </summary>
        internal InventoryStack(InventoryQuantity quantity, ItemData definition)
        {
            Identifier = quantity.Identifier;
            Count = quantity.Count;
            Definition = definition;
        }

        #endregion // 초기화
    }
}
