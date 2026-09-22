using System;
using UnityEngine;

using ProjectT.Data;
using ProjectT.Units;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 결제 전 배치 상태와 미리보기를 관리하고 위치를 확정할 때만 전투에 구매를 요청합니다.
    /// 화면 좌표 변환과 입력 신호는 BattleInput이 담당합니다.
    /// </summary>
    public sealed class PlacementController
    {
        #region 필드
        private readonly BattleManager battle;
        private readonly DeploymentPreview preview;
        private readonly Action<string> showMessage;
        private UnitClassData pendingClass;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 아직 결제되지 않은 배치를 진행 중인지 반환합니다.
        /// </summary>
        public bool IsPlacing => pendingClass != null;

        /// <summary>
        /// 구매 버튼에서 시작한 드래그 배치인지 반환합니다.
        /// </summary>
        public bool IsDragging { get; private set; }

        /// <summary>
        /// 배치 시작·취소·확정으로 진행 상태가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 배치에 성공한 아군을 전달합니다.
        /// </summary>
        public event Action<CharacterUnit> Deployed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 전투·미리보기·안내 문구 출력을 연결합니다.
        /// </summary>
        public PlacementController(BattleManager battle, DeploymentPreview preview, Action<string> showMessage)
        {
            this.battle = battle;
            this.preview = preview;
            this.showMessage = showMessage;
        }

        #endregion // 초기화

        #region 함수
        /// <summary>
        /// 비용을 차감하지 않고 배치할 클래스를 선택합니다. 명령할 수 없거나 잔액이 부족하면 무시하고 false입니다.
        /// </summary>
        public bool Begin(UnitClassData unitClass, bool dragging)
        {
            if (!battle.CanCommand || unitClass == null || battle.Wallet.Balance < unitClass.DeploymentCost)
            {
                return false;
            }

            pendingClass = unitClass;
            IsDragging = dragging;
            showMessage(unitClass.DisplayName + " 배치 · 초록 위치에 놓기 · 오른쪽 클릭 또는 Esc로 취소");
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// 포인터 위치의 배치 가능 여부를 미리보기로 표시합니다. 전장 밖이면 숨깁니다.
        /// </summary>
        public void UpdatePreview(Vector2? worldPosition)
        {
            if (!IsPlacing)
            {
                return;
            }

            if (!worldPosition.HasValue)
            {
                preview.Hide();
                return;
            }

            preview.Show(pendingClass, worldPosition.Value, battle.CanDeployAt(pendingClass, worldPosition.Value, out _));
        }

        /// <summary>
        /// 배치 위치를 확정해 구매를 요청하고 결과를 안내합니다. 전장 밖이거나 실패하면 비용을 차감하지 않습니다.
        /// </summary>
        public void Confirm(Vector2? worldPosition)
        {
            UnitClassData unitClass = pendingClass;
            Clear();
            if (!worldPosition.HasValue)
            {
                showMessage("전장 밖에서는 배치할 수 없습니다. 비용은 차감되지 않았습니다.");
                return;
            }

            if (!battle.TryDeploy(unitClass, worldPosition.Value, out CharacterUnit unit, out string reason))
            {
                showMessage(reason + " 비용은 차감되지 않았습니다.");
                return;
            }

            Deployed?.Invoke(unit);
            showMessage(unitClass.DisplayName + " 배치 완료 · 아군을 클릭하면 공격 범위를 확인할 수 있습니다.");
        }

        /// <summary>
        /// 미결제 배치를 취소합니다. 진행 중인 배치가 없으면 무시합니다.
        /// </summary>
        public void Cancel()
        {
            if (!IsPlacing)
            {
                return;
            }

            Clear();
            showMessage("배치를 취소했습니다. 비용은 차감되지 않았습니다.");
        }

        /// <summary>
        /// 진행 중인 배치와 미리보기를 정리합니다.
        /// </summary>
        private void Clear()
        {
            pendingClass = null;
            IsDragging = false;
            preview.Hide();
            Changed?.Invoke();
        }

        #endregion // 함수
    }
}
