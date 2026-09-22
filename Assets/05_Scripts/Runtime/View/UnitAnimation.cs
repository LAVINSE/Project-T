using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectT.Units;

namespace ProjectT.View
{
    /// <summary>
    /// 유닛 상태 알림을 캐릭터의 Animator에 반영하고 공격 클립의 타격 이벤트를 전투에 전달합니다.
    /// 동작은 SWCategory 자산이며 목록에 Idle·Walk·MeleeAttack·Death가 항상 있고, 유닛마다 추가 동작을 등록할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class UnitAnimation : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 동작 하나와 Animator Controller 상태의 연결입니다. 클립은 편집기에서 상태를 읽어 자동으로 채웁니다.
        /// </summary>
        [Serializable]
        public sealed class ActionState
        {
            #region 필드
            [SerializeField, Tooltip("동작 SWCategory 자산입니다.")] private SWCategory action;
            [SerializeField, Tooltip("Animator Controller의 상태 이름입니다. 비우면 동작 이름을 사용합니다.")] private string stateName;
            [SerializeField, SWReadOnly, Tooltip("상태에 연결된 클립입니다. 편집기에서 자동으로 채웁니다.")] private AnimationClip clip;

            #endregion // 필드

            #region 프로퍼티
            /// <summary>
            /// 연결할 동작입니다.
            /// </summary>
            public SWCategory Action => action;

            /// <summary>
            /// 재생할 Animator 상태 이름입니다. 비어 있으면 동작 코드명입니다.
            /// </summary>
            public string StateName => string.IsNullOrWhiteSpace(stateName) && action != null ? action.CodeName : stateName;

            /// <summary>
            /// 상태의 클립이며 상태를 찾지 못하면 null입니다.
            /// </summary>
            public AnimationClip Clip => clip;

            #endregion // 프로퍼티

            #region 함수
            /// <summary>
            /// 동작 코드명과 같은 상태 이름으로 연결을 만듭니다.
            /// </summary>
            public ActionState(SWCategory action)
            {
                this.action = action;
                stateName = action.CodeName;
            }

            /// <summary>
            /// 편집기에서 읽은 상태 클립을 기록합니다.
            /// </summary>
            internal void SetClip(AnimationClip value)
            {
                clip = value;
            }

            #endregion // 함수
        }

        #endregion // 데이터

        #region 필드
        [SWGroup("동작")]
        [SerializeField, Tooltip("Idle·Walk·MeleeAttack·Death는 고정이며 필요한 동작을 추가합니다.")]
        private List<ActionState> actions = new List<ActionState>();
        private Animator animator;
        private UnitBase unit;
        private Coroutine attackRoutine;
        private float playedAttack = float.NegativeInfinity;
        private bool isAttacking;
        private bool wasAlive;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 같은 객체의 Animator입니다. 프리팹 자산에서도 조회할 수 있습니다.
        /// </summary>
        private Animator Animator => animator != null ? animator : animator = GetComponent<Animator>();

        /// <summary>
        /// 프리팹의 기본 그림입니다. 배치 미리보기와 초상화 대체 그림으로 사용하며 미설정이면 null입니다.
        /// </summary>
        public Sprite IdleSprite => GetComponent<SpriteRenderer>().sprite;

        /// <summary>
        /// 등록된 동작 목록입니다.
        /// </summary>
        public IReadOnlyList<ActionState> Actions => actions;

        /// <summary>
        /// 사망 클립 길이이며 미설정이면 0입니다.
        /// </summary>
        public float DeathDuration => GetLength(ProjectDefine.UnitAction.Death);

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 같은 프리팹의 전투 개체를 찾아 연결합니다.
        /// </summary>
        private void Awake()
        {
            unit = GetComponentInParent<UnitBase>();
        }

        /// <summary>
        /// 풀에서 꺼낼 때 유닛 상태 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (unit == null)
            {
                return;
            }

