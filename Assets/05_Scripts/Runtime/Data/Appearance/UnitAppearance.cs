using System;
using UnityEngine;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.Data
{
    /// <summary>
    /// 전투 수치와 분리된 픽셀 캐릭터의 상태별 프레임을 보관합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/디펜스/캐릭터 외형")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Units", "ProjectT.Defense.Runtime", "UnitAppearance")]
    public sealed class UnitAppearance : SWScriptableObject
    {
        #region 필드
        [SerializeField, Tooltip("구매·선택 화면의 초상화입니다. 비어 있으면 첫 대기 프레임을 사용합니다.")] private Sprite portrait;
        [SerializeField] private Sprite[] idleFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] moveFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] attackFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] deathFrames = Array.Empty<Sprite>();
        [SerializeField] private Color tint = Color.white;
        [SerializeField, Min(1)] private float framesPerSecond = 10f;
        [SerializeField] private float feetOffset;
        [SerializeField] private float healthBarHeight = 2f;
        [SerializeField, Min(0)] private int attackImpactFrame;
        [SerializeField] private bool spritesFaceRight = true;
        [SerializeField] private Vector2 attackOriginOffset = new Vector2(0.5f, 0.8f);

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 화면용 초상화입니다. 미지정이면 첫 대기 프레임을 사용하며 프레임도 없으면 null입니다.
        /// </summary>
        public Sprite Portrait => portrait != null ? portrait : idleFrames.Length > 0 ? idleFrames[0] : null;

        /// <summary>
        /// 상태별 애니메이션 프레임을 반환합니다.
        /// </summary>
        public Sprite[] GetFrames(bool alive, bool moving, bool attacking)
        {
            return !alive && deathFrames.Length > 0
                ? deathFrames
                : attacking && attackFrames.Length > 0
                    ? attackFrames
                    : moving && moveFrames.Length > 0
                        ? moveFrames
                        : idleFrames;
        }

        /// <summary>
        /// 캐릭터의 기본 색상입니다.
        /// </summary>
        public Color Tint => tint;

        /// <summary>
        /// 초당 표시할 프레임 수입니다.
        /// </summary>
        public float FramesPerSecond => framesPerSecond;

        /// <summary>
        /// 그림의 발을 개체 좌표에 맞추기 위한 높이입니다.
        /// </summary>
        public float FeetOffset => feetOffset;

        /// <summary>
        /// 캐릭터 머리 위 체력 표시 높이입니다.
        /// </summary>
        public float HealthBarHeight => healthBarHeight;

        /// <summary>
        /// 원본 그림이 오른쪽을 향하는지 반환합니다.
        /// </summary>
        public bool SpritesFaceRight => spritesFaceRight;

        /// <summary>
        /// 한 번의 공격에서 타격 그림이 시작하는 재생 비율입니다.
        /// </summary>
        public float AttackImpactRatio => attackFrames.Length == 0
            ? 0f
            : (float)Mathf.Clamp(attackImpactFrame, 0, attackFrames.Length - 1) / attackFrames.Length;

        /// <summary>
        /// 공격 간격 안에서 전체 공격 프레임이 끝나도록 재생 시간을 계산합니다.
        /// </summary>
        public float GetAttackDuration(float interval)
        {
            return Mathf.Min(interval, Mathf.Max(1, attackFrames.Length) / framesPerSecond);
        }

        /// <summary>
        /// 오른쪽을 향했을 때 발 위치에서 공격이 출발하는 지점입니다.
        /// </summary>
        public Vector2 AttackOriginOffset => attackOriginOffset;

        #endregion // 함수
    }
}
