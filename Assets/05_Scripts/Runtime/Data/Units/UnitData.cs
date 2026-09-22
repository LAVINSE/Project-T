using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;

using ProjectT.View;

namespace ProjectT.Data
{
    /// <summary>
    /// 캐릭터와 적의 공통 능력치·프리팹·표시 정보를 보관합니다. 미설정 데이터는 생성할 수 없습니다.
    /// </summary>
    public abstract class UnitData : ProjectData
    {
        #region 필드
        [SerializeField] private string displayName;
        [SerializeField] private GameObject prefab;
        [SerializeField] private float maximumHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float attackDamage;
        [SerializeField] private float attackRange;
        [SerializeField] private float attackInterval;
        [SerializeField] private SWCategory attackAction;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private float feetOffset;
        [SerializeField] private float healthBarHeight = 2f;
        [SerializeField] private bool spritesFaceRight = true;
        [SerializeField] private Vector2 attackOriginOffset = new Vector2(0.5f, 0.8f);
        [NonSerialized] private GameObject cachedPrefab;
        [NonSerialized] private UnitAnimation cachedAnimation;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 게임 화면에 표시할 이름입니다.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// 생성할 종류별 프리팹입니다. 미지정이면 null입니다.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// 새 생명의 최대 체력입니다.
        /// </summary>
        public float MaximumHealth => maximumHealth;

        /// <summary>
        /// 초당 이동 거리입니다.
        /// </summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>
        /// 한 번의 타격 피해입니다.
        /// </summary>
        public float AttackDamage => attackDamage;

        /// <summary>
        /// 공격 가능한 월드 거리입니다.
        /// </summary>
        public float AttackRange => attackRange;

        /// <summary>
        /// 연속 공격 시작 사이의 게임 시간입니다.
        /// </summary>
        public float AttackInterval => attackInterval;

        /// <summary>
        /// 공격할 때 재생할 동작(SWCategory)입니다. 프리팹의 UnitAnimation 동작 목록에 등록되어 있어야 합니다.
        /// </summary>
        public SWCategory AttackAction => attackAction;

        /// <summary>
        /// 프리팹의 애니메이션 설정입니다. 프리팹 참조가 바뀔 때만 다시 찾으며 없으면 null입니다.
        /// </summary>
        public UnitAnimation Animation
        {
            get
            {
                if (cachedPrefab != prefab || (prefab != null && cachedAnimation == null))
                {
                    cachedPrefab = prefab;
                    cachedAnimation = prefab != null ? prefab.GetComponentInChildren<UnitAnimation>(true) : null;
                }

                return cachedAnimation;
            }
        }

        /// <summary>
        /// 화면용 초상화이며 미지정이면 프리팹의 기본 그림, 둘 다 없으면 null입니다.
        /// </summary>
        public Sprite Portrait => portrait != null ? portrait : PreviewSprite;

        /// <summary>
        /// 배치 미리보기용 기본 그림입니다. 프리팹 설정이 없으면 null입니다.
        /// </summary>
        public Sprite PreviewSprite => Animation != null ? Animation.IdleSprite : null;

        /// <summary>
        /// 원본 그림에 곱하는 색상입니다.
        /// </summary>
        public Color Tint => tint;

        /// <summary>
        /// 발 위치를 맞추는 세로 보정입니다.
        /// </summary>
        public float FeetOffset => feetOffset;

        /// <summary>
        /// 머리 위 체력바 높이입니다.
        /// </summary>
        public float HealthBarHeight => healthBarHeight;

        /// <summary>
        /// 원본 그림이 오른쪽을 향하는지 반환합니다.
        /// </summary>
        public bool SpritesFaceRight => spritesFaceRight;

        /// <summary>
        /// 발 위치 기준 타격 효과 출발점입니다.
        /// </summary>
        public Vector2 AttackOriginOffset => attackOriginOffset;

        /// <summary>
        /// 클립의 실제 타격 이벤트 비율이며 설정이 없으면 0입니다.
        /// </summary>
        public float AttackImpactRatio => Animation != null ? Animation.GetImpactRatio(attackAction) : 0f;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 공격 간격이 짧을 때만 동작을 압축하며 잘못된 설정이면 0을 반환합니다.
        /// </summary>
        public float GetAttackDuration(float interval)
        {
            if (Animation == null || !interval.ExIsPositive())
            {
                return 0f;
            }

            return Mathf.Min(interval, Animation.GetLength(attackAction));
        }

        /// <summary>
        /// 공통 능력치·표시 수치·프리팹 연결을 검사합니다.
        /// </summary>
        public override bool Validate(List<DataIssue> issues)
        {
            bool valid = CheckName(displayName, nameof(displayName), issues);
            valid &= CheckPositive(maximumHealth, nameof(maximumHealth), issues);
            valid &= CheckPositive(moveSpeed, nameof(moveSpeed), issues);
            valid &= CheckPositive(attackDamage, nameof(attackDamage), issues);
            valid &= CheckPositive(attackRange, nameof(attackRange), issues);
            valid &= CheckPositive(attackInterval, nameof(attackInterval), issues);
            valid &= Check(feetOffset.ExIsFinite(), nameof(feetOffset), "유한한 수를 입력하세요.", issues);
            valid &= Check(healthBarHeight.ExIsFinite(), nameof(healthBarHeight), "유한한 수를 입력하세요.", issues);
            valid &= Check(attackOriginOffset.ExIsFinite(), nameof(attackOriginOffset), "좌표는 유한한 수여야 합니다.", issues);
            valid &= CheckRequired(attackAction, nameof(attackAction), issues);
            if (!CheckRequired(prefab, nameof(prefab), issues) || attackAction == null)
            {
                return false;
            }

            string animationReason = "프리팹에 UnitAnimation이 필요합니다.";
            valid &= Check(
                Animation != null && Animation.TryValidate(attackAction, out animationReason),
                nameof(prefab),
                animationReason,
                issues);
            valid &= Check(
                HasUnitComponent(prefab),
                nameof(prefab),
                "데이터 종류에 맞는 캐릭터 또는 적 컴포넌트가 필요합니다.",
                issues);
            return valid;
        }

        /// <summary>
        /// 프리팹에 데이터 종류와 맞는 유닛 컴포넌트가 있는지 확인합니다.
        /// </summary>
        protected abstract bool HasUnitComponent(GameObject unitPrefab);

        #endregion // 함수
    }
}
