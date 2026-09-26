using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;

namespace ProjectT.Data
{
    /// <summary>
    /// 결과 장비의 희귀도별 기본 제작 비용입니다. 제작법은 다르게 할 값만 덮어씁니다. 등록하지 않은 희귀도의 기본 비용은 없음입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CraftingCostData", menuName = "Project T/데이터/제작 기본 비용")]
    public sealed class CraftingCostData : ProjectData
    {
        #region 필드
        [SerializeField] private CraftingCostEntry[] rarityCosts = Array.Empty<CraftingCostEntry>();

        #endregion // 필드

        #region 조회
        /// <summary>
        /// 희귀도의 기본 비용을 찾습니다. 등록하지 않은 희귀도이면 null입니다.
        /// </summary>
        public CraftingCostEntry Find(SWCategory rarity)
        {
            if (rarity == null)
            {
                return null;
            }

            foreach (CraftingCostEntry entry in rarityCosts)
            {
                if (entry != null && entry.Rarity != null && entry.Rarity.CodeName == rarity.CodeName)
                {
                    return entry;
                }
            }

            return null;
        }

        #endregion // 조회

        #region 검사
        /// <summary>
        /// 희귀도 중복과 각 비용의 참조·수량을 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = true;
            var rarities = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < rarityCosts.Length; index++)
            {
                string path = nameof(rarityCosts) + ".Array.data[" + index + "]";
                CraftingCostEntry entry = rarityCosts[index];
                if (!Check(entry != null && entry.Rarity != null, path, "희귀도를 연결하세요.", issues))
                {
                    valid = false;
                    continue;
                }

                valid &= Check(rarities.Add(entry.Rarity.CodeName), path, "같은 희귀도의 기본 비용이 중복되었습니다.", issues);
                valid &= CheckNonNegative(entry.SoulCost, path + ".soulCost", issues);
                valid &= ValidateMaterials(entry.Materials, path + ".materials", issues);
            }

            return valid;
        }

        /// <summary>
        /// 재료 항목마다 검사하고 같은 재료의 중복을 확인합니다.
        /// </summary>
        private bool ValidateMaterials(IReadOnlyList<CraftingMaterial> materials, string path, List<DataIssue> issues)
        {
            bool valid = true;
            var items = new HashSet<ItemData>();
            for (int index = 0; index < materials.Count; index++)
            {
                string entryPath = path + ".Array.data[" + index + "]";
                CraftingMaterial material = materials[index];
                if (!Check(material != null, entryPath, "빈 재료 항목을 제거하세요.", issues))
                {
                    valid = false;
                    continue;
                }

                valid &= material.Validate(this, entryPath, issues);
                valid &= material.Item == null
                    || Check(items.Add(material.Item), entryPath, "같은 재료가 중복되었습니다. 한 줄로 합쳐 주세요.", issues);
            }

            return valid;
        }

        #endregion // 검사
    }

    /// <summary>
    /// 한 희귀도의 기본 소울·재료 비용입니다. 0은 필요 없음입니다.
    /// </summary>
    [Serializable]
    public sealed class CraftingCostEntry
    {
        #region 필드
        [SerializeField] private SWCategory rarity;
        [SerializeField, Min(0)] private double soulCost;
        [SerializeField] private CraftingMaterial[] materials = Array.Empty<CraftingMaterial>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 비용을 사용할 결과 장비의 희귀도입니다.
        /// </summary>
        public SWCategory Rarity => rarity;

        /// <summary>
        /// 제작 1회의 소울 비용입니다. 0이면 필요 없습니다.
        /// </summary>
        public double SoulCost => soulCost;

        /// <summary>
        /// 제작 1회에 소비할 재료입니다.
        /// </summary>
        public IReadOnlyList<CraftingMaterial> Materials => materials;

        #endregion // 프로퍼티
    }
}
