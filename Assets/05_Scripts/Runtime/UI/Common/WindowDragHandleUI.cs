using System;
using UnityEngine;
using UnityEngine.EventSystems;

using SW.Base;
using SW.Util;

namespace ProjectT.UI
{
    /// <summary>
    /// 제목 영역의 왼쪽 드래그로 연결된 창을 이동합니다. 부모 화면의 범위를 넘지 않도록 제한합니다.
    /// </summary>
    public sealed class WindowDragHandleUI : SWMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 필드
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform window;
        private RectTransform boundary;
        private Func<bool> canMove;
        private Action onStarted;
        private Vector2 pointerOffset;
        private Camera eventCamera;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 제목 영역을 잡고 창을 이동 중인지 반환합니다.
        /// </summary>
        public bool IsDragging { get; private set; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 이동할 창과 허용 조건을 연결합니다. 진행 중이던 이동 입력은 해제합니다.
        /// </summary>
        public void Initialize(RectTransform target, Func<bool> allowed, Action started)
        {
            CancelDrag();
            window = target;
            boundary = (RectTransform)target.parent;
            canMove = allowed;
            onStarted = started;
        }

        /// <summary>
        /// 다시 열린 창이 현재 화면 범위에 있도록 위치를 정리합니다.
        /// </summary>
        private void OnEnable()
        {
            ClampPosition();
        }

        /// <summary>
        /// 화면이나 제목 영역 크기가 바뀌면 창 위치를 현재 범위로 제한합니다.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            ClampPosition();
        }

        /// <summary>
        /// 비활성화되면 진행 중인 이동 입력을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            CancelDrag();
        }

        #endregion // 초기화

        #region 드래그
        /// <summary>
        /// 왼쪽 버튼으로 잡은 위치와 창 사이의 간격을 유지하며 이동을 시작합니다. 이동 불가 상태에서는 무시합니다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || window == null || boundary == null || canMove == null
                || eventData.button != PointerEventData.InputButton.Left || !canMove())
            {
                return;
            }

            eventCamera = eventData.pressEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boundary, eventData.pressPosition, eventCamera, out Vector2 point))
            {
                return;
            }

            pointerOffset = (Vector2)window.localPosition - point;
            IsDragging = true;
            onStarted?.Invoke();
            OnDrag(eventData);
        }

        /// <summary>
        /// 입력 좌표를 부모 화면 좌표로 변환하여 창을 이동합니다. 중간에 이동 불가 상태가 되면 즉시 중단합니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            if (window == null || boundary == null || !canMove())
            {
                CancelDrag();
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(boundary, eventData.position, eventCamera, out Vector2 point))
            {
                Vector2 position = point + pointerOffset;
                window.localPosition = new Vector3(position.x, position.y, window.localPosition.z);
                ClampPosition();
            }
        }

        /// <summary>
        /// 마우스를 놓으면 이동을 종료합니다. 아이템 놓기 동작은 호출하지 않습니다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            CancelDrag();
        }

        /// <summary>
        /// 현재 위치를 유지하면서 드래그 입력만 해제합니다.
        /// </summary>
        public void CancelDrag()
        {
            IsDragging = false;
            eventCamera = null;
        }

        #endregion // 드래그

        #region 위치 제한
        /// <summary>
        /// 창의 네 모서리를 부모 좌표로 확인해 화면 안으로 보정합니다. 창이 화면보다 크면 해당 축을 가운데 맞춥니다.
        /// </summary>
        private void ClampPosition()
        {
            if (window == null || boundary == null || boundary.rect.width <= 0f || boundary.rect.height <= 0f)
            {
                return;
            }

            window.GetWorldCorners(corners);
            Vector2 minimum = boundary.InverseTransformPoint(corners[0]);
            Vector2 maximum = minimum;
            for (int index = 1; index < corners.Length; index++)
            {
                Vector2 point = boundary.InverseTransformPoint(corners[index]);
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            Rect bounds = boundary.rect;
            float horizontal = ClampAxis(minimum.x, maximum.x, bounds.xMin, bounds.xMax);
            float vertical = ClampAxis(minimum.y, maximum.y, bounds.yMin, bounds.yMax);
            window.localPosition += new Vector3(horizontal, vertical, 0f);
        }

        /// <summary>
        /// 한 축에서 화면을 벗어난 거리만 반환합니다. 화면보다 큰 창은 중앙 보정 거리를 반환합니다.
        /// </summary>
        private float ClampAxis(float minimum, float maximum, float boundaryMinimum, float boundaryMaximum)
        {
            if (maximum - minimum > boundaryMaximum - boundaryMinimum)
            {
                return (boundaryMinimum + boundaryMaximum - minimum - maximum) * 0.5f;
            }

            return Mathf.Clamp(0f, boundaryMinimum - minimum, boundaryMaximum - maximum);
        }

        #endregion // 위치 제한
    }
}
