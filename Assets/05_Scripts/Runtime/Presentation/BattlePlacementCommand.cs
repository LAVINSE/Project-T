using System;
using UnityEngine;
using UnityEngine.InputSystem;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 클릭 배치와 드래그 배치의 임시 상태를 관리하고 위치 확정 시에만 전투에 구매를 요청합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattlePlacementCommand")]
    public sealed class BattlePlacementCommand : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleSession session;
        [SerializeField] private BattlefieldPointer pointer;
        [SerializeField] private DeploymentPreview preview;
        private UnitClassData pendingClass;
        private int consumedFrame = -1;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 아직 결제되지 않은 배치를 진행 중인지 반환합니다.
        /// </summary>
        public bool IsPlacing => pendingClass != null;

        /// <summary>
        /// 구매 버튼에서 시작한 드래그가 진행 중인지 반환합니다.
        /// </summary>
        public bool IsDragging { get; private set; }

        /// <summary>
        /// 배치 확정·취소와 같은 프레임에 선택·이동 명령이 중복되지 않도록 합니다.
        /// </summary>
        public bool ConsumesWorldInput => IsPlacing || consumedFrame == Time.frameCount;

        /// <summary>
        /// 배치 완료 개체를 선택 표시 담당에 알립니다.
        /// </summary>
        public event Action<CharacterUnit> Deployed;

        /// <summary>
        /// 입력 안내를 전투 화면에 전달합니다.
        /// </summary>
        public event Action<string> MessageChanged;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 비용을 차감하지 않고 배치할 클래스를 선택합니다.
        /// </summary>
        public void BeginPlacement(UnitClassData definition, bool dragging = false)
        {
            if (!session.CanCommand
                || session.Wallet == null
                || definition == null
                || !definition.IsValid
                || session.Wallet.Balance < definition.DeploymentCost)
            {
                return;
            }

            pendingClass = definition;
            IsDragging = dragging;
            consumedFrame = Time.frameCount;
            MessageChanged?.Invoke(definition.DisplayName + " 배치 · 초록 위치에 놓기 · 오른쪽 클릭 또는 Esc로 취소");
        }

        /// <summary>
        /// 드래그를 끝낸 화면 좌표에 한 번만 배치합니다.
        /// </summary>
        public void EndDrag(Vector2 screenPosition)
        {
            if (IsDragging && IsPlacing)
            {
                ConfirmPlacement(screenPosition);
            }
        }

        /// <summary>
        /// 미결제 배치를 취소하고 미리보기를 정리합니다.
        /// </summary>
        public void CancelPlacement()
        {
            if (!IsPlacing)
            {
                return;
            }

            Clear();
            MessageChanged?.Invoke("배치를 취소했습니다. 비용은 차감되지 않았습니다.");
        }

        /// <summary>
        /// 배치 미리보기와 클릭·드래그의 확정 또는 취소 입력을 처리합니다.
        /// </summary>
        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            if (!session.CanCommand)
            {
                CancelPlacement();
                return;
            }

            Mouse mouse = Mouse.current;
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                CancelPlacement();
                return;
            }

            if (mouse == null)
            {
                preview.Hide();
                return;
            }

            Vector2 screenPosition = mouse.position.ReadValue();
            if (pointer.TryGetPosition(screenPosition, out Vector2 position))
            {
                preview.Show(pendingClass, position, session.CanDeployAt(pendingClass, position));
            }
            else
            {
                preview.Hide();
            }

            if (!IsDragging
                && consumedFrame != Time.frameCount
                && mouse.leftButton.wasPressedThisFrame
                && pointer.TryGetPosition(screenPosition, out _))
            {
                ConfirmPlacement(screenPosition);
            }
        }

        /// <summary>
        /// 배치 위치를 다시 검증하고 구매 결과를 전달합니다.
        /// </summary>
        private void ConfirmPlacement(Vector2 screenPosition)
        {
            UnitClassData definition = pendingClass;
            Clear();
            if (!pointer.TryGetPosition(screenPosition, out Vector2 position))
            {
                MessageChanged?.Invoke("전장 밖에서는 배치할 수 없습니다. 비용은 차감되지 않았습니다.");
                return;
            }

            if (!session.TryDeploy(definition, position, out CharacterUnit unit, out string reason))
            {
                MessageChanged?.Invoke(reason + " 비용은 차감되지 않았습니다.");
                return;
            }

            Deployed?.Invoke(unit);
            MessageChanged?.Invoke(definition.DisplayName + " 배치 완료 · 아군을 클릭하면 공격 범위를 확인할 수 있습니다.");
        }

        /// <summary>
        /// 진행 중인 배치 입력과 미리보기를 정리합니다.
        /// </summary>
        private void Clear()
        {
            pendingClass = null;
            IsDragging = false;
            consumedFrame = Time.frameCount;
            preview.Hide();
        }

        /// <summary>
        /// 컴포넌트가 꺼지면 진행 중인 배치를 취소합니다.
        /// </summary>
        private void OnDisable()
        {
            if (preview != null)
            {
                CancelPlacement();
            }
        }

        /// <summary>
        /// 응용 프로그램이 포커스를 잃으면 배치를 취소합니다.
        /// </summary>
        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                CancelPlacement();
            }
        }

        #endregion // 함수
    }
}
