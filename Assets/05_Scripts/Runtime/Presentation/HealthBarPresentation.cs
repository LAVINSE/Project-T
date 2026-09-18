using System;
using UnityEngine;

using SW.Base;
using SW.Util;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 사용자가 만든 가로·세로 체력바 프리팹을 실제 체력과 공유 색상 설정으로 표시합니다.
    /// </summary>
    public sealed class HealthBarPresentation : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 공유 색상 데이터에서 선택할 체력바 종류입니다.
        /// </summary>
        public enum OwnerKind
        {
            ArcaneWorkshop,
            Character,
            Enemy
        }

        #endregion // 데이터

        #region 필드
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private bool vertical;
        private Vector3 fullScale;
        private Vector3 fullPosition;
        private bool initialized;
        private CombatHealth health;
        private ColorData colors;
        private OwnerKind ownerKind;
        private bool hideWhenEmpty;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 표시하는 체력 비율입니다.
        /// </summary>
        public float Fraction => health != null ? Mathf.Clamp01(health.Current / health.Maximum) : 0f;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 체력과 공유 색상을 연결합니다. 풀 재사용 때에도 새 개체의 체력으로 갱신합니다.
        /// </summary>
        public bool Bind(CombatHealth ownerHealth, ColorData colorData, OwnerKind kind, bool hideEmpty)
        {
            if (ownerHealth == null)
            {
                SWLog.LogWarning("[HealthBarPresentation] 연결 실패: 대상 체력이 없습니다.");
                return false;
            }

            if (colorData == null)
            {
                SWLog.LogWarning("[HealthBarPresentation] 연결 실패: ColorData가 없습니다.");
                return false;
            }

            if (background == null || fill == null || fill.sprite == null)
            {
                SWLog.LogWarning("[HealthBarPresentation] 연결 실패: 배경·채움 렌더러와 채움 스프라이트를 확인해 주세요.");
                return false;
            }

            if (!initialized)
            {
                fullScale = fill.transform.localScale;
                fullPosition = fill.transform.localPosition;
                initialized = true;
            }

            health = ownerHealth;
            colors = colorData;
            ownerKind = kind;
            hideWhenEmpty = hideEmpty;
            Refresh();
            return true;
        }

        /// <summary>
        /// 연결된 체력과 색상 데이터로 표시 상태를 갱신합니다.
        /// </summary>
        private void LateUpdate()
        {
            if (health != null && colors != null)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 체력 비율에 맞춰 색상과 채움 크기를 변경하고 한쪽 끝을 고정합니다.
        /// </summary>
        private void Refresh()
        {
            float fraction = Fraction;
            Color backgroundColor;
            Gradient gradient;
            switch (ownerKind)
            {
                case OwnerKind.ArcaneWorkshop:
                    backgroundColor = colors.ArcaneHpBackgroundColor;
                    gradient = colors.ArcaneFillColorGradient;
                    break;
                case OwnerKind.Character:
                    backgroundColor = colors.CharacterHpBackgroundColor;
                    gradient = colors.CharacterFillColorGradient;
                    break;
                default:
                    backgroundColor = colors.EnemyHpBackgroundColor;
                    gradient = colors.EnemyFillColorGradient;
                    break;
            }

            background.color = backgroundColor;
            fill.color = gradient.Evaluate(fraction);
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

        #endregion // 함수
    }
}
