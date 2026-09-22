using System.Collections;
using UnityEngine;

using SW.Base;
using SW.Pooling;

namespace ProjectT.View
{
    /// <summary>
    /// 공격 방향과 타격을 짧게 보여 준 뒤 스스로 풀에 반환합니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class AttackTrace : SWMonoBehaviour
    {
        #region 필드
        private const float LineWidth = 0.07f;
        private LineRenderer line;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 대여 때마다 위치·색·수명을 초기화하고 사라지는 연출을 시작합니다.
        /// </summary>
        public void Show(Vector2 start, Vector2 end, bool ranged)
        {
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = ranged ? ProjectDefine.Palette.RangedTrace : ProjectDefine.Palette.MeleeTrace;
            line.endColor = Color.white;
            line.widthMultiplier = LineWidth;
            StartCoroutine(FadeOut(ranged ? 0.2f : 0.12f));
        }

        /// <summary>
        /// 표시 시간 동안 선을 가늘게 만든 뒤 풀에 반환합니다.
        /// </summary>
        private IEnumerator FadeOut(float duration)
        {
            for (float remaining = duration; remaining > 0f; remaining -= Time.deltaTime)
            {
                line.widthMultiplier = LineWidth * remaining / duration;
                yield return null;
            }

            SWPool.Instance.Release(gameObject);
        }

        #endregion // 함수
    }
}
