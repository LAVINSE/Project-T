using System;
using UnityEngine;

using SW.Attributes;
using SW.Util;

namespace ProjectT.Data
{
    /// <summary>
    /// 색상처럼 모든 장면에서 공유하는 읽기 전용 설정을 제공합니다. 전투별 상태와 스테이지 자산은 보관하지 않습니다.
    /// </summary>
    public sealed class DataManager : SWSingleton<DataManager>
    {
        #region 필드
        [SWGroup("공통 설정")]
        [SerializeField] private ColorData colorData;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 모든 체력바가 공유하는 원본 색상 데이터입니다. 실행 중 원본 값을 변경하지 않습니다.
        /// </summary>
        public ColorData ColorData => colorData;

        #endregion // 프로퍼티

        #region 초기화
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            if (colorData == null)
            {
                SWLog.LogWarning("[DataManager] 초기화 실패: ColorData 참조가 필요합니다.");
            }
        }

        #endregion // 초기화
    }
}
