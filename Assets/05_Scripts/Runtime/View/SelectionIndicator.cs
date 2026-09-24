using UnityEngine;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Units;

namespace ProjectT.View
{
    /// <summary>
    /// 선택한 아군의 실제 공격 범위와 아직 도착하지 않은 명령 목적지를 표시합니다. 선택과 유닛 상태 알림에만 반응합니다.
    /// </summary>
    public sealed class SelectionIndicator : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleInput input;
        [SerializeField] private LineRenderer attackRange;
        [SerializeField] private LineRenderer destinationRing;
        [SerializeField] private LineRenderer destinationCross;
        private CharacterUnit target;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 선택 변경 알림을 구독하고 현재 선택을 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            input.Selection.Changed += OnSelectionChanged;
            input.Placement.Changed += OnSelectionChanged;
            OnSelectionChanged();
        }

        /// <summary>
        /// 선택 변경과 유닛 알림 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            input.Selection.Changed -= OnSelectionChanged;
            input.Placement.Changed -= OnSelectionChanged;
            SetTarget(null);
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 배치 중이 아닐 때만 선택한 아군을 표시 대상으로 삼습니다.
        /// </summary>
        private void OnSelectionChanged()
        {
            SetTarget(input.Placement.IsPlacing ? null : input.Selection.SelectedUnit);
        }

        /// <summary>
        /// 표시 대상을 바꾸고 대상의 이동·체력 알림을 다시 구독합니다.
        /// </summary>
        private void SetTarget(CharacterUnit unit)
        {
            if (target != null)
            {
                target.Moved -= Refresh;
                target.MovingChanged -= Refresh;
                target.HealthChanged -= Refresh;
                target.StatsChanged -= Refresh;
            }

            target = unit;
            if (target != null)
            {
                target.Moved += Refresh;
                target.MovingChanged += Refresh;
                target.HealthChanged += Refresh;
                target.StatsChanged += Refresh;
            }

            Refresh();
        }

        /// <summary>
        /// 대상의 공격 범위와 남은 이동 목적지를 그립니다.
        /// </summary>
        private void Refresh()
        {
            bool visible = target != null;
            Vector2 position = visible ? (Vector2)target.transform.position : Vector2.zero;
            bool hasDestination = visible
                && (target.Movement.IsMoving
                || (!target.Health.IsAlive && Vector2.Distance(position, target.RequestedDestination) > 0.05f));
            attackRange.enabled = visible;
            destinationRing.enabled = hasDestination;
            destinationCross.enabled = hasDestination;
            if (!visible)
            {
                return;
            }

            Color rangeColor = target.CanFight ? ProjectDefine.Palette.ActiveRange : ProjectDefine.Palette.InactiveRange;
            attackRange.startColor = rangeColor;
            attackRange.endColor = rangeColor;
            attackRange.ExDrawCircle(position, target.AttackRange);
            if (!hasDestination)
            {
                return;
            }

            Vector3 destination = target.RequestedDestination;
            float length = ProjectDefine.Battle.DestinationCrossLength;
            destinationRing.ExDrawCircle(destination, ProjectDefine.Battle.DestinationRingRadius);
            destinationCross.SetPosition(0, destination + Vector3.left * length);
            destinationCross.SetPosition(1, destination + Vector3.right * length);
            destinationCross.SetPosition(2, destination);
            destinationCross.SetPosition(3, destination + Vector3.up * length);
            destinationCross.SetPosition(4, destination + Vector3.down * length);
        }

        #endregion // 함수
    }
}
