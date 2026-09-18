using UnityEngine;

using SW.Base;
using SW.Pooling;

using ProjectT.Battle;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 전투의 공격 요청을 SWUtils 풀의 짧은 섬광으로 표시합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattleAttackPresentation")]
    public sealed class BattleAttackPresentation : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleSession session;
        private SWPool pool;
        [SerializeField] private AttackTrace tracePrefab;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 전투의 타격 효과 요청을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            pool = SWPool.Instance;
            session.Attacked += OnAttacked;
        }

        /// <summary>
        /// 전투의 타격 효과 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            session.Attacked -= OnAttacked;
        }

        /// <summary>
        /// 타격 위치와 공격 종류에 맞는 궤적을 풀에서 생성합니다.
        /// </summary>
        private void OnAttacked(Vector2 start, Vector2 end, bool ranged)
        {
            var trace = pool.Spawn<AttackTrace>(tracePrefab.gameObject);
            trace.Show(pool, start, end, ranged);
        }

        #endregion // 함수
    }
}
