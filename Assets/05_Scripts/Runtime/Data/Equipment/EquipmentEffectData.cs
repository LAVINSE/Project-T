namespace ProjectT.Data
{
    /// <summary>
    /// 장비 효과 설정의 공통 정의입니다. 실제 장착 상태와 전투 적용은 별도 모듈에서 관리합니다.
    /// </summary>
    public abstract class EquipmentEffectData : ProjectData
    {
        #region 프로퍼티
        /// <summary>
        /// 편집 중 효과의 설명입니다. 유효하지 않은 설정은 Validate로 확인하며 이 설명만으로 적용하지 않습니다.
        /// </summary>
        public abstract string EffectDescription { get; }

        #endregion // 프로퍼티
    }
}
