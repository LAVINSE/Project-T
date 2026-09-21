using UnityEngine;

using TMPro;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 한 클래스의 외형·가격을 표시하고 클릭·드래그 구매를 기존 배치 명령에 연결합니다.
    /// </summary>
    public sealed class CharacterPurchaseSlot : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image portrait;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private ClassDeploymentButton dragHandler;
        private BattlePlacementCommand placement;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 구매할 클래스 정의입니다.
        /// </summary>
        public AllyClassDefinition Definition { get; private set; }

        /// <summary>
        /// 외형·가격·클릭·드래그에 필요한 참조입니다.
        /// </summary>
        public bool HasRequiredReferences => button != null && portrait != null && priceLabel != null && dragHandler != null;

        /// <summary>
        /// 구매 입력을 받는 버튼입니다.
        /// </summary>
        public UnityEngine.UI.Button Button => button;

        #endregion // 프로퍼티

        #region 연결
        /// <summary>
        /// 검증된 클래스와 배치 명령을 연결합니다. 재연결 시 클릭 구독을 중복하지 않습니다.
        /// </summary>
        public void Configure(AllyClassDefinition definition, BattlePlacementCommand command)
        {
            button.onClick.RemoveListener(BeginPlacement);
            Definition = definition;
            placement = command;
            portrait.sprite = definition.Appearance.Portrait;
            portrait.enabled = portrait.sprite != null;
            priceLabel.text = definition.DeploymentCost.ToString("0");
            dragHandler.Configure(command, definition);
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
        /// 클릭으로 미결제 배치를 시작합니다. 위치 확정 시 기존 배치 기능이 결제합니다.
        /// </summary>
        private void BeginPlacement()
        {
            if (placement != null && Definition != null && button.IsInteractable())
            {
                placement.BeginPlacement(Definition);
            }
        }

        /// <summary>
        /// 제거 시 클릭 구독을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(BeginPlacement);
            }
        }

        #endregion // 연결
    }
}
