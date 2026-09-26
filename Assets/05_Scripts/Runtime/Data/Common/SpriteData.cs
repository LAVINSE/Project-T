using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

namespace ProjectT.Data
{
    /// <summary>
    /// 모든 장면에서 사용하는 공통 아이콘을 보관합니다. 실행 중 원본 참조를 변경하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "SpriteData", menuName = "Project T/Common/Sprite Data")]
    public sealed class SpriteData : ProjectData
    {
        #region 필드
        [SWGroup("배속")]
        [SerializeField] private Sprite speedIcon1;
        [SerializeField] private Sprite speedIcon2;
        [SerializeField] private Sprite speedIcon3;

        [SWGroup("정지/시작")]
        [SerializeField] private Sprite pauseIcon;
        [SerializeField] private Sprite playIcon;

        [SWGroup("결과")]
        [SerializeField] private Sprite resultVictorySprite;
        [SerializeField] private Sprite resultLoseSprite;

        [SWGroup("아이콘")]
        [SerializeField] private Sprite swordsIcon;
        [SerializeField] private Sprite swordsBrokenIcon;

        [SWGroup("등급 박스")]
        [SerializeField] private Sprite commonBoxSprite;
        [SerializeField] private GradeBox[] gradeBoxes = Array.Empty<GradeBox>();
        #endregion // 필드

        #region 조회
        /// <summary>
        /// 희귀도 분류의 박스를 반환합니다. 미지정·미등록 분류는 일반 박스입니다.
        /// </summary>
        public Sprite GetGradeBox(SWCategory category)
        {
            foreach (GradeBox entry in gradeBoxes)
            {
                if (entry.Category != null && category != null && entry.Category.CodeName == category.CodeName)
                {
                    return entry.Sprite;
                }
            }

            return commonBoxSprite;
        }

        /// <summary>
        /// 현재 배속의 아이콘을 반환합니다. 미연결 아이콘이나 지원하지 않는 배속은 null입니다.
        /// </summary>
        public Sprite GetSpeedIcon(int speedMultiplier)
        {
            switch (speedMultiplier)
            {
                case 1:
                    return speedIcon1;
                case 2:
                    return speedIcon2;
                case 3:
                    return speedIcon3;
                default:
                    SWLog.LogWarning("[SpriteData] 아이콘 조회 실패: 지원하지 않는 배속입니다. " + speedMultiplier);
                    return null;
            }
        }

        /// <summary>
        /// 정지 상태에 맞는 조작 아이콘을 반환합니다. 미설정이면 null입니다.
        /// </summary>
        public Sprite GetPauseAndPlayIcon(bool pause)
        {
            return pause ? pauseIcon : playIcon;
        }

        /// <summary>
        /// 승패에 맞는 결과 배경을 반환합니다. 미설정이면 null입니다.
        /// </summary>
        public Sprite GetResultSprite(bool isWin)
        {
            return isWin ? resultVictorySprite : resultLoseSprite;
        }
        
        /// <summary>
        /// 승패에 맞는 검 장식을 반환합니다. 미설정이면 null입니다.
        /// </summary>
        public Sprite GetSwordsIcon(bool isWin)
        {
            return isWin ? swordsIcon : swordsBrokenIcon;
        }

        /// <summary>
        /// 지원하는 세 배속의 아이콘이 모두 연결되었는지 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckRequired(speedIcon1, nameof(speedIcon1), issues);
            valid &= CheckRequired(speedIcon2, nameof(speedIcon2), issues);
            valid &= CheckRequired(speedIcon3, nameof(speedIcon3), issues);
            return valid;
        }
        #endregion // 조회

        #region 등급 표시
        /// <summary>
        /// 사용자 분류와 슬롯 박스 스프라이트를 연결합니다.
        /// </summary>
        [Serializable]
        private sealed class GradeBox
        {
            [SerializeField] private SWCategory category;
            [SerializeField] private Sprite sprite;

            /// <summary>표시 대상 분류입니다.</summary>
            public SWCategory Category => category;
            /// <summary>사용자가 지정한 박스 스프라이트입니다.</summary>
            public Sprite Sprite => sprite;
        }

        #endregion // 등급 표시
    }
}
