using UnityEngine;
using UnityEngine.InputSystem;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 왼쪽 클릭 선택·오른쪽 클릭 이동을 처리하고 배치 입력과 중복되지 않도록 합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattleMouseCommand")]
    public sealed class BattleMouseCommand : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleSession session;
        [SerializeField] private BattlefieldPointer pointer;
        [SerializeField] private BattlePlacementCommand placement;
        private CharacterUnit selectedUnit;
        private CharacterUnit latestPurchase;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 선택한 아군입니다. 부활 대기 중인 아군도 선택할 수 있습니다.
        /// </summary>
        public CharacterUnit SelectedUnit => selectedUnit;

        /// <summary>
        /// 플레이어에게 표시할 구매·선택·이동 안내입니다.
        /// </summary>
        public string Message { get; private set; } = "클래스를 드래그하거나 클릭한 뒤 지면을 클릭해 배치하세요.";

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 구매할 클래스를 골라 위치 선택을 시작합니다. 배치가 완료되어야 비용이 차감됩니다.
        /// </summary>
        public void PurchaseClass(UnitClassData definition)
        {
            placement.BeginPlacement(definition);
        }

        /// <summary>
        /// 배치 완료와 안내 문구 변경 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (placement == null)
            {
                return;
            }

            placement.Deployed += OnDeployed;
            placement.MessageChanged += OnMessageChanged;
        }

        /// <summary>
        /// 배치 관련 알림의 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (placement == null)
            {
                return;
            }

            placement.Deployed -= OnDeployed;
            placement.MessageChanged -= OnMessageChanged;
        }

        /// <summary>
        /// 배치 명령의 안내 문구를 선택 화면에 전달합니다.
        /// </summary>
        private void OnMessageChanged(string message)
        {
            Message = message;
        }

        /// <summary>
        /// 새 아군을 강조하고 구매 결과를 표시합니다.
        /// </summary>
        private void OnDeployed(CharacterUnit created)
        {
            SelectUnit(null);
            latestPurchase = created;
            created.GetComponent<UnitPresentation>().SetAwaitingSelection(true);
        }

        /// <summary>
        /// 아군 한 명을 선택합니다. 목록을 이용하면 겹친 다른 아군도 선택할 수 있습니다.
        /// </summary>
        public void SelectUnit(CharacterUnit unit)
        {
            if (!session.CanCommand)
            {
                return;
            }

            placement.CancelPlacement();
            if (selectedUnit != null)
            {
                selectedUnit.GetComponent<UnitPresentation>().SetSelected(false);
            }

            selectedUnit = unit;
            if (selectedUnit != null)
            {
                var presentation = selectedUnit.GetComponent<UnitPresentation>();
                presentation.SetSelected(true);
                presentation.SetAwaitingSelection(false);
            }

            Message = unit != null ? unit.Definition.DisplayName + " 선택 · 오른쪽 클릭으로 이동하세요." : "아군을 왼쪽 클릭해 선택하고, 오른쪽 클릭으로 이동하세요.";
        }

        /// <summary>
        /// 마우스 입력으로 아군을 선택하고 이동 목적지를 지시합니다.
        /// </summary>
        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !session.CanCommand || placement.ConsumesWorldInput)
            {
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
            {
                return;
            }

            if (!pointer.TryGetPosition(mouse.position.ReadValue(), out Vector2 position))
            {
                return;
            }

            ExecuteWorldClick(position, mouse.rightButton.wasPressedThisFrame);
        }

        /// <summary>
        /// 왼쪽 클릭은 선택만 변경하고 오른쪽 클릭만 이동 명령을 전달합니다.
        /// </summary>
        public void ExecuteWorldClick(Vector2 position, bool rightButton = false)
        {
            if (!session.CanCommand)
            {
                return;
            }

            if (placement.IsPlacing)
            {
                if (rightButton)
                {
                    placement.CancelPlacement();
                }

                return;
            }

            if (rightButton)
            {
                if (selectedUnit == null)
                {
                    Message = "이동할 아군을 먼저 왼쪽 클릭해 선택하세요.";
                    return;
                }

                Message = session.TryMove(selectedUnit, position)
                    ? selectedUnit.Health.IsAlive
                        ? "목적지로 이동합니다. 이동 중에는 공격과 저지를 멈춥니다."
                        : "부활 후 이동할 목적지를 변경했습니다."
                    : "그곳까지 이동할 수 없습니다.";
                return;
            }

            // 배치 위치가 겹치면 방금 구매한 아군을 먼저 고릅니다. 다른 아군은 목록에서도 선택할 수 있습니다.
            if (latestPurchase != null && ContainsClick(latestPurchase, position))
            {
                SelectUnit(latestPurchase);
                return;
            }

            CharacterUnit nearest = null;
            float closest = float.PositiveInfinity;
            foreach (CharacterUnit ally in session.Allies)
            {
                if (!ContainsClick(ally, position))
                {
                    continue;
                }

                float distance = Vector2.Distance(position, ally.transform.position);
                if (distance >= closest)
                {
                    continue;
                }

                nearest = ally;
                closest = distance;
            }

            SelectUnit(nearest);
        }

        /// <summary>
        /// 마우스 월드 좌표가 유닛 선택 영역에 포함되는지 확인합니다.
        /// </summary>
        private static bool ContainsClick(CharacterUnit ally, Vector2 position)
        {
            Vector2 feet = ally.transform.position;
            return Mathf.Abs(position.x - feet.x) <= 0.65f
                && position.y >= feet.y - 0.3f
                && position.y <= feet.y + ally.Definition.HealthBarHeight;
        }

        #endregion // 함수
    }
}
