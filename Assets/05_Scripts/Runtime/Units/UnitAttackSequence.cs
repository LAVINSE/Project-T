using System;
using UnityEngine;

using SW.Util;

namespace ProjectT.Units
{
    /// <summary>
    /// 한 번의 공격 준비·타격·회복을 같은 게임 시각으로 관리합니다. 피해 계산과 그림 출력은 외부에서 처리합니다.
    /// </summary>
    public sealed class UnitAttackSequence
    {
        #region 필드
        private bool active;
        private CombatHealth targetHealth;
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
        /// 타격 프레임이 시작되는 게임 시각입니다.
        /// </summary>
        public float ImpactAt { get; private set; }

        /// <summary>
        /// 이번 공격의 타격을 이미 처리했는지 반환합니다.
        /// </summary>
        public bool HasImpacted { get; private set; }

        /// <summary>
        /// 공격 중 바라볼 대상의 최신 위치입니다.
        /// </summary>
        public Vector2 TargetPosition { get; private set; }

        /// <summary>
        /// 아직 타격 시점에 도달하지 않은 유효한 공격인지 반환합니다.
        /// </summary>
        public bool HasPendingImpact => active && !HasImpacted;

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
        /// 대상의 현재 생명을 기억하고 공격 시각을 설정합니다.
        /// </summary>
        public bool Begin(
            float time,
            float interval,
            float duration,
            float impactRatio,
            Vector2 targetPosition,
            CombatHealth health)
        {
            if (!Finite(time)
                || !Finite(interval)
                || !Finite(duration)
                || !Finite(impactRatio)
                || interval <= 0f
                || duration <= 0f
                || impactRatio < 0f
                || impactRatio >= 1f
                || health == null)
            {
                SWLog.LogWarning("[UnitAttackSequence] 공격 시작 실패: 시간과 대상 설정이 올바르지 않습니다.");
                return false;
            }

            StartedAt = time;
            Duration = duration;
            ImpactAt = time + duration * impactRatio;
            readyAt = time + Mathf.Max(interval, duration);
            TargetPosition = targetPosition;
            targetHealth = health;
            targetLife = health.LifeVersion;
            HasImpacted = false;
            active = true;
            return true;
        }

        /// <summary>
        /// 풀 재사용이나 부활로 교체된 생명에게 이전 공격이 전달되지 않게 검사합니다.
        /// </summary>
        public bool MatchesTarget(CombatHealth health)
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
        /// 타격 시점부터 한 번만 참을 반환합니다. 정지한 시각의 반복 호출로 피해가 중복되지 않습니다.
        /// </summary>
        public bool TryImpact(float time)
        {
            if (!HasPendingImpact || time < ImpactAt)
            {
                return false;
            }

            HasImpacted = true;
            return true;
        }

        /// <summary>
        /// 이동·사망·대상 소실 때 동작과 미발생 타격을 취소하되 기존 재사용 대기는 유지합니다.
        /// </summary>
        public void Cancel()
        {
            active = false;
        }

        /// <summary>
        /// 새 개체 또는 부활한 생명의 공격 상태를 초기화합니다.
        /// </summary>
        public void Reset()
        {
            active = false;
            targetHealth = null;
            StartedAt = readyAt = float.NegativeInfinity;
            Duration = 0f;
            HasImpacted = false;
        }

        /// <summary>
        /// 공격 시간 설정이 유한한 값인지 확인합니다.
        /// </summary>
        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion // 함수
    }
}
