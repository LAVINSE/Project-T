using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

namespace ProjectT.Data
{
    /// <summary>
    /// 설계도 아이템이 해금하는 제작법입니다. 비용은 결과 장비 희귀도의 기본값을 사용하고, 체크한 항목만 이 제작법의 값으로 덮어씁니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingRecipeData", menuName = "Project T/데이터/제작법")]
    public sealed class CraftingRecipeData : ProjectData
    {
        #region 필드
        [SerializeField] private EquipmentData result;

        [SWGroup("비용 덮어쓰기")]
        [SerializeField] private bool overrideSoulCost;
        [SerializeField, Min(0)] private double soulCost;
        [SerializeField] private bool overrideMaterials;
        [SerializeField] private CraftingMaterial[] materials = Array.Empty<CraftingMaterial>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 제작할 장비입니다. 성능 등급은 장비의 가중치로 추첨합니다.
        /// </summary>
        public EquipmentData Result => result;

        #endregion // 프로퍼티

        #region 비용
        /// <summary>
        /// 적용할 소울 비용입니다. 덮어쓰지 않았고 기본값도 없으면 0입니다.
        /// </summary>
        public double GetSoulCost(CraftingCostData defaults)
        {
            if (overrideSoulCost)
            {
                return soulCost;
            }

            return FindDefault(defaults)?.SoulCost ?? 0d;
        }

        /// <summary>
        /// 적용할 재료 목록입니다. 덮어쓰지 않았고 기본값도 없으면 빈 목록입니다.
        /// </summary>
        public IReadOnlyList<CraftingMaterial> GetMaterials(CraftingCostData defaults)
        {
            if (overrideMaterials)
            {
                return materials;
            }

            return FindDefault(defaults)?.Materials ?? Array.Empty<CraftingMaterial>();
        }

        /// <summary>
        /// 결과 장비 희귀도의 기본 비용을 찾습니다. 기본 비용 자산이나 희귀도가 없으면 null입니다.
        /// </summary>
        private CraftingCostEntry FindDefault(CraftingCostData defaults)
        {
            return defaults != null && result != null ? defaults.Find(result.Rarity) : null;
        }

        #endregion // 비용

        #region 검사
        /// <summary>
        /// 결과 장비와 덮어쓴 비용을 검사합니다. 덮어쓰지 않은 값은 기본 비용 자산에서 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckRequired(result, nameof(result), issues);
            if (result != null)
            {
                valid &= Check(result.IsValid, nameof(result), "결과 장비의 검사 오류를 먼저 수정하세요.", issues);
            }

            valid &= !overrideSoulCost || CheckNonNegative(soulCost, nameof(soulCost), issues);
            valid &= !overrideMaterials || ValidateMaterials(issues);
            return valid;
        }

        /// <summary>
        /// 덮어쓴 재료 항목마다 검사하고 같은 재료의 중복을 확인합니다.
        /// </summary>
        private bool ValidateMaterials(List<DataIssue> issues)
        {
            bool valid = true;
            var items = new HashSet<ItemData>();
            for (int index = 0; index < materials.Length; index++)
            {
                string path = nameof(materials) + ".Array.data[" + index + "]";
                CraftingMaterial material = materials[index];
                if (!Check(material != null, path, "빈 재료 항목을 제거하세요.", issues))
                {
                    valid = false;
                    continue;
                }

                valid &= material.Validate(this, path, issues);
                valid &= material.Item == null
                    || Check(items.Add(material.Item), path, "같은 재료가 중복되었습니다. 한 줄로 합쳐 주세요.", issues);
            }

            return valid;
        }

        #endregion // 검사
    }
}
