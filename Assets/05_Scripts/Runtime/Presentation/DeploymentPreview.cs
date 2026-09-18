using UnityEngine;

using SW.Base;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 배치할 캐릭터와 공격 범위를 반투명으로 보여주며 유효하지 않은 위치를 빨간색으로 구분합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "DeploymentPreview")]
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
        public void Show(AllyClassDefinition definition, Vector2 position, bool valid)
        {
            Sprite[] frames = definition.Appearance.GetFrames(true, false, false);
            character.sprite = frames.Length > 0 ? frames[0] : null;
            character.transform.position = position + Vector2.up * definition.Appearance.FeetOffset;
            if (character.sprite != null)
            {
                character.transform.localScale = Vector3.one * character.sprite.pixelsPerUnit / 32f;
            }

            Color color = valid ? new Color(0.4f, 1f, 0.72f, 0.8f) : new Color(1f, 0.3f, 0.28f, 0.8f);
            character.color = valid ? new Color(1f, 1f, 1f, 0.8f) : new Color(color.r, color.g, color.b, 0.8f);
            attackRange.startColor = attackRange.endColor = color;
            placementRing.startColor = placementRing.endColor = color;
            BattleIndicatorGeometry.DrawCircle(attackRange, position, definition.AttackRange);
            BattleIndicatorGeometry.DrawCircle(placementRing, position, 0.5f);
            character.enabled = attackRange.enabled = placementRing.enabled = true;
        }

        /// <summary>
        /// 배치 취소·완료 또는 화면 요소 위에서 미리보기를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            character.enabled = attackRange.enabled = placementRing.enabled = false;
        }

        #endregion // 함수
    }
}
