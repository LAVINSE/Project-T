using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Battle;
using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 한 클래스의 외형·가격을 표시하고 클릭·드래그 구매를 전장 입력에 전달합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class CharacterSlotUI : SWMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 필드
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text priceLabel;
        private BattleInput input;
        private bool isDragging;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 구매할 클래스 정의입니다.
        /// </summary>
        public UnitClassData UnitClass { get; private set; }

        #endregion // 프로퍼티

        #region 연결
        /// <summary>
        /// 클래스와 전장 입력을 연결합니다. 재연결 시 클릭 구독을 중복하지 않습니다.
        /// </summary>
        public void Configure(UnitClassData unitClass, BattleInput battleInput)
        {
            button.onClick.RemoveListener(BeginPlacement);
            UnitClass = unitClass;
            input = battleInput;
            portrait.sprite = unitClass.Portrait;
            portrait.enabled = portrait.sprite != null;
            priceLabel.text = unitClass.DeploymentCost.ToString("0");
            button.onClick.AddListener(BeginPlacement);
        }

        /// <summary>
        /// 사용 가능 여부를 표시하며 비활성 구매로 재화를 차감하지 않습니다.
        /// </summary>
        public void SetAvailable(bool available)
        {
            button.interactable = available;
        }

        /// <summary>
        /// 클릭으로 미결제 배치를 시작합니다. 위치 확정 시 전투가 결제합니다.
        /// </summary>
        private void BeginPlacement()
        {
            if (input != null && button.IsInteractable())
            {
                input.BeginPlacement(UnitClass);
            }
        }

        #endregion // 연결

        #region 드래그
        /// <summary>
        /// 활성 구매 버튼에서 왼쪽 드래그로 배치를 시작합니다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (input == null || eventData.button != PointerEventData.InputButton.Left || !button.IsInteractable())
            {
                return;
            }

            input.BeginPlacement(UnitClass, true);
            isDragging = input.Placement.IsPlacing;
        }

        /// <summary>
        /// 이벤트 시스템의 드래그 전달을 유지합니다. 미리보기 이동은 전장 입력이 처리합니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 드래그를 끝낸 위치를 전장 입력에 전달하여 결제와 생성을 요청합니다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            isDragging = false;
            input.EndDrag(eventData.position);
        }

        /// <summary>
        /// 비활성화된 구매 버튼의 진행 중인 드래그를 취소합니다.
        /// </summary>
        private void OnDisable()
        {
            if (isDragging && input != null)
            {
                input.CancelPlacement();
            }

            isDragging = false;
        }

        /// <summary>
        /// 제거 시 클릭 구독을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            button.onClick.RemoveListener(BeginPlacement);
        }

        #endregion // 드래그
    }
}
