using System;
using UnityEngine;

using ProjectT.Units;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 아군 선택과 이동 명령, 새로 구매한 아군 강조를 관리합니다. 화면 좌표 변환과 입력 신호는 BattleInput이 담당합니다.
    /// </summary>
    public sealed class SelectionController
    {
        #region 필드
        private readonly BattleManager battle;
        private readonly Action<string> showMessage;
        private CharacterUnit latestPurchase;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 선택한 아군입니다. 부활 대기 중인 아군도 선택할 수 있습니다.
        /// </summary>
        public CharacterUnit SelectedUnit { get; private set; }

        /// <summary>
        /// 선택한 아군이 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 전투와 안내 문구 출력을 연결합니다.
        /// </summary>
        public SelectionController(BattleManager battle, Action<string> showMessage)
        {
            this.battle = battle;
            this.showMessage = showMessage;
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 아군 한 명을 선택합니다. null이면 선택을 해제합니다.
        /// </summary>
        public void Select(CharacterUnit unit)
        {
            SetSelected(unit);
            showMessage(unit != null
                ? unit.Definition.DisplayName + " 선택 · 오른쪽 클릭으로 이동하세요."
                : "아군을 왼쪽 클릭해 선택하고, 오른쪽 클릭으로 이동하세요.");
        }

        /// <summary>
        /// 클릭 위치의 아군을 선택합니다. 겹치면 방금 구매한 아군, 그다음 가장 가까운 아군을 고릅니다.
        /// </summary>
        public void SelectAt(Vector2 position)
        {
            if (latestPurchase != null && ContainsClick(latestPurchase, position))
            {
                Select(latestPurchase);
                return;
            }

            CharacterUnit nearest = null;
            float closest = float.PositiveInfinity;
            foreach (CharacterUnit ally in battle.Allies)
            {
                float distance = Vector2.Distance(position, ally.transform.position);
                if (!ContainsClick(ally, position) || distance >= closest)
                {
                    continue;
                }

                nearest = ally;
                closest = distance;
            }

            Select(nearest);
        }

        /// <summary>
        /// 선택한 아군에게 이동 명령을 내리고 결과를 안내합니다.
        /// </summary>
        public void MoveSelected(Vector2 destination)
        {
            if (SelectedUnit == null)
            {
                showMessage("이동할 아군을 먼저 왼쪽 클릭해 선택하세요.");
                return;
            }

            if (!battle.TryMove(SelectedUnit, destination))
            {
                showMessage("그곳까지 이동할 수 없습니다.");
                return;
            }

            if (battle.TimeController.IsPaused)
            {
                showMessage("이동 목적지를 예약했습니다. 정지를 해제하면 마지막 명령을 실행합니다.");
                return;
            }
            showMessage(SelectedUnit.Health.IsAlive
                ? "목적지로 이동합니다. 이동 중에는 공격과 저지를 멈춥니다."
                : "부활 후 이동할 목적지를 변경했습니다.");
        }

        /// <summary>
        /// 새로 구매한 아군은 선택을 해제하고 직접 선택할 때까지 화살표로 강조합니다.
        /// </summary>
        public void MarkNewUnit(CharacterUnit unit)
        {
            SetSelected(null);
            latestPurchase = unit;
            latestPurchase.GetComponent<UnitView>().SetAwaitingSelection(true);
        }

        /// <summary>
        /// 선택 표시를 옮기고 변경을 알립니다.
        /// </summary>
        private void SetSelected(CharacterUnit unit)
        {
            if (SelectedUnit != null)
            {
                SelectedUnit.GetComponent<UnitView>().SetSelected(false);
            }

            SelectedUnit = unit;
            if (SelectedUnit != null)
            {
                var view = SelectedUnit.GetComponent<UnitView>();
                view.SetSelected(true);
                view.SetAwaitingSelection(false);
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// 마우스 월드 좌표가 유닛 선택 영역에 포함되는지 확인합니다.
        /// </summary>
        private static bool ContainsClick(CharacterUnit ally, Vector2 position)
        {
            Vector2 feet = ally.transform.position;
            return Mathf.Abs(position.x - feet.x) <= ProjectDefine.Battle.SelectionHalfWidth
                && position.y >= feet.y - ProjectDefine.Battle.SelectionFootMargin
                && position.y <= feet.y + ally.Definition.HealthBarHeight;
        }

        #endregion // 함수
    }
}
