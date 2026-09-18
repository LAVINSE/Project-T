using UnityEngine;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Data;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 공방 프리팹의 피격·선택지 알림·파손 상태를 애니메이션과 전용 체력바에 연결합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "WorkshopObjectivePresentation")]
    public sealed class WorkshopObjectivePresentation : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleSession session;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer workshopSprite;
        [SerializeField] private HealthBarPresentation healthBar;
        private float previousHealth = -1f;
        private bool choicesAvailable;
        private static readonly int HitParameter = Animator.StringToHash("Hit");
        private static readonly int AlertParameter = Animator.StringToHash("Alert");

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 플레이어에게 처리할 공방 선택지가 제공된 상태입니다. 체력 비율과 무관합니다.
        /// </summary>
        public bool ChoicesAvailable => choicesAvailable;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 선택지 제공 기능이 알림을 켜고 선택 완료·취소 시 끕니다. 발생 규칙은 선택지 기능이 소유합니다.
        /// </summary>
        public void SetChoicesAvailable(bool available)
        {
            choicesAvailable = available;
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool(
                    AlertParameter,
                    available && (session == null || session.Workshop == null || session.Workshop.Health.IsAlive));
            }
        }

        /// <summary>
        /// 공방 체력의 변화와 선택지 알림을 애니메이션·체력바에 반영합니다.
        /// </summary>
        private void Update()
        {
            if (session == null || session.Workshop == null)
            {
                return;
            }

            float currentHealth = session.Workshop.Health.Current;
            if (previousHealth < 0)
            {
                bool bound = healthBar.Bind(
                    session.Workshop.Health,
                    DataManager.Instance.ColorData,
                    HealthBarPresentation.OwnerKind.ArcaneWorkshop,
                    false);
                healthBar.gameObject.SetActive(bound);
            }
            else if (currentHealth < previousHealth && currentHealth > 0)
            {
                animator.SetTrigger(HitParameter);
            }

            previousHealth = currentHealth;
            if (!session.Workshop.Health.IsAlive)
            {
                choicesAvailable = false;
                animator.SetBool(AlertParameter, false);
                animator.enabled = false;
                workshopSprite.color = new Color(0.42f, 0.43f, 0.46f);
                return;
            }

            animator.SetBool(AlertParameter, choicesAvailable);
        }

        #endregion // 함수
    }
}
