namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 적의 생성과 전투 능력 편집을 담당하는 독립 창입니다.
    /// </summary>
    public sealed class UnitEnemyDataEditorWindow : ProjectDataEditorWindow
    {
        #region 속성
        /// <summary>
        /// 이 창에서 목록 조회와 생성에 사용하는 데이터 종류입니다.
        /// </summary>
        protected override int DataKind => 1;

        #endregion // 속성
    }
}
