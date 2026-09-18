using UnityEngine;

using SW.Base;
using SW.Pooling;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 공격 방향과 타격을 짧게 보여 주고 풀로 반환합니다.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "AttackTrace")]
    public sealed class AttackTrace : SWMonoBehaviour
    {
        #region 필드
        private SWPool pool;
        private LineRenderer line;
        private float remaining;
        private float duration;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 대여 때마다 위치·색·수명을 모두 초기화합니다.
        /// </summary>
        public void Show(SWPool owner, Vector2 start, Vector2 end, bool ranged)
        {
            pool = owner;
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = ranged ? new Color(0.3f, 1f, 0.8f) : new Color(1f, 0.86f, 0.45f);
            line.endColor = Color.white;
            duration = ranged ? 0.2f : 0.12f;
            remaining = duration;
            line.widthMultiplier = 0.07f;
        }

        /// <summary>
        /// 공격 궤적의 표시 시간을 진행하고 종료 시 풀에 반환합니다.
        /// </summary>
        private void Update()
        {
            if (pool == null)
            {
                return;
            }

            remaining -= Time.deltaTime;
            line.widthMultiplier = 0.07f * Mathf.Clamp01(remaining / duration);
            if (remaining <= 0f)
            {
                SWPool owner = pool;
                pool = null;
                owner.Release(gameObject);
            }
        }

        #endregion // 함수
    }
}
