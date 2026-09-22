namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 재화 자산의 생성·복제·기본 수량 편집을 담당하는 독립 창입니다.
    /// </summary>
    public sealed class CurrencyDataEditorWindow : ProjectDataEditorWindow
    {
        #region 프로퍼티
        /// <summary>
        /// 목록과 생성에 사용할 데이터 종류입니다.
        /// </summary>
        protected override int DataKind => 4;

        #endregion // 프로퍼티
    }
}
