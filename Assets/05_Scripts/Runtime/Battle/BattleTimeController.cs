using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Util;

namespace ProjectT.Battle
{
    /// <summary>
    /// 한 전투의 배속과 정지 소유권을 관리합니다. 마지막 정지 요청이 해제되면 선택한 배속을 복원합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleTimeController : SWMonoBehaviour
    {
        #region 필드
        private readonly HashSet<PauseLease> leases = new HashSet<PauseLease>();
        private float resumeTimeScale = 1f;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 팝업·수동 정지·결과 화면 중 하나라도 전투를 정지시켰는지 반환합니다.
        /// </summary>
        public bool IsPaused => leases.Count > 0;

        /// <summary>
        /// 정지 상태가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action PauseChanged;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 배율을 변경합니다. 정지 중에는 재개 배율만 바꾸며 잘못된 값은 기존 상태를 보존합니다.
        /// </summary>
        public bool TrySetTimeScale(float value)
        {
            if (!value.ExIsPositive())
            {
                SWLog.LogWarning("[BattleTimeController] 배속 변경 실패: 양수 배율이 필요합니다.");
                return false;
            }

            resumeTimeScale = value;
            if (!IsPaused)
            {
                Time.timeScale = value;
            }

            return true;
        }

        /// <summary>
        /// 독립된 정지 소유권을 얻습니다. 반환값을 폐기하면 이 요청의 정지만 해제합니다.
        /// </summary>
        public IDisposable Pause()
        {
            var lease = new PauseLease(this);
            leases.Add(lease);
            Time.timeScale = 0f;
            if (leases.Count == 1)
            {
                PauseChanged?.Invoke();
            }

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

            Time.timeScale = resumeTimeScale;
            PauseChanged?.Invoke();
        }

        /// <summary>
        /// 장면 종료 때 남은 정지 소유권을 정리하고 기본 배속을 복원합니다.
        /// </summary>
        private void OnDestroy()
        {
            leases.Clear();
            Time.timeScale = 1f;
        }

        #endregion // 함수

        #region 정지 소유권
        /// <summary>
        /// 하나의 정지 요청을 소유하며 해제 시 관리자에 반환합니다.
        /// </summary>
        private sealed class PauseLease : IDisposable
        {
            #region 필드
            private BattleTimeController owner;

            #endregion // 필드

            #region 함수
            /// <summary>
            /// 정지를 요청한 관리자를 보관합니다.
            /// </summary>
            public PauseLease(BattleTimeController owner)
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

        #endregion // 정지 소유권
    }
}
