using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

namespace ProjectT.Data
{
    /// <summary>
    /// 아이템 자산의 고정 식별자와 실제 정의를 연결합니다. 편집기에서 자산 목록을 갱신하며 빌드에서도 같은 식별자를 사용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalogData", menuName = "Project T/데이터/아이템 목록")]
    public sealed class ItemCatalogData : ProjectData
    {
        #region 필드
        [SerializeField, SWReadOnly] private ItemDefinitionReference[] items = Array.Empty<ItemDefinitionReference>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 자산 이름·경로와 독립적인 식별자 목록입니다. 아이템이 없으면 빈 목록입니다.
        /// </summary>
        public IReadOnlyList<ItemDefinitionReference> Items => items;

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 식별자와 아이템 참조의 누락·중복을 검사합니다. 빈 목록은 정상이며 잘못된 연결은 false입니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = Check(items != null, nameof(items), "아이템 목록이 없습니다.", issues);
            if (!valid)
            {
                return false;
            }

            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            var definitions = new HashSet<ItemData>();
            foreach (ItemDefinitionReference item in items)
            {
                valid &= Check(item != null
                    && Guid.TryParseExact(item.Identifier, "N", out _)
                    && item.Definition != null
                    && identifiers.Add(item.Identifier)
                    && definitions.Add(item.Definition), nameof(items), "아이템 참조 또는 저장 식별자가 누락되거나 중복되었습니다.", issues);
            }

            return valid;
        }

        #endregion // 검사
    }

    /// <summary>
    /// 편집기 자산의 고정 식별자를 실행 중에도 사용할 수 있도록 정의와 함께 직렬화합니다.
    /// </summary>
    [Serializable]
    public sealed class ItemDefinitionReference
    {
        #region 필드
        [SerializeField] private string identifier;
        [SerializeField] private ItemData definition;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// Unity 자산의 고정 식별자입니다. 복제한 자산은 다른 식별자를 갖습니다.
        /// </summary>
        public string Identifier => identifier;

        /// <summary>
        /// 해당 종류의 실제 아이템 정의입니다. 참조가 없으면 목록 검사를 통과하지 못합니다.
        /// </summary>
        public ItemData Definition => definition;

        #endregion // 프로퍼티
    }
}
