using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

using SW.Base;

using ProjectT.Data;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 전장 마우스·키보드 입력 신호를 받아 화면 좌표를 전장 좌표로 바꾸고 배치 또는 선택 담당에 전달합니다.
    /// 배치 중에는 배치가 입력을 먼저 사용하며, 안내 문구를 한 곳에서 제공합니다.
    /// </summary>
    [RequireComponent(typeof(BattleManager))]
    public sealed class BattleInput : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Camera worldCamera;
        [SerializeField] private DeploymentPreview preview;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private InputAction selectAction;
        private InputAction commandAction;
        private InputAction cancelAction;
        private InputAction pointAction;
        private BattleManager battle;
        private PointerEventData pointerData;
        private EventSystem pointerEventSystem;
        private Func<bool> blocksFieldInput;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 아군 선택과 이동 명령 담당입니다.
        /// </summary>
        public SelectionController Selection { get; private set; }

        /// <summary>
        /// 결제 전 배치 담당입니다.
        /// </summary>
        public PlacementController Placement { get; private set; }

        /// <summary>
        /// 플레이어에게 표시할 구매·선택·이동 안내입니다.
        /// </summary>
        public string Message { get; private set; } = "클래스를 드래그하거나 클릭한 뒤 지면을 클릭해 배치하세요.";

        /// <summary>
        /// 안내 문구가 바뀌었을 때 발생합니다.
        /// </summary>
        public event Action MessageChanged;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 선택·배치 담당과 마우스·키보드 입력 동작을 만듭니다.
        /// </summary>
        private void Awake()
        {
            battle = GetComponent<BattleManager>();
            Selection = new SelectionController(battle, ShowMessage);
            Placement = new PlacementController(battle, preview, ShowMessage);
            selectAction = new InputAction("Select", InputActionType.Button, "<Mouse>/leftButton");
            commandAction = new InputAction("Command", InputActionType.Button, "<Mouse>/rightButton");
            cancelAction = new InputAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            pointAction = new InputAction("Point", InputActionType.PassThrough, "<Mouse>/position");
        }

        /// <summary>
        /// 입력과 전투·배치 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            selectAction.performed += OnSelect;
            commandAction.performed += OnCommand;
            cancelAction.performed += OnCancel;
            pointAction.performed += OnPoint;
            battle.StateChanged += OnBattleStateChanged;
            Placement.Deployed += Selection.MarkNewUnit;
            selectAction.Enable();
            commandAction.Enable();
            cancelAction.Enable();
            pointAction.Enable();
        }

        /// <summary>
        /// 진행 중인 배치를 취소하고 입력과 알림 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            Placement.Cancel();
            selectAction.Disable();
            commandAction.Disable();
            cancelAction.Disable();
            pointAction.Disable();
            selectAction.performed -= OnSelect;
            commandAction.performed -= OnCommand;
            cancelAction.performed -= OnCancel;
            pointAction.performed -= OnPoint;
            battle.StateChanged -= OnBattleStateChanged;
            Placement.Deployed -= Selection.MarkNewUnit;
        }

        /// <summary>
        /// 입력 동작을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            selectAction.Dispose();
            commandAction.Dispose();
            cancelAction.Dispose();
            pointAction.Dispose();
        }

        #endregion // 초기화

        #region 화면 요청
        /// <summary>
        /// 아이템 집기·버리기 확인처럼 필드가 소비하면 안 되는 입력의 조건을 연결합니다.
        /// </summary>
        public void SetFieldInputBlocker(Func<bool> blocker)
        {
            blocksFieldInput = blocker;
        }

        /// <summary>
        /// 구매 버튼의 클릭·드래그로 배치를 시작하고 현재 포인터 위치에 미리보기를 표시합니다.
        /// </summary>
        public void BeginPlacement(UnitClassData unitClass, bool dragging = false)
        {
            if (Placement.Begin(unitClass, dragging))
            {
                Placement.UpdatePreview(GetWorldPosition(pointAction.ReadValue<Vector2>()));
            }
        }

        /// <summary>
        /// 구매 버튼 드래그를 끝낸 화면 좌표에 한 번만 배치합니다.
        /// </summary>
        public void EndDrag(Vector2 screenPosition)
        {
            if (Placement.IsPlacing && Placement.IsDragging)
            {
                Placement.Confirm(GetWorldPosition(screenPosition));
            }
        }

        /// <summary>
        /// 진행 중인 배치를 취소합니다.
        /// </summary>
        public void CancelPlacement()
        {
            Placement.Cancel();
        }

        #endregion // 화면 요청

        #region 입력 신호
        /// <summary>
        /// 왼쪽 클릭으로 클릭 배치를 확정하거나 아군을 선택합니다.
        /// </summary>
        private void OnSelect(InputAction.CallbackContext context)
        {
            Vector2? position = GetWorldPosition(pointAction.ReadValue<Vector2>());
            if (!battle.CanInteract || !position.HasValue)
            {
                return;
            }

            if (Placement.IsPlacing)
            {
                if (!Placement.IsDragging)
                {
                    Placement.Confirm(position);
                }

                return;
            }

            Selection.SelectAt(position.Value);
        }

        /// <summary>
        /// 오른쪽 클릭으로 배치를 취소하거나 선택한 아군을 이동시킵니다.
        /// </summary>
        private void OnCommand(InputAction.CallbackContext context)
        {
            if (blocksFieldInput?.Invoke() == true)
            {
                return;
            }
            if (Placement.IsPlacing)
            {
                Placement.Cancel();
                return;
            }

            Vector2? position = GetWorldPosition(pointAction.ReadValue<Vector2>());
            if (battle.CanInteract && position.HasValue)
            {
                Selection.MoveSelected(position.Value);
            }
        }

        /// <summary>
        /// Esc로 진행 중인 배치를 취소합니다.
        /// </summary>
        private void OnCancel(InputAction.CallbackContext context)
        {
            Placement.Cancel();
        }

        /// <summary>
        /// 배치 중 포인터가 움직이면 미리보기를 옮깁니다.
        /// </summary>
        private void OnPoint(InputAction.CallbackContext context)
        {
            if (Placement.IsPlacing)
            {
                Placement.UpdatePreview(GetWorldPosition(context.ReadValue<Vector2>()));
            }
        }

        /// <summary>
        /// 정지·결과로 명령할 수 없게 되면 배치를 취소하고, 잔액 변화는 미리보기에 반영합니다.
        /// </summary>
        private void OnBattleStateChanged()
        {
            if (!Placement.IsPlacing)
            {
                return;
            }

            if (!battle.CanCommand)
            {
                Placement.Cancel();
                return;
            }

            Placement.UpdatePreview(GetWorldPosition(pointAction.ReadValue<Vector2>()));
        }

        /// <summary>
        /// 응용 프로그램이 포커스를 잃으면 배치를 취소합니다.
        /// </summary>
        private void OnApplicationFocus(bool focused)
        {
            if (!focused && Placement != null)
            {
                Placement.Cancel();
            }
        }

        /// <summary>
        /// 안내 문구를 바꾸고 화면에 알립니다.
        /// </summary>
        public void ShowMessage(string message)
        {
            Message = message;
            MessageChanged?.Invoke();
        }

        #endregion // 입력 신호

        #region 좌표 변환
        /// <summary>
        /// 카메라 밖이나 화면 버튼·팝업 위가 아닌 위치만 전장 좌표로 반환하며 그 밖에는 null입니다.
        /// </summary>
        private Vector2? GetWorldPosition(Vector2 screenPosition)
        {
            if (blocksFieldInput?.Invoke() == true || !worldCamera.pixelRect.Contains(screenPosition))
            {
                return null;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                if (pointerData == null || pointerEventSystem != eventSystem)
                {
                    pointerEventSystem = eventSystem;
                    pointerData = new PointerEventData(eventSystem);
                }

                pointerData.Reset();
                pointerData.position = screenPosition;
                raycastResults.Clear();
                eventSystem.RaycastAll(pointerData, raycastResults);
                if (raycastResults.Count > 0)
                {
                    return null;
                }
            }

            return (Vector2)worldCamera.ScreenToWorldPoint(screenPosition);
        }

        #endregion // 좌표 변환
    }
}
