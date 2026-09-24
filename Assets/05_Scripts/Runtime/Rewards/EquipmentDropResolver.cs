using System;
using System.Collections.Generic;

using ProjectT.Data;
using SW.Util;

namespace ProjectT.Rewards
{
    /// <summary>
    /// 장비의 가중치 추첨 결과를 저장 가능한 등급별 아이템 정의로 연결합니다. 수량과 저장은 변경하지 않습니다.
    /// </summary>
    public sealed class EquipmentDropResolver
    {
        #region 필드
        private readonly EquipmentData equipment;
        private readonly Dictionary<EquipmentPerformanceGrade, ItemData> items;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 모든 추첨 가능한 등급이 연결된 결과만 보관합니다.
        /// </summary>
        private EquipmentDropResolver(EquipmentData equipment, Dictionary<EquipmentPerformanceGrade, ItemData> items)
        {
            this.equipment = equipment;
            this.items = items;
        }

        /// <summary>
        /// 추첨 가능한 등급마다 정확히 하나의 등록 아이템이 있는지 검사합니다. 누락·중복이면 null과 사유입니다.
        /// </summary>
        public static EquipmentDropResolver Create(EquipmentData equipment, IEnumerable<ItemData> definitions, out string reason)
        {
            reason = string.Empty;
            if (equipment == null || !equipment.IsValid || equipment.GetTotalSelectionWeight() <= 0d || definitions == null)
            {
                reason = "장비 등급·효과·가중치 또는 아이템 목록을 확인하세요.";
                SWLog.LogWarning("[EquipmentDropResolver] 생성 실패: " + reason);
                return null;
            }

            var candidates = new List<ItemData>(definitions);
            var items = new Dictionary<EquipmentPerformanceGrade, ItemData>();
            foreach (EquipmentPerformanceGrade grade in equipment.PerformanceGrades)
            {
                if (grade.SelectionWeight <= 0f)
                {
                    continue;
                }

                ItemData selected = null;
                foreach (ItemData item in candidates)
                {
                    if (item == null || item.Equipment != equipment || item.PerformanceGrade == null
                        || !string.Equals(item.PerformanceGrade.CodeName, grade.PerformanceGrade.CodeName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (selected != null || !item.IsValid)
                    {
                        reason = equipment.DisplayName + " / " + grade.PerformanceGrade.DisplayName + ": 등급별 아이템이 중복되었거나 유효하지 않습니다.";
                        SWLog.LogWarning("[EquipmentDropResolver] 생성 실패: " + reason);
                        return null;
                    }

                    selected = item;
                }

                if (selected == null)
                {
                    reason = equipment.DisplayName + " / " + grade.PerformanceGrade.DisplayName + ": 해당 등급의 아이템을 만들고 아이템 목록에 등록하세요.";
                    SWLog.LogWarning("[EquipmentDropResolver] 생성 실패: " + reason);
                    return null;
                }

                items.Add(grade, selected);
            }

            return new EquipmentDropResolver(equipment, items);
        }

        #endregion // 초기화

        #region 추첨
        /// <summary>
        /// 난수 하나로 저장할 아이템을 결정합니다. 난수 범위 오류나 변경된 등급 연결은 false입니다.
        /// </summary>
        public bool TryResolve(double sample, out ItemData item)
        {
            item = null;
            return equipment.TrySelectPerformanceGrade(sample, out EquipmentPerformanceGrade grade)
                && items.TryGetValue(grade, out item);
        }

        #endregion // 추첨
    }
}
