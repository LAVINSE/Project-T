using UnityEngine;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Data;

namespace ProjectT.View
{
    /// <summary>
    /// 공방 프리팹의 피격·선택지 알림·파손 상태를 애니메이션과 전용 체력바에 연결합니다.
    /// </summary>
    public sealed class WorkshopView : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer workshopSprite;
        [SerializeField] private HealthBar healthBar;
        private Workshop workshop;
        private float previousHealth;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 플레이어에게 처리할 공방 선택지가 제공된 상태입니다. 체력 비율과 무관합니다.
        /// </summary>
        public bool ChoicesAvailable { get; private set; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 전투가 만든 공방 체력을 체력바와 피격 연출에 연결합니다. 전투 관리자가 호출합니다.
        /// </summary>
        public void Initialize(Workshop target, ColorData colors)
        {
            workshop = target;
            previousHealth = workshop.Health.Current;
            workshop.Health.Changed += OnHealthChanged;
            healthBar.gameObject.SetActive(healthBar.Bind(workshop.Health, colors, HealthBarType.Workshop, false));
        }

        /// <summary>
        /// 체력 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (workshop != null)
            {
                workshop.Health.Changed -= OnHealthChanged;
            }
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 선택지 제공 기능이 알림을 켜고 선택 완료·취소 시 끕니다. 파괴된 공방은 알림을 켜지 않습니다.
        /// </summary>
        public void SetChoicesAvailable(bool available)
        {
            bool alive = workshop == null || workshop.Health.IsAlive;
            ChoicesAvailable = available && alive;
            if (animator.isActiveAndEnabled)
            {
                animator.SetBool(ProjectDefine.AnimatorHash.Alert, ChoicesAvailable);
            }
        }

        /// <summary>
        /// 피해를 받으면 피격 연출을, 체력이 0이 되면 파손 상태를 표시합니다.
        /// </summary>
        private void OnHealthChanged()
        {
            float currentHealth = workshop.Health.Current;
            bool damaged = currentHealth < previousHealth;
            previousHealth = currentHealth;
            if (workshop.Health.IsAlive)
            {
                if (damaged)
                {
                    animator.SetTrigger(ProjectDefine.AnimatorHash.Hit);
                }

                return;
            }

            ChoicesAvailable = false;
            animator.SetBool(ProjectDefine.AnimatorHash.Alert, false);
            animator.enabled = false;
            workshopSprite.color = ProjectDefine.Palette.DestroyedWorkshop;
        }

        #endregion // 함수
    }
}
