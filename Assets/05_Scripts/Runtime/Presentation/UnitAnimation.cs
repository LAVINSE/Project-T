using UnityEngine;

using SW.Base;

using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 유닛 상태를 애니메이터에 연결하고 클립 타격 이벤트를 전투 순서에 전달합니다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class UnitAnimation : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Animator animator;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip moveClip;
        [SerializeField] private AnimationClip attackClip;
        [SerializeField] private AnimationClip deathClip;
        [SerializeField] private string idleState;
        [SerializeField] private string attackState;
        [SerializeField] private string deathState;
        private CharacterUnit character;
        private EnemyUnit enemy;
        private CombatHealth currentHealth;
        private int currentLife;
        private float playedAttack = float.NegativeInfinity;
        private bool wasAlive;
        private bool wasAttacking;
        private static readonly int WalkParameter = Animator.StringToHash("Walk");
        private static readonly int DeathParameter = Animator.StringToHash("Death");

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 프리팹과 배치 미리보기의 기본 그림입니다. 미설정이면 null입니다.
        /// </summary>
        public Sprite IdleSprite => idleSprite;

        /// <summary>
        /// 공격 클립 길이이며 미설정이면 0입니다.
        /// </summary>
        public float AttackDuration => attackClip == null ? 0f : attackClip.length;

        /// <summary>
        /// 사망 클립 길이이며 미설정이면 0입니다.
        /// </summary>
        public float DeathDuration => deathClip == null ? 0f : deathClip.length;

        /// <summary>
        /// 컨트롤러·상태·클립·타격 이벤트가 모두 연결되었는지 반환합니다.
        /// </summary>
        public bool IsConfigured => animator != null
            && animator.runtimeAnimatorController != null
            && idleSprite != null
            && idleClip != null
            && moveClip != null
            && attackClip != null
            && deathClip != null
            && !string.IsNullOrWhiteSpace(idleState)
            && !string.IsNullOrWhiteSpace(attackState)
            && !string.IsNullOrWhiteSpace(deathState)
            && FindImpactTime() >= 0f;

        /// <summary>
        /// 실제 클립 타격 이벤트 위치를 0부터 1 미만의 비율로 반환하며 미설정이면 0입니다.
        /// </summary>
        public float AttackImpactRatio => AttackDuration > 0f ? Mathf.Max(0f, FindImpactTime()) / AttackDuration : 0f;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 같은 프리팹의 전투 개체를 찾아 연결합니다.
        /// </summary>
        private void Awake()
        {
            character = GetComponentInParent<CharacterUnit>();
            enemy = GetComponentInParent<EnemyUnit>();
        }

        /// <summary>
        /// 풀에서 다시 생성되면 이전 생명의 애니메이션 연결을 초기화합니다.
        /// </summary>
        private void OnEnable()
        {
            currentHealth = null;
            playedAttack = float.NegativeInfinity;
            wasAttacking = false;
        }

        #endregion // 초기화

        #region 재생
        /// <summary>
        /// 전투 상태와 공격 간격을 반영합니다. 초기화되지 않은 개체는 재생하지 않습니다.
        /// </summary>
        private void Update()
        {
            CombatHealth health = character != null ? character.Health : enemy != null ? enemy.Health : null;
            if (health == null || animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            UnitAttackSequence sequence = character != null ? character.Attack : enemy.Attack;
            if (currentHealth != health || currentLife != health.LifeVersion)
            {
                currentHealth = health;
                currentLife = health.LifeVersion;
                playedAttack = float.NegativeInfinity;
                wasAttacking = false;
                wasAlive = true;
                animator.Rebind();
                animator.speed = 1f;
                animator.Play(idleState, 0, 0f);
            }

            bool alive = health.IsAlive;
            bool attacking = alive && sequence.IsPlaying(Time.time);
            bool moving = alive
                && !attacking
                && (character != null ? character.Movement.IsMoving : !enemy.Movement.IsStopped && enemy.IsActive);
            animator.SetBool(WalkParameter, moving);
            animator.SetBool(DeathParameter, !alive);
            animator.speed = attacking && sequence.Duration > 0f ? AttackDuration / sequence.Duration : 1f;
            if (!alive && wasAlive)
            {
                animator.Play(deathState, 0, 0f);
            }
            else if (attacking && playedAttack != sequence.StartedAt)
            {
                playedAttack = sequence.StartedAt;
                animator.Play(attackState, 0, 0f);
            }
            else if (alive && wasAttacking && !attacking)
            {
                animator.Play(idleState, 0, 0f);
            }

            wasAlive = alive;
            wasAttacking = attacking;
        }

        /// <summary>
        /// 현재 공격 클립의 타격 이벤트만 전달하며 취소·사망·지난 공격 이벤트는 무시합니다.
        /// </summary>
        public void AttackImpact(AnimationEvent animationEvent)
        {
            CombatHealth health = character != null ? character.Health : enemy != null ? enemy.Health : null;
            UnitAttackSequence sequence = character != null ? character.Attack : enemy != null ? enemy.Attack : null;
            if (health == null
                || !health.IsAlive
                || sequence == null
                || !wasAttacking
                || playedAttack != sequence.StartedAt
                || animationEvent == null
                || animationEvent.animatorClipInfo.clip != attackClip)
            {
                return;
            }

            sequence.SignalImpact(playedAttack);
        }

        /// <summary>
        /// 선택 동작의 원본 클립을 반환하며 설정되지 않은 경우 null입니다.
        /// </summary>
        public AnimationClip GetClip(bool alive, bool moving, bool attacking)
        {
            return !alive ? deathClip : attacking ? attackClip : moving ? moveClip : idleClip;
        }

        /// <summary>
        /// 타격 이벤트의 클립 시각을 반환하며 없거나 범위 밖이면 -1입니다.
        /// </summary>
        private float FindImpactTime()
        {
            if (attackClip == null)
            {
                return -1f;
            }

            foreach (AnimationEvent animationEvent in attackClip.events)
            {
                if (animationEvent.functionName == nameof(AttackImpact)
                    && animationEvent.time >= 0f
                    && animationEvent.time < attackClip.length)
                {
                    return animationEvent.time;
                }
            }

            return -1f;
        }

        #endregion // 재생
    }
}
