using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Stat;
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

        [SerializeField] private SWStatOverride maximumHealth;
        [SerializeField] private SWStatOverride moveSpeed;
        [SerializeField] private SWStatOverride attackDamage;
        [SerializeField] private SWStatOverride attackRange;
        [SerializeField] private SWStatOverride attackSpeed;
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
        /// 최대 체력 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat MaximumHealthStat => maximumHealth?.Stat;

        /// <summary>
        /// 공격력 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat AttackDamageStat => attackDamage?.Stat;

        /// <summary>
        /// 공격 속도 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat AttackSpeedStat => attackSpeed?.Stat;

        /// <summary>
        /// 이동속도 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat MoveSpeedStat => moveSpeed?.Stat;

        /// <summary>
        /// 사거리 원본 스탯입니다. 미연결이면 null입니다.
        /// </summary>
        public SWStat AttackRangeStat => attackRange?.Stat;

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
        public float MaximumHealth => maximumHealth.ExGetConfiguredValue();

        /// <summary>
        /// 초당 이동 거리입니다.
        /// </summary>
        public float MoveSpeed => moveSpeed.ExGetConfiguredValue();

        /// <summary>
        /// 한 번의 타격 피해입니다.
        /// </summary>
        public float AttackDamage => attackDamage.ExGetConfiguredValue();

        /// <summary>
        /// 공격 가능한 월드 거리입니다.
        /// </summary>
        public float AttackRange => attackRange.ExGetConfiguredValue();

        /// <summary>
        /// 게임 시간 1초당 공격 횟수입니다.
        /// </summary>
        public float AttackSpeed => attackSpeed.ExGetConfiguredValue();

        /// <summary>
        /// 공격 속도를 내부 실행 주기로 변환합니다. 잘못된 속도는 0을 반환합니다.
        /// </summary>
        public float AttackInterval => AttackSpeed.ExIsPositive() ? 1f / AttackSpeed : 0f;

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
            var definitions = new HashSet<SWStat>();
            var identifiers = new HashSet<int>();
            foreach (SWStatOverride setting in GetStatSettings())
            {
                if (setting?.Stat != null)
                {
                    valid &= Check(definitions.Add(setting.Stat) && (setting.Stat.ID == 0 || identifiers.Add(setting.Stat.ID)),
                        nameof(maximumHealth), "한 유닛의 서로 다른 능력치에 같은 스탯 또는 식별 번호를 중복 연결할 수 없습니다.", issues);
                }
            }

            valid &= CheckPositive(MaximumHealth, nameof(maximumHealth), issues);
            valid &= CheckPositive(MoveSpeed, nameof(moveSpeed), issues);
            valid &= CheckPositive(AttackDamage, nameof(attackDamage), issues);
            valid &= CheckPositive(AttackRange, nameof(attackRange), issues);
            valid &= CheckPositive(AttackSpeed, nameof(attackSpeed), issues);
            valid &= Check(AttackInterval.ExIsPositive(), nameof(attackSpeed), "공격 주기로 변환할 수 있는 양수를 입력하세요.", issues);
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
            valid &= CheckStat(maximumHealth, nameof(maximumHealth), issues);
            valid &= CheckStat(attackDamage, nameof(attackDamage), issues);
            valid &= CheckStat(attackSpeed, nameof(attackSpeed), issues);
            valid &= CheckStat(moveSpeed, nameof(moveSpeed), issues);
            valid &= CheckStat(attackRange, nameof(attackRange), issues);
            return valid;
        }

        /// <summary>
        /// 프리팹에 데이터 종류와 맞는 유닛 컴포넌트가 있는지 확인합니다.
        /// </summary>
        protected abstract bool HasUnitComponent(GameObject unitPrefab);

        #endregion // 함수

        #region 스탯 정의
        /// <summary>
        /// 개체별 런타임 스탯으로 복제할 설정을 열거합니다. 참조 유효성은 Validate에서 검사합니다.
        /// </summary>
        public virtual IEnumerable<SWStatOverride> GetStatSettings()
        {
            yield return maximumHealth;
            yield return attackDamage;
            yield return attackSpeed;
            yield return moveSpeed;
            yield return attackRange;
        }

        #endregion // 스탯 정의
    }
}
