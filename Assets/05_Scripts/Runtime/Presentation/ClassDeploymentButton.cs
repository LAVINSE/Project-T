using UnityEngine;
using UnityEngine.EventSystems;

using SW.Base;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 기존 구매 버튼의 드래그 이벤트를 전장 배치 명령으로 전달합니다.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "ClassDeploymentButton")]
    public sealed class ClassDeploymentButton : SWMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 필드
        private BattlePlacementCommand placement;
        private UnitClassData definition;
        private UnityEngine.UI.Button button;
        private bool startedDrag;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 이 버튼의 클래스와 배치 담당을 연결합니다. 클릭은 기존 버튼 이벤트가 담당합니다.
        /// </summary>
        public void Configure(BattlePlacementCommand command, UnitClassData selectedClass)
        {
            placement = command;
            definition = selectedClass;
            button = GetComponent<UnityEngine.UI.Button>();
        }

        /// <summary>
        /// 활성 구매 버튼에서 왼쪽 드래그를 시작합니다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || button == null || !button.IsInteractable())
            {
                return;
            }

            placement.BeginPlacement(definition, true);
            startedDrag = placement.IsDragging;
        }

        /// <summary>
        /// 화면 이벤트 시스템의 드래그 전달을 유지합니다. 미리보기 갱신은 배치 담당이 수행합니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 현재 포인터 위치를 배치 담당에 전달하여 결제와 생성을 요청합니다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!startedDrag || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            startedDrag = false;
            placement.EndDrag(eventData.position);
        }

        /// <summary>
        /// 비활성화된 구매 버튼의 진행 중인 입력을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (startedDrag && placement != null)
            {
                placement.CancelPlacement();
            }

            startedDrag = false;
        }

        #endregion // 함수
    }
}
