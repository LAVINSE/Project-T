using System.Collections.Generic;
using UnityEngine;

using SW.Base;

namespace ProjectT.Data
{
    /// <summary>
    /// 영구 아이템 종류의 이름·그림·설명·분류·기본 지급량입니다. 장착·사용 효과와 보유 수량은 별도 모듈에서 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/데이터/아이템", fileName = "ItemData")]
    public sealed class ItemData : RewardData
    {
        #region 필드
        [SerializeField] private SWCategory category;
        [SerializeField, Tooltip("획득 저장에 성공하면 인벤토리 아이콘 연출을 표시합니다.")] private bool specialLoot;
        [SerializeField, TextArea(3, 8)] private string description;
        [SerializeField, Min(0)] private double sellPrice;
        [SerializeField] private CurrencyData sellCurrency;
        [SerializeField] private EquipmentData equipment;
        [SerializeField] private SWCategory performanceGrade;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 인벤토리 분류입니다. 미연결 아이템은 기타로 표시합니다.
        /// </summary>
        public SWCategory Category => category;

        /// <summary>
        /// 자동 보관 후 획득 연출을 표시할 특별 전리품인지 반환합니다. 미지정이면 일반 아이템입니다.
        /// </summary>
        public bool IsSpecialLoot => specialLoot;

        /// <summary>
        /// 마우스를 올렸을 때 표시할 설명입니다. 미작성 시 빈 문자열입니다.
        /// </summary>
        public string Description => description ?? string.Empty;

        /// <summary>
        /// 한 개의 판매가입니다. 장비 아이템은 연결한 장비의 공통 판매가를 사용합니다.
        /// </summary>
        public double SellPrice => equipment != null ? equipment.SellPrice : sellPrice;

        /// <summary>
        /// 판매 시 받는 재화입니다. 미지정이면 판매가를 표시하지 않으며 장비는 장비 정의를 따릅니다.
        /// </summary>
        public CurrencyData SellCurrency => equipment != null ? equipment.SellCurrency : sellCurrency;

        /// <summary>
        /// 장비 아이템의 종류·희귀도·등급별 효과 정의입니다. 일반 아이템은 연결하지 않습니다.
        /// </summary>
        public EquipmentData Equipment => equipment;

        /// <summary>
        /// 장비는 장비 정의의 그림을 사용합니다. 장비 그림이 없으면 null이며 일반 아이템은 자체 그림을 사용합니다.
        /// </summary>
        public override Sprite Icon => equipment != null ? equipment.Icon : base.Icon;

        /// <summary>
        /// 이 아이템이 보관하는 고정 성능 등급입니다. 장비를 연결했다면 그 장비의 등급 목록에 있어야 합니다.
        /// </summary>
        public SWCategory PerformanceGrade => performanceGrade;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 표시 정보와 정수 지급 수량을 확인합니다. 잘못된 수량은 지급하지 않습니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = base.Validate(issues);
            valid &= equipment != null || CheckNonNegative(sellPrice, nameof(sellPrice), issues);
            valid &= equipment != null || Check(sellCurrency == null || sellCurrency.IsValid,
                nameof(sellCurrency), "판매 재화의 검사 오류를 확인하세요.", issues);
            valid &= Check(DefaultAmount.ExIsItemCount(), "defaultAmount", "아이템 수량은 계산 가능한 범위의 0 이상 정수여야 합니다.", issues);
            valid &= Check(category == null || !string.IsNullOrWhiteSpace(category.CodeName), nameof(category), "아이템 분류에 코드명이 필요합니다.", issues);
            valid &= Check(equipment != null || performanceGrade == null, nameof(equipment), "성능 등급을 지정한 아이템은 장비 정의를 연결하세요.", issues);
            if (equipment != null)
            {
                valid &= Check(equipment.IsValid, nameof(equipment), "장비 정의의 검사 오류를 먼저 수정하세요.", issues);
                valid &= Check(equipment.TryGetPerformanceGrade(performanceGrade, out _), nameof(performanceGrade), "장비에 등록된 성능 등급을 연결하세요.", issues);
            }

            return valid;
        }

        /// <summary>
        /// 동일 장비·성능 등급이 다른 아이템으로 나뉘는 것을 검사합니다. 편집 원본은 제외하고 중복이면 false입니다.
        /// </summary>
        public bool ValidateEquipmentIdentity(IEnumerable<ItemData> items, ItemData source, List<DataIssue> issues)
        {
            if (equipment == null || performanceGrade == null || items == null)
            {
                return true;
            }

            bool valid = true;
            foreach (ItemData item in items)
            {
                if (item == null || item == this || item == source || item.Equipment != equipment || item.PerformanceGrade == null)
                {
                    continue;
                }

                valid &= Check(item.PerformanceGrade.CodeName != performanceGrade.CodeName, nameof(performanceGrade),
                    "같은 장비와 성능 등급이 이미 연결된 아이템이 있습니다: " + item.name + ". 해당 아이템을 재사용하세요.", issues);
            }

            return valid;
        }

        #endregion // 검사
    }
}
