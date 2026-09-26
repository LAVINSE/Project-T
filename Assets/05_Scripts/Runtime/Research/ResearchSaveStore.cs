namespace ProjectT.Research
{
    /// <summary>
    /// SWUtils의 원자적 로컬 저장으로 연구 진행을 읽고 기록합니다. 클라우드와 백업 파일을 만들지 않습니다.
    /// </summary>
    public sealed class ResearchSaveStore : ProjectSaveStore<ResearchSaveData>
    {
        #region 프로퍼티
        /// <summary>
        /// 연구 진행을 보관하는 저장 슬롯입니다.
        /// </summary>
        protected override string Slot => ProjectDefine.Save.ResearchSlot;

        /// <summary>
        /// 연구 저장을 읽지 못했을 때 전달할 사유입니다.
        /// </summary>
        protected override string LoadFailureReason => "연구 저장을 불러오지 못했습니다. 기존 파일을 보존합니다.";

        #endregion // 프로퍼티

        #region 확장
        /// <summary>
        /// 저장이 없는 신규 사용자의 빈 진행입니다.
        /// </summary>
        protected override ResearchSaveData CreateEmpty()
        {
            return new ResearchSaveData(null);
        }

        #endregion // 확장
    }
}
