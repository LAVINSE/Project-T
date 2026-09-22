using System;
using UnityEngine;

using SW.Util;

namespace ProjectT.Units
{
    /// <summary>
    /// 한 번의 공격 준비·타격·회복을 같은 게임 시각으로 관리합니다. 피해 계산과 그림 출력은 외부에서 처리합니다.
    /// </summary>
    public sealed class UnitAttack
    {
        #region 필드
        private bool active;
        private bool impactSignaled;
        private bool hasImpacted;
        private Health targetHealth;
        private int targetLife;
        private float readyAt = float.NegativeInfinity;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 또는 마지막 공격을 시작한 게임 시각입니다.
        /// </summary>
        public float StartedAt { get; private set; } = float.NegativeInfinity;

        /// <summary>
        /// 공격 애니메이션 한 번의 재생 시간입니다.
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 공격 중 바라볼 대상의 최신 위치입니다.
        /// </summary>
        public Vector2 TargetPosition { get; private set; }

        /// <summary>
        /// 아직 타격을 처리하지 않은 유효한 공격인지 반환합니다.
        /// </summary>
        public bool HasPendingImpact => active && !hasImpacted;

        /// <summary>
        /// 새 공격 동작이 시작될 때 발생합니다.
        /// </summary>
        public event Action Started;

        /// <summary>
        /// 진행 중인 공격이 이동·사망·대상 소실로 취소될 때 발생합니다.
        /// </summary>
        public event Action Canceled;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 취소되지 않은 공격의 준비 또는 회복 동작이 재생 중인지 반환합니다.
        /// </summary>
        public bool IsPlaying(float time)
        {
            return active && time < StartedAt + Duration;
        }

        /// <summary>
        /// 동작과 재사용 대기가 모두 끝난 경우 다음 공격을 허용합니다.
        /// </summary>
        public bool CanStart(float time)
        {
            return !IsPlaying(time) && time >= readyAt;
        }

        /// <summary>
        /// 대상의 현재 생명을 기억하고 공격 시각을 설정합니다. 잘못된 설정이면 시작하지 않습니다.
        /// </summary>
        public bool Begin(
            float time,
            float interval,
            float duration,
            Vector2 targetPosition,
            Health health)
        {
            if (!time.ExIsFinite() || !interval.ExIsPositive() || !duration.ExIsPositive() || health == null)
            {
                SWLog.LogWarning("[UnitAttack] 공격 시작 실패: 시간과 대상 설정이 올바르지 않습니다.");
                return false;
            }

            StartedAt = time;
            Duration = duration;
            readyAt = time + Mathf.Max(interval, duration);
            TargetPosition = targetPosition;
            targetHealth = health;
            targetLife = health.LifeVersion;
            hasImpacted = false;
            impactSignaled = false;
            active = true;
            Started?.Invoke();
            return true;
        }

        /// <summary>
        /// 풀 재사용이나 부활로 교체된 생명에게 이전 공격이 전달되지 않게 검사합니다.
        /// </summary>
        public bool MatchesTarget(Health health)
        {
            return ReferenceEquals(targetHealth, health)
                && health != null
                && health.IsAlive
                && health.LifeVersion == targetLife;
        }

        /// <summary>
        /// 살아 있는 공격 대상의 위치를 따라 바라봅니다.
        /// </summary>
        public void AimAt(Vector2 position)
        {
            TargetPosition = position;
        }

        /// <summary>
        /// 현재 공격의 애니메이션 이벤트를 기록합니다. 취소되었거나 다른 공격이면 거절합니다.
        /// </summary>
        public bool SignalImpact(float startedAt)
        {
            if (!HasPendingImpact || StartedAt != startedAt)
            {
                return false;
            }

            impactSignaled = true;
            return true;
        }

        /// <summary>
        /// 현재 공격의 이벤트를 받은 뒤 한 번만 참을 반환하며 중복 피해를 막습니다.
        /// </summary>
        public bool TryImpact()
        {
            if (!HasPendingImpact || !impactSignaled)
            {
                return false;
            }

            hasImpacted = true;
            return true;
        }

        /// <summary>
        /// 이동·사망·대상 소실 때 동작과 미발생 타격을 취소하되 기존 재사용 대기는 유지합니다.
        /// </summary>
        public void Cancel()
        {
            if (!active)
            {
                return;
            }

            active = false;
            impactSignaled = false;
            Canceled?.Invoke();
        }

        /// <summary>
        /// 새 개체 또는 부활한 생명의 공격 상태를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            active = false;
            impactSignaled = false;
            hasImpacted = false;
            targetHealth = null;
            StartedAt = float.NegativeInfinity;
            readyAt = float.NegativeInfinity;
            Duration = 0f;
        }

        #endregion // 함수
    }
}
