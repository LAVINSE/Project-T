using UnityEngine;

using SW.Base;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 이동·공격·사망 프레임과 체력, 선택 표시를 게임 규칙과 분리해 갱신합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "UnitPresentation")]
    public sealed class UnitPresentation : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private HealthBarPresentation healthBar;
        [SerializeField] private LineRenderer selectionRing;
        [SerializeField] private LineRenderer newUnitArrow;
        private CharacterUnit ally;
        private EnemyUnit enemy;
        private Vector3 previousPosition;
        private bool selected;
        private bool awaitingSelection;
        private CombatHealth displayedHealth;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 선택 표시를 켜거나 끕니다.
        /// </summary>
        public void SetSelected(bool value)
        {
            selected = value;
        }

        /// <summary>
        /// 새로 구매한 아군을 직접 선택할 때까지 화살표로 강조합니다.
        /// </summary>
        public void SetAwaitingSelection(bool value)
        {
            awaitingSelection = value;
        }

        /// <summary>
        /// 같은 객체에 있는 아군 또는 적의 수명 컴포넌트를 캐시합니다.
        /// </summary>
        private void Awake()
        {
            ally = GetComponent<CharacterUnit>();
            enemy = GetComponent<EnemyUnit>();
        }

        /// <summary>
        /// 풀에서 다시 꺼낸 유닛의 애니메이션과 선택 표시를 초기화합니다.
        /// </summary>
        private void OnEnable()
        {
            previousPosition = transform.position;
            selected = false;
            awaitingSelection = false;
        }

        /// <summary>
        /// 현재 이동·공격·생존 상태를 스프라이트와 체력바에 반영합니다.
        /// </summary>
        private void LateUpdate()
        {
            CombatHealth health = ally != null ? ally.Health : enemy != null ? enemy.Health : null;
            if (health == null)
            {
                return;
            }

            UnitData appearance = ally != null ? ally.Definition : enemy.Definition;
            UnitAttackSequence attack = ally != null ? ally.Attack : enemy.Attack;
            bool attacking = health.IsAlive && attack.IsPlaying(Time.time);
            characterRenderer.transform.localPosition = Vector3.up * appearance.FeetOffset;
            float direction = attacking ? attack.TargetPosition.x - transform.position.x : transform.position.x - previousPosition.x;
            if (Mathf.Abs(direction) > 0.001f)
            {
                characterRenderer.flipX = appearance.SpritesFaceRight ? direction < 0 : direction > 0;
            }

            previousPosition = transform.position;
            Color tint = appearance.Tint;
            if (!health.IsAlive)
            {
                tint.a = 0.55f;
            }

            characterRenderer.color = tint;
            characterRenderer.sortingOrder = WorldDepthSorting.OrderAt(transform.position.y) + 1;
            if (displayedHealth != health)
            {
                displayedHealth = health;
                bool bound = healthBar.Bind(
                    health,
                    DataManager.Instance.ColorData,
                    ally != null ? HealthBarPresentation.OwnerKind.Character : HealthBarPresentation.OwnerKind.Enemy,
                    true);
                healthBar.gameObject.SetActive(bound);
            }

            healthBar.transform.localPosition = Vector3.up * appearance.HealthBarHeight;
            selectionRing.enabled = selected;
            if (newUnitArrow != null)
            {
                newUnitArrow.enabled = awaitingSelection && health.IsAlive;
                if (newUnitArrow.enabled)
                {
                    Vector3 tip = transform.position + Vector3.up * (appearance.HealthBarHeight + 0.3f + Mathf.Sin(Time.time * 5f) * 0.08f);
                    newUnitArrow.SetPosition(0, tip + new Vector3(-0.22f, 0.3f, 0));
                    newUnitArrow.SetPosition(1, tip);
                    newUnitArrow.SetPosition(2, tip + new Vector3(0.22f, 0.3f, 0));
                    newUnitArrow.SetPosition(3, tip);
                    newUnitArrow.SetPosition(4, tip + Vector3.up * 0.65f);
                }
            }

            if (selected)
            {
                for (int index = 0; index < selectionRing.positionCount; index++)
                {
                    float angle = index * Mathf.PI * 2f / selectionRing.positionCount;
                    selectionRing.SetPosition(index, transform.position + new Vector3(Mathf.Cos(angle) * 0.55f, Mathf.Sin(angle) * 0.23f, 0));
                }
            }
        }

        #endregion // 함수
    }
}
