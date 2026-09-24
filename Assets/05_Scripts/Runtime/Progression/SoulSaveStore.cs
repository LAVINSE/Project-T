namespace ProjectT.Progression
{
    /// <summary>
    /// SWUtils의 원자적 로컬 저장으로 소울 상태를 읽고 기록합니다. 클라우드와 백업 파일을 만들지 않습니다.
    /// </summary>
    public sealed class SoulSaveStore : ProjectSaveStore<SoulSaveData>
    {
        #region 프로퍼티
        /// <summary>
        /// 소울 잔액과 지급 기록을 보관하는 저장 슬롯입니다.
        /// </summary>
        protected override string Slot => ProjectDefine.Save.SoulSlot;

        /// <summary>
        /// 소울 저장을 읽지 못했을 때 전달할 사유입니다.
        /// </summary>
        protected override string LoadFailureReason => "소울 저장을 불러오지 못했습니다. 기존 파일을 보존합니다.";

        #endregion // 프로퍼티

        #region 확장
        /// <summary>
        /// 저장이 없는 신규 사용자의 빈 상태를 만듭니다. 정의의 기본 보상 수량을 시작 잔액으로 사용하지 않습니다.
        /// </summary>
        protected override SoulSaveData CreateEmpty()
        {
            return SoulSaveData.CreateEmpty();
        }

        #endregion // 확장
    }
}
