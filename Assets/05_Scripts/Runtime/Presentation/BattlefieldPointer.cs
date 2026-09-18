using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using SW.Base;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 화면 포인터를 전장 좌표로 변환하며 현재 화면 요소가 가리는 입력을 제외합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattlefieldPointer")]
    public sealed class BattlefieldPointer : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Camera worldCamera;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private PointerEventData pointerData;
        private EventSystem previousEventSystem;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 카메라 밖이나 화면 버튼·팝업 위가 아닌 위치만 전장 좌표로 반환합니다.
        /// </summary>
        public bool TryGetPosition(Vector2 screenPosition, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (worldCamera == null || !worldCamera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            EventSystem current = EventSystem.current;
            if (current != null)
            {
                if (pointerData == null || previousEventSystem != current)
                {
                    previousEventSystem = current;
                    pointerData = new PointerEventData(current);
                }

                pointerData.Reset();
                pointerData.position = screenPosition;
                hits.Clear();
                current.RaycastAll(pointerData, hits);
                if (hits.Count > 0)
                {
                    return false;
                }
            }

            worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return true;
        }

        #endregion // 함수
    }
}
