using UnityEngine;

using SW.Util;

using ProjectT.Data;

namespace ProjectT.Inventory
{
    /// <summary>
    /// Main에서 영구 인벤토리를 준비하고 장면 전환에도 유지합니다. 보유 수량 규칙과 파일 처리는 별도 모듈에 위임합니다.
    /// </summary>
    public sealed class InventoryManager : SWSingleton<InventoryManager>
    {
        #region 필드
        [SerializeField] private ItemCatalogData catalog;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 저장을 불러온 영구 보관소입니다. 초기화 실패 시 null입니다.
        /// </summary>
        public InventoryStore Store { get; private set; }

        /// <summary>
        /// 초기화 실패 원인입니다. 정상 상태에서는 빈 문자열입니다.
        /// </summary>
        public string InitializationError { get; private set; } = string.Empty;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 중복 관리자를 제외하고 영구 보관소를 준비합니다. 불러오기 실패 시 보관소는 null이며 원인을 유지합니다.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            Store = InventoryStore.Create(catalog, new InventorySaveStore(), out string reason);
            InitializationError = reason;
        }

        #endregion // 초기화
    }
}
