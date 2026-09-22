using UnityEngine;

using SW.Base;
using SW.Util;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.View
{
    /// <summary>
    /// 사용자가 만든 가로·세로 체력바 프리팹을 실제 체력과 공유 색상으로 표시합니다. 체력이 바뀔 때만 갱신합니다.
    /// </summary>
    public sealed class HealthBar : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private bool vertical;
        private Vector3 fullScale;
        private Vector3 fullPosition;
        private bool hasFullSize;
        private Health health;
        private Gradient fillGradient;
        private bool hideWhenEmpty;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 체력과 공유 색상을 연결합니다. 풀 재사용 때에도 새 체력으로 다시 연결하며 실패하면 false입니다.
        /// </summary>
        public bool Bind(Health ownerHealth, ColorData colors, HealthBarType type, bool hideEmpty)
        {
            if (ownerHealth == null || colors == null)
            {
                SWLog.LogWarning("[HealthBar] 연결 실패: 대상 체력과 ColorData가 필요합니다.");
                return false;
            }

            if (!hasFullSize)
            {
                fullScale = fill.transform.localScale;
                fullPosition = fill.transform.localPosition;
                hasFullSize = true;
            }

            Unbind();
            health = ownerHealth;
            health.Changed += Refresh;
            fillGradient = colors.GetHealthFill(type);
            background.color = colors.GetHealthBackground(type);
            hideWhenEmpty = hideEmpty;
            Refresh();
            return true;
        }

        /// <summary>
        /// 체력 비율에 맞춰 색상과 채움 크기를 변경하고 한쪽 끝을 고정합니다.
        /// </summary>
        private void Refresh()
        {
            float fraction = Mathf.Clamp01(health.Fraction);
            fill.color = fillGradient.Evaluate(fraction);
            background.enabled = !hideWhenEmpty || health.IsAlive;
            fill.enabled = health.IsAlive;
            Vector3 scale = fullScale;
            Vector3 position = fullPosition;
            // 원본 피벗과 관계없이 가로는 왼쪽, 세로는 아래쪽 끝을 고정합니다.
            if (vertical)
            {
                scale.y *= fraction;
                position.y += fill.sprite.bounds.min.y * (fullScale.y - scale.y);
            }
            else
            {
                scale.x *= fraction;
                position.x += fill.sprite.bounds.min.x * (fullScale.x - scale.x);
            }

            fill.transform.localScale = scale;
            fill.transform.localPosition = position;
        }

        /// <summary>
        /// 이전 체력의 변경 알림 구독을 해제합니다.
        /// </summary>
        private void Unbind()
        {
            if (health != null)
            {
                health.Changed -= Refresh;
                health = null;
            }
        }

        /// <summary>
        /// 제거될 때 체력 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
        }

        #endregion // 함수
    }
}
