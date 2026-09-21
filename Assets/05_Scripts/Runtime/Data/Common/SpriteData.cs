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
    public sealed class SpriteData : SWScriptableObject
    {
        #region 필드
        [SWGroup("배속")]
        [SerializeField] private Sprite speedIcon1;
        [SerializeField] private Sprite speedIcon2;
        [SerializeField] private Sprite speedIcon3;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 지원하는 세 배속의 아이콘이 모두 연결되어 있는지 반환합니다.
        /// </summary>
        public bool HasSpeedIcons => speedIcon1 != null && speedIcon2 != null && speedIcon3 != null;

        #endregion // 프로퍼티

        #region 조회
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

        #endregion // 조회
    }
}
