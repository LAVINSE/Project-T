using UnityEngine;

using SW.Base;

using ProjectT.Data;

namespace ProjectT.View
{
    /// <summary>
    /// 배치할 캐릭터와 공격 범위를 반투명으로 보여주며 유효하지 않은 위치를 빨간색으로 구분합니다.
    /// </summary>
    public sealed class DeploymentPreview : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SpriteRenderer character;
        [SerializeField] private LineRenderer attackRange;
        [SerializeField] private LineRenderer placementRing;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 현재 클래스의 외형과 실제 공격 거리로 미리보기를 갱신합니다.
        /// </summary>
        public void Show(UnitClassData unitClass, Vector2 position, bool valid)
        {
            character.sprite = unitClass.PreviewSprite;
            character.transform.position = position + Vector2.up * unitClass.FeetOffset;
            if (character.sprite != null)
            {
                character.transform.localScale = Vector3.one * character.sprite.pixelsPerUnit / ProjectDefine.Battle.PixelsPerUnit;
            }

            Color color = valid ? ProjectDefine.Palette.ValidPlacement : ProjectDefine.Palette.InvalidPlacement;
            character.color = valid ? ProjectDefine.Palette.PreviewCharacter : color;
            attackRange.startColor = color;
            attackRange.endColor = color;
            placementRing.startColor = color;
            placementRing.endColor = color;
            attackRange.ExDrawCircle(position, unitClass.AttackRange);
            placementRing.ExDrawCircle(position, ProjectDefine.Battle.PlacementRingRadius);
            SetVisible(true);
        }

        /// <summary>
        /// 배치 취소·완료 또는 화면 요소 위에서 미리보기를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        /// <summary>
        /// 미리보기 구성 요소를 함께 켜거나 끕니다.
        /// </summary>
        private void SetVisible(bool visible)
        {
            character.enabled = visible;
            attackRange.enabled = visible;
            placementRing.enabled = visible;
        }

        #endregion // 함수
    }
}
