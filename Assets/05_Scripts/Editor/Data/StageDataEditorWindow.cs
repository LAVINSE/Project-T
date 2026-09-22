namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 스테이지의 생성과 라운드 구성 편집을 담당하는 독립 창입니다.
    /// </summary>
    public sealed class StageDataEditorWindow : ProjectDataEditorWindow
    {
        #region 속성
        /// <summary>
        /// 이 창에서 목록 조회와 생성에 사용하는 데이터 종류입니다.
        /// </summary>
        protected override int DataKind => 2;

        #endregion // 속성
    }
}
