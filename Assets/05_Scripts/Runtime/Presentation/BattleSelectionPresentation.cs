using UnityEngine;

using SW.Base;

using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 선택한 아군의 실제 공격 범위와 아직 도착하지 않은 명령 목적지를 표시합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattleSelectionPresentation")]
    public sealed class BattleSelectionPresentation : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleMouseCommand commands;
        [SerializeField] private BattlePlacementCommand placement;
        [SerializeField] private LineRenderer attackRange;
        [SerializeField] private LineRenderer destinationRing;
        [SerializeField] private LineRenderer destinationCross;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 선택한 아군의 공격 범위와 남은 이동 목적지를 표시합니다.
        /// </summary>
        private void LateUpdate()
        {
            AllyUnit selected = commands.SelectedUnit;
            bool visible = selected != null && !placement.IsPlacing;
            attackRange.enabled = visible;
            bool moving = visible
                && (selected.Movement.IsMoving
                || (!selected.Health.IsAlive
                && Vector2.Distance(selected.transform.position, selected.RequestedDestination) > 0.05f));
            destinationRing.enabled = destinationCross.enabled = moving;
            if (!visible)
            {
                return;
            }

            Color rangeColor = selected.CanFight ? new Color(1f, 0.86f, 0.4f, 0.8f) : new Color(0.7f, 0.75f, 0.8f, 0.6f);
            attackRange.startColor = attackRange.endColor = rangeColor;
            BattleIndicatorGeometry.DrawCircle(attackRange, selected.transform.position, selected.Definition.AttackRange);
            if (!moving)
            {
                return;
            }

            Vector3 destination = selected.RequestedDestination;
            BattleIndicatorGeometry.DrawCircle(destinationRing, destination, 0.48f);
            destinationCross.SetPosition(0, destination + Vector3.left * 0.72f);
            destinationCross.SetPosition(1, destination + Vector3.right * 0.72f);
            destinationCross.SetPosition(2, destination);
            destinationCross.SetPosition(3, destination + Vector3.up * 0.72f);
            destinationCross.SetPosition(4, destination + Vector3.down * 0.72f);
        }

        #endregion // 함수
    }
}
