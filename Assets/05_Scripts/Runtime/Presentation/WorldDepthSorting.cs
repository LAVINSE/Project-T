using UnityEngine;
using UnityEngine.Rendering;

using SW.Base;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 여러 그림으로 구성된 소품도 하나의 지면 기준점으로 캐릭터와 앞뒤를 정렬합니다.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(SortingGroup))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "WorldDepthSorting")]
    public sealed class WorldDepthSorting : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Vector3 localGroundPosition;
        private SortingGroup group;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 소품이 바닥에 닿는 월드 위치입니다.
        /// </summary>
        public Vector3 GroundPosition => transform.TransformPoint(localGroundPosition);

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 장면 제작 시 확인한 바닥 위치를 객체 기준 좌표로 보관합니다.
        /// </summary>
        public void Configure(Vector3 worldGroundPosition)
        {
            localGroundPosition = transform.InverseTransformPoint(worldGroundPosition);
            Refresh();
        }

        /// <summary>
        /// 지면의 높이가 낮을수록 앞에 표시하며 바닥 타일과 화면 표시용 순서는 남겨 둡니다.
        /// </summary>
        public static int OrderAt(float groundHeight)
        {
            return Mathf.Clamp(Mathf.RoundToInt(-groundHeight * 100f), -1800, 1800);
        }

        /// <summary>
        /// 활성화 직후 월드 높이에 맞는 정렬 순서를 적용합니다.
        /// </summary>
        private void OnEnable()
        {
            Refresh();
        }

        /// <summary>
        /// 변경된 월드 높이에 맞춰 정렬 순서를 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            Refresh();
        }

        /// <summary>
        /// 현재 세로 위치로 스프라이트의 그리기 순서를 계산합니다.
        /// </summary>
        private void Refresh()
        {
            if (group == null)
            {
                group = GetComponent<SortingGroup>();
            }

            group.sortingLayerID = 0;
            group.sortingOrder = OrderAt(GroundPosition.y);
        }

        #endregion // 함수
    }
}
