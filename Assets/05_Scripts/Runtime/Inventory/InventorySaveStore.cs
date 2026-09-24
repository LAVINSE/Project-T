namespace ProjectT.Inventory
{
    /// <summary>
    /// SWUtils 로컬 저장으로 영구 아이템 수량을 읽고 기록합니다. 소울 저장 슬롯과 기존 파일은 변경하지 않습니다.
    /// </summary>
    public sealed class InventorySaveStore : ProjectSaveStore<InventorySaveData>
    {
        #region 프로퍼티
        /// <summary>
        /// 영구 아이템 수량과 지급 기록을 보관하는 저장 슬롯입니다.
        /// </summary>
        protected override string Slot => ProjectDefine.Save.InventorySlot;

        /// <summary>
        /// 아이템 저장을 읽지 못했을 때 전달할 사유입니다.
        /// </summary>
        protected override string LoadFailureReason => "아이템 저장을 불러오지 못했습니다. 기존 파일을 보존합니다.";

        #endregion // 프로퍼티

        #region 확장
        /// <summary>
        /// 저장이 없는 사용자의 빈 인벤토리 상태를 만듭니다.
        /// </summary>
        protected override InventorySaveData CreateEmpty()
        {
            return InventorySaveData.CreateEmpty();
        }

        #endregion // 확장
    }
}