            unit.Initialized += OnInitialized;
            unit.HealthChanged += OnHealthChanged;
            unit.MovingChanged += ApplyWalk;
            unit.Attack.Started += OnAttackStarted;
            unit.Attack.Canceled += OnAttackCanceled;
        }

        /// <summary>
        /// 풀에 반환될 때 구독과 진행 중인 공격 재생을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (unit == null)
            {
                return;
            }

            unit.Initialized -= OnInitialized;
            unit.HealthChanged -= OnHealthChanged;
            unit.MovingChanged -= ApplyWalk;
            unit.Attack.Started -= OnAttackStarted;
            unit.Attack.Canceled -= OnAttackCanceled;
            StopAttackRoutine();
            isAttacking = false;
        }

        #endregion // 초기화

        #region 재생
        /// <summary>
        /// 새 생명의 대기 동작부터 다시 재생합니다.
        /// </summary>
        private void OnInitialized()
        {
            wasAlive = true;
            PlayFromIdle();
        }

        /// <summary>
        /// 사망하면 사망 동작을, 부활하면 대기 동작을 재생합니다.
        /// </summary>
        private void OnHealthChanged()
        {
            bool alive = unit.Health.IsAlive;
            if (alive == wasAlive)
            {
                return;
            }

            wasAlive = alive;
            if (alive)
            {
                PlayFromIdle();
                return;
            }

            StopAttackRoutine();
            isAttacking = false;
            Animator.speed = 1f;
            Animator.SetBool(ProjectDefine.AnimatorHash.Walk, false);
            Animator.SetBool(ProjectDefine.AnimatorHash.Death, true);
            Play(ProjectDefine.UnitAction.Death);
        }

        /// <summary>
        /// 공격 방식에 맞는 공격 동작을 공격 간격 안에 끝나도록 재생하고, 끝나면 기본 속도로 돌아옵니다.
        /// </summary>
        private void OnAttackStarted()
        {
            UnitAttack attack = unit.Attack;
            string attackAction = unit.Data.AttackAction.CodeName;
            float clipLength = GetLength(attackAction);
            StopAttackRoutine();
            isAttacking = true;
            playedAttack = attack.StartedAt;
            Animator.SetBool(ProjectDefine.AnimatorHash.Walk, false);
            Animator.speed = attack.Duration > 0f && clipLength > 0f ? clipLength / attack.Duration : 1f;
            Play(attackAction);
            attackRoutine = StartCoroutine(FinishAttackAfter(attack.Duration));
        }

        /// <summary>
        /// 취소된 공격 동작을 중단하고 대기 또는 이동 동작으로 돌아옵니다.
        /// </summary>
        private void OnAttackCanceled()
        {
            if (!isAttacking)
            {
                return;
            }

            StopAttackRoutine();
            FinishAttack();
        }

        /// <summary>
        /// 공격 재생 시간이 지나면 공격 동작을 마칩니다. 전투 시간 배율을 따릅니다.
        /// </summary>
        private IEnumerator FinishAttackAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            attackRoutine = null;
            FinishAttack();
        }

        /// <summary>
        /// 공격 상태를 해제하고 생존 중이면 대기 동작으로 돌아옵니다.
        /// </summary>
        private void FinishAttack()
        {
            isAttacking = false;
            Animator.speed = 1f;
            if (unit.Health.IsAlive)
            {
                Play(ProjectDefine.UnitAction.Idle);
                ApplyWalk();
            }
        }

        /// <summary>
        /// 공격 중이 아닐 때 유닛 이동 상태를 걷기 파라미터에 반영합니다.
        /// </summary>
        private void ApplyWalk()
        {
            Animator.SetBool(ProjectDefine.AnimatorHash.Walk, !isAttacking && unit.IsMoving);
        }

        /// <summary>
        /// 애니메이터를 초기화하고 대기 동작부터 재생합니다.
        /// </summary>
        private void PlayFromIdle()
        {
            StopAttackRoutine();
            isAttacking = false;
            playedAttack = float.NegativeInfinity;
            Animator.Rebind();
            Animator.speed = 1f;
            Animator.SetBool(ProjectDefine.AnimatorHash.Death, false);
            Play(ProjectDefine.UnitAction.Idle);
            ApplyWalk();
        }

        /// <summary>
        /// 동작에 연결된 상태를 처음부터 재생합니다. 등록되지 않은 동작은 무시합니다.
        /// </summary>
        private void Play(string actionCode)
        {
            ActionState state = FindState(actionCode);
            if (state != null)
            {
                Animator.Play(state.StateName, 0, 0f);
            }
        }

        /// <summary>
        /// 진행 중인 공격 종료 대기를 중단합니다.
        /// </summary>
        private void StopAttackRoutine()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        /// <summary>
        /// 공격 클립의 AttackImpact 이벤트를 받습니다. 현재 공격의 이벤트만 전달하며 취소·사망·지난 공격은 무시합니다.
        /// </summary>
        public void AttackImpact(AnimationEvent animationEvent)
        {
            if (unit == null
                || unit.Health == null
                || !unit.Health.IsAlive
                || !isAttacking
                || playedAttack != unit.Attack.StartedAt
                || animationEvent == null
                || animationEvent.animatorClipInfo.clip != GetClip(unit.Data.AttackAction))
            {
                return;
            }

            unit.Attack.SignalImpact(playedAttack);
        }

        #endregion // 재생

        #region 조회
        /// <summary>
        /// 고정 동작과 공격 동작이 등록되어 클립이 연결되었고 공격 클립에 타격 이벤트가 있는지 검사합니다. 실패하면 이유를 반환합니다.
        /// </summary>
        public bool TryValidate(SWCategory attackAction, out string reason)
        {
            reason = string.Empty;
            if (Animator.runtimeAnimatorController == null)
            {
                reason = "Animator Controller를 연결하세요.";
                return false;
            }

            var required = new List<string>(ProjectDefine.UnitAction.Fixed) { attackAction.CodeName };
            foreach (string actionCode in required)
            {
                if (GetClip(actionCode) == null)
                {
                    reason = actionCode + " 동작을 등록하고 Animator에 있는 상태 이름을 입력하세요.";
                    return false;
                }
            }

            if (FindImpactTime(GetClip(attackAction)) < 0f)
            {
                reason = attackAction.CodeName + " 클립에 AttackImpact 이벤트를 추가하세요.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 동작의 클립이며 등록되지 않았거나 상태를 찾지 못하면 null입니다.
        /// </summary>
        public AnimationClip GetClip(SWCategory action)
        {
            return action != null ? GetClip(action.CodeName) : null;
        }

        /// <summary>
        /// 코드명에 해당하는 동작의 클립이며 등록되지 않았거나 상태를 찾지 못하면 null입니다.
        /// </summary>
        public AnimationClip GetClip(string actionCode)
        {
            ActionState state = FindState(actionCode);
            return state != null ? state.Clip : null;
        }

        /// <summary>
        /// 동작의 클립 길이이며 미설정이면 0입니다.
        /// </summary>
        public float GetLength(SWCategory action)
        {
            return action != null ? GetLength(action.CodeName) : 0f;
        }

        /// <summary>
        /// 동작 클립의 타격 이벤트 위치를 0부터 1 미만의 비율로 반환하며 미설정이면 0입니다.
        /// </summary>
        public float GetImpactRatio(SWCategory action)
        {
            AnimationClip clip = GetClip(action);
            return clip != null && clip.length > 0f ? Mathf.Max(0f, FindImpactTime(clip)) / clip.length : 0f;
        }

        /// <summary>
        /// 코드명에 해당하는 동작의 클립 길이이며 미설정이면 0입니다.
        /// </summary>
        private float GetLength(string actionCode)
        {
            AnimationClip clip = GetClip(actionCode);
            return clip != null ? clip.length : 0f;
        }

        /// <summary>
        /// 코드명이 같은 동작 연결을 찾으며 없으면 null입니다.
        /// </summary>
        private ActionState FindState(string actionCode)
        {
            foreach (ActionState state in actions)
            {
                if (state != null && state.Action != null && state.Action.CodeName == actionCode)
                {
                    return state;
                }
            }

            return null;
        }

        /// <summary>
        /// 클립의 타격 이벤트 시각을 반환하며 없거나 범위 밖이면 -1입니다.
        /// </summary>
        private static float FindImpactTime(AnimationClip clip)
        {
            if (clip == null)
            {
                return -1f;
            }

            foreach (AnimationEvent animationEvent in clip.events)
            {
                if (animationEvent.functionName == nameof(AttackImpact)
                    && animationEvent.time >= 0f
                    && animationEvent.time < clip.length)
                {
                    return animationEvent.time;
                }
            }

            return -1f;
        }

        #endregion // 조회

#if UNITY_EDITOR
        #region 편집기
        /// <summary>
        /// 고정 동작이 빠지지 않게 채우고 각 동작의 상태 클립을 Animator Controller에서 읽어 기록합니다.
        /// </summary>
        private void OnValidate()
        {
            for (int index = 0; index < ProjectDefine.UnitAction.Fixed.Length; index++)
            {
                string actionCode = ProjectDefine.UnitAction.Fixed[index];
                SWCategory action = FindActionAsset(actionCode);
                if (FindState(actionCode) == null && action != null)
                {
                    actions.Insert(Mathf.Min(index, actions.Count), new ActionState(action));
                }
            }

            var controller = Animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            foreach (ActionState state in actions)
            {
                state?.SetClip(controller != null && state.Action != null ? FindStateClip(controller, state.StateName) : null);
            }
        }

        /// <summary>
        /// 동작 폴더에서 코드명이 같은 SWCategory 자산을 찾습니다. 없으면 null입니다.
        /// </summary>
        private static SWCategory FindActionAsset(string actionCode)
        {
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:SWCategory", new[] { ProjectDefine.UnitAction.Folder }))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SWCategory>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.CodeName == actionCode)
                {
                    return asset;
                }
            }

            return null;
        }

        /// <summary>
        /// 컨트롤러 첫 레이어에서 이름이 같은 상태의 클립을 찾습니다. 없으면 null입니다.
        /// </summary>
        private static AnimationClip FindStateClip(UnityEditor.Animations.AnimatorController controller, string stateName)
        {
            foreach (var childState in controller.layers[0].stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    return childState.state.motion as AnimationClip;
                }
            }

            return null;
        }

        #endregion // 편집기
#endif
    }
}
