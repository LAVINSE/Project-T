using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;

namespace ProjectT.Data
{
    /// <summary>
    /// 공방·아군·적의 체력바 배경과 체력 비율별 채움 색상을 공유합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorData", menuName = "Project T/Common/Color Data")]
    public sealed class ColorData : ProjectData
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

        #region 조회
        /// <summary>
        /// 체력바 종류의 배경색을 반환합니다.
        /// </summary>
        public Color GetHealthBackground(HealthBarType type)
        {
            switch (type)
            {
                case HealthBarType.Workshop:
                    return arcaneHpBackgroundColor;
                case HealthBarType.Character:
                    return characterHpBackgroundColor;
                default:
                    return enemyHpBackgroundColor;
            }
        }

        /// <summary>
        /// 체력바 종류의 남은 체력 비율별 채움 색상을 반환합니다.
        /// </summary>
        public Gradient GetHealthFill(HealthBarType type)
        {
            switch (type)
            {
                case HealthBarType.Workshop:
                    return arcaneFillColorGradient;
                case HealthBarType.Character:
                    return characterFillColorGradient;
                default:
                    return enemyFillColorGradient;
            }
        }

        /// <summary>
        /// 모든 채움 색상이 연결되었는지 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = Check(arcaneFillColorGradient != null, nameof(arcaneFillColorGradient), "채움 색상을 지정하세요.", issues);
            valid &= Check(characterFillColorGradient != null, nameof(characterFillColorGradient), "채움 색상을 지정하세요.", issues);
            valid &= Check(enemyFillColorGradient != null, nameof(enemyFillColorGradient), "채움 색상을 지정하세요.", issues);
            return valid;
        }

        #endregion // 조회
    }
}
