using System;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 출전할 스테이지 하나를 표시하고 선택을 조립 지점에 전달합니다.
    /// </summary>
    public sealed class HubStageButtonUI : MonoBehaviour
    {
        #region 필드
        [SerializeField] private Button stageButton;
        [SerializeField] private TMP_Text nameText;
        private StageData stage;
        private Action<StageData> selected;

        #endregion // 필드

        #region 표시와 입력
        /// <summary>
        /// 스테이지 이름을 표시하고 선택 요청을 연결합니다. 출전할 수 없는 스테이지는 누를 수 없게 표시합니다.
        /// </summary>
        public void Present(StageData target, bool available, Action<StageData> onSelected)
        {
            stage = target;
            selected = onSelected;
            nameText.text = target.DisplayName;
            stageButton.interactable = available;
            stageButton.onClick.RemoveListener(OnClicked);
            stageButton.onClick.AddListener(OnClicked);
        }

        /// <summary>
        /// 장면 전환 중 반복 선택을 막습니다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            stageButton.interactable = interactable;
        }

        /// <summary>
        /// 선택한 스테이지를 전달합니다.
        /// </summary>
        private void OnClicked()
        {
            selected?.Invoke(stage);
        }

        /// <summary>
        /// 버튼 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            stageButton.onClick.RemoveListener(OnClicked);
        }

        #endregion // 표시와 입력
    }
}
