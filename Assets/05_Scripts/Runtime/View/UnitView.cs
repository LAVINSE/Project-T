using System.Collections;
using UnityEngine;

using SW.Base;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.View
{
    /// <summary>
    /// 유닛의 방향·그리기 순서·체력바·선택 표시를 상태 알림이 올 때만 갱신합니다.
    /// </summary>
    [RequireComponent(typeof(UnitBase))]
    public sealed class UnitView : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private LineRenderer selectionRing;
        [SerializeField] private LineRenderer newUnitArrow;
        private UnitBase unit;
        private Coroutine arrowRoutine;
        private Vector3 previousPosition;
        private bool isSelected;
        private bool isAwaitingSelection;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 같은 객체의 유닛을 연결합니다.
        /// </summary>
        private void Awake()
        {
            unit = GetComponent<UnitBase>();
        }

        /// <summary>
        /// 풀에서 꺼낼 때 선택 표시를 초기화하고 유닛 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            isSelected = false;
            isAwaitingSelection = false;
            selectionRing.enabled = false;
            newUnitArrow.enabled = false;
            unit.Moved += OnMoved;
            unit.HealthChanged += OnHealthChanged;
            unit.Attack.Started += OnAttackStarted;
        }

        /// <summary>
        /// 풀에 반환될 때 구독과 강조 표시를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            unit.Moved -= OnMoved;
            unit.HealthChanged -= OnHealthChanged;
            unit.Attack.Started -= OnAttackStarted;
            StopArrow();
        }

        /// <summary>
        /// 유닛 초기화 직후 배치 담당이 호출합니다. 새 생명의 표시 위치·체력바·색상·그리기 순서를 적용합니다.
        /// </summary>
        public void Initialize(ColorData colors)
        {
            UnitData data = unit.Data;
            characterRenderer.transform.localPosition = Vector3.up * data.FeetOffset;
            healthBar.transform.localPosition = Vector3.up * data.HealthBarHeight;
            HealthBarType type = unit is CharacterUnit ? HealthBarType.Character : HealthBarType.Enemy;
            healthBar.gameObject.SetActive(healthBar.Bind(unit.Health, colors, type, true));
            previousPosition = transform.position;
            ApplyTint();
            ApplySortingOrder();
        }

        #endregion // 초기화

        #region 선택 표시
        /// <summary>
        /// 선택 원을 켜거나 끕니다.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            selectionRing.enabled = selected;
            if (selected)
            {
                selectionRing.ExDrawEllipse(transform.position, ProjectDefine.Battle.SelectionRingRadius);
            }
        }

        /// <summary>
        /// 새로 구매한 아군을 직접 선택할 때까지 화살표로 강조합니다.
        /// </summary>
        public void SetAwaitingSelection(bool awaiting)
        {
            isAwaitingSelection = awaiting;
            RefreshArrow();
        }

        /// <summary>
        /// 강조가 필요한 생존 아군에게만 화살표 움직임을 재생합니다.
        /// </summary>
        private void RefreshArrow()
        {
            bool visible = isAwaitingSelection && unit.Health != null && unit.Health.IsAlive;
            if (!visible)
            {
                StopArrow();
                return;
            }

            if (arrowRoutine == null)
            {
                newUnitArrow.enabled = true;
                arrowRoutine = StartCoroutine(AnimateArrow());
            }
        }

        /// <summary>
        /// 강조 중인 동안 머리 위 화살표를 위아래로 움직입니다.
        /// </summary>
        private IEnumerator AnimateArrow()
        {
            while (true)
            {
                float height = unit.Data.HealthBarHeight + 0.3f + Mathf.Sin(Time.time * 5f) * 0.08f;
                Vector3 tip = transform.position + Vector3.up * height;
                newUnitArrow.SetPosition(0, tip + new Vector3(-0.22f, 0.3f, 0f));
                newUnitArrow.SetPosition(1, tip);
                newUnitArrow.SetPosition(2, tip + new Vector3(0.22f, 0.3f, 0f));
                newUnitArrow.SetPosition(3, tip);
                newUnitArrow.SetPosition(4, tip + Vector3.up * 0.65f);
                yield return null;
            }
        }

        /// <summary>
        /// 화살표 움직임을 멈추고 숨깁니다.
        /// </summary>
        private void StopArrow()
        {
            if (arrowRoutine != null)
            {
                StopCoroutine(arrowRoutine);
                arrowRoutine = null;
            }

            newUnitArrow.enabled = false;
        }

        #endregion // 선택 표시

        #region 상태 반영
        /// <summary>
        /// 이동 방향으로 그림을 뒤집고 그리기 순서와 선택 원 위치를 갱신합니다.
        /// </summary>
        private void OnMoved()
        {
            bool attacking = unit.Health.IsAlive && unit.Attack.IsPlaying(Time.time);
            float direction = attacking
                ? unit.Attack.TargetPosition.x - transform.position.x
                : transform.position.x - previousPosition.x;
            Face(direction);
            previousPosition = transform.position;
            ApplySortingOrder();
            if (isSelected)
            {
                selectionRing.ExDrawEllipse(transform.position, ProjectDefine.Battle.SelectionRingRadius);
            }
        }

        /// <summary>
        /// 공격을 시작하면 대상 방향을 바라봅니다.
        /// </summary>
        private void OnAttackStarted()
        {
            Face(unit.Attack.TargetPosition.x - transform.position.x);
        }

        /// <summary>
        /// 사망·부활에 맞춰 색상과 강조 표시를 갱신합니다.
        /// </summary>
        private void OnHealthChanged()
        {
            ApplyTint();
            RefreshArrow();
        }

        /// <summary>
        /// 원본 그림 방향을 기준으로 좌우를 뒤집습니다. 방향 변화가 거의 없으면 유지합니다.
        /// </summary>
        private void Face(float direction)
        {
            if (Mathf.Abs(direction) > 0.001f)
            {
                characterRenderer.flipX = unit.Data.SpritesFaceRight ? direction < 0f : direction > 0f;
            }
        }

        /// <summary>
        /// 데이터 색상을 적용하고 사망 중에는 반투명하게 표시합니다.
        /// </summary>
        private void ApplyTint()
        {
            Color tint = unit.Data.Tint;
            if (!unit.Health.IsAlive)
            {
                tint.a = ProjectDefine.Battle.DeadUnitAlpha;
            }

            characterRenderer.color = tint;
        }

        /// <summary>
        /// 발 높이에 맞는 그리기 순서를 적용합니다.
        /// </summary>
        private void ApplySortingOrder()
        {
            characterRenderer.sortingOrder = transform.position.y.ExToSortingOrder() + 1;
        }

        #endregion // 상태 반영
    }
}
