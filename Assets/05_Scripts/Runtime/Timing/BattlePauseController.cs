using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Util;

namespace ProjectT.Timing
{
    /// <summary>
    /// 한 전투의 모든 팝업이 공유하는 정지 소유권을 관리하고 마지막 팝업이 닫히면 이전 시간 배율을 복원합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Timing", "ProjectT.Defense.Runtime", "BattlePauseController")]
    public sealed class BattlePauseController : SWMonoBehaviour
    {
        #region 필드
        private readonly HashSet<PauseLease> leases = new HashSet<PauseLease>();
        private float previousTimeScale;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 팝업 또는 업그레이드 화면이 전투를 정지시켰는지 반환합니다.
        /// </summary>
        public bool IsPaused => leases.Count > 0;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 독립된 정지 소유권을 얻습니다. 반환값을 폐기하면 이 요청의 정지만 해제합니다.
        /// </summary>
        public IDisposable Pause()
        {
            if (!isActiveAndEnabled)
            {
                SWLog.LogWarning("[BattlePauseController] 정지 실패: 활성 전투의 정지 관리자만 사용할 수 있습니다.");
                return null;
            }

            if (leases.Count == 0)
            {
                previousTimeScale = Time.timeScale;
            }

            var lease = new PauseLease(this);
            leases.Add(lease);
            Time.timeScale = 0f;
            return lease;
        }

        /// <summary>
        /// 정지 소유권을 해제하고 마지막 요청이 끝나면 시간을 복원합니다.
        /// </summary>
        private void Release(PauseLease lease)
        {
            if (!leases.Remove(lease) || leases.Count > 0)
            {
                return;
            }

            Time.timeScale = previousTimeScale;
        }

        /// <summary>
        /// 장면 종료나 관리자 비활성화 때 남은 정지 소유권을 정리하고 시간을 복원합니다.
        /// </summary>
        private void OnDisable()
        {
            if (leases.Count == 0)
            {
                return;
            }

            leases.Clear();
            Time.timeScale = previousTimeScale;
        }

        /// <summary>
        /// 하나의 정지 요청을 소유하며 해제 시 관리자에 반환합니다.
        /// </summary>
        private sealed class PauseLease : IDisposable
        {
            #region 필드
            private BattlePauseController owner;

            #endregion // 필드

            #region 함수
            /// <summary>
            /// 하나의 정지 요청을 소유하며 해제 시 관리자에 반환합니다.
            /// </summary>
            public PauseLease(BattlePauseController owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// 정지 요청을 한 번만 반환하고 관리자 참조를 비웁니다.
            /// </summary>
            public void Dispose()
            {
                if (owner != null)
                {
                    owner.Release(this);
                }

                owner = null;
            }

            #endregion // 함수
        }

        #endregion // 함수
    }
}
