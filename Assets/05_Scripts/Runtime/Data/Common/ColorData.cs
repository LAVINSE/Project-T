using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectT.Data
{
    /// <summary>
    /// 공방·아군·적의 체력바 배경과 체력 비율별 채움 색상을 공유합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorData", menuName = "Project T/Common/Color Data")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Data", "ProjectT.Defense.Runtime", "ColorData")]
    public class ColorData : SWScriptableObject
    {
        #region 필드
        [SWGroup("ArcaneWorkshop")]
        [SerializeField] private Color arcaneHpBackgroundColor;
        [SerializeField] private Gradient arcaneFillColorGradient;
        [SWGroup("Character")]
        [SerializeField] private Color characterHpBackgroundColor;
        [SerializeField] private Gradient characterFillColorGradient;
        [SWGroup("Enemy")]
        [SerializeField] private Color enemyHpBackgroundColor;
        [SerializeField] private Gradient enemyFillColorGradient;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 공방 전용 체력바의 배경색입니다.
        /// </summary>
        public Color ArcaneHpBackgroundColor => arcaneHpBackgroundColor;

        /// <summary>
        /// 남은 공방 체력 비율에 따른 채움 색상입니다.
        /// </summary>
        public Gradient ArcaneFillColorGradient => arcaneFillColorGradient;

        /// <summary>
        /// 아군 공통 체력바의 배경색입니다.
        /// </summary>
        public Color CharacterHpBackgroundColor => characterHpBackgroundColor;

        /// <summary>
        /// 남은 아군 체력 비율에 따른 채움 색상입니다.
        /// </summary>
        public Gradient CharacterFillColorGradient => characterFillColorGradient;

        /// <summary>
        /// 적 공통 체력바의 배경색입니다.
        /// </summary>
        public Color EnemyHpBackgroundColor => enemyHpBackgroundColor;

        /// <summary>
        /// 남은 적 체력 비율에 따른 채움 색상입니다.
        /// </summary>
        public Gradient EnemyFillColorGradient => enemyFillColorGradient;

        #endregion // 프로퍼티
    }
}
