using UnityEngine;
using UnityEngine.Rendering;

using SW.Base;

namespace ProjectT.View
{
    /// <summary>
    /// 여러 그림으로 구성된 고정 소품을 하나의 지면 기준점으로 캐릭터와 앞뒤 정렬합니다. 활성화될 때 한 번만 계산합니다.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(SortingGroup))]
    public sealed class DepthSorting : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Vector3 localGroundPosition;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 소품이 바닥에 닿는 월드 위치입니다.
        /// </summary>
        public Vector3 GroundPosition => transform.TransformPoint(localGroundPosition);

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 활성화 직후 월드 높이에 맞는 정렬 순서를 적용합니다.
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 편집 모드에서 소품을 옮겼을 때만 정렬을 다시 계산합니다. 실행 중에는 동작하지 않습니다.
        /// </summary>
        private void Update()
        {
            if (Application.isPlaying || !transform.hasChanged)
            {
                return;
            }

            transform.hasChanged = false;
            Refresh();
        }
#endif

        /// <summary>
        /// 현재 지면 높이로 그리기 순서를 계산합니다.
        /// </summary>
        private void Refresh()
        {
            var group = GetComponent<SortingGroup>();
            group.sortingLayerID = 0;
            group.sortingOrder = GroundPosition.y.ExToSortingOrder();
        }

        #endregion // 함수
    }
}
