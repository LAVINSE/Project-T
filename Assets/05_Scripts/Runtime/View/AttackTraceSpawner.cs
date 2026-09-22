using UnityEngine;

using SW.Base;
using SW.Pooling;

using ProjectT.Battle;

namespace ProjectT.View
{
    /// <summary>
    /// 전투의 공격 알림을 SWUtils 풀의 짧은 공격 궤적으로 표시합니다.
    /// </summary>
    [RequireComponent(typeof(BattleManager))]
    public sealed class AttackTraceSpawner : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private AttackTrace tracePrefab;
        private BattleManager battle;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 같은 객체의 전투를 연결합니다.
        /// </summary>
        private void Awake()
        {
            battle = GetComponent<BattleManager>();
        }

        /// <summary>
        /// 전투의 공격 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            battle.Attacked += OnAttacked;
        }

        /// <summary>
        /// 전투의 공격 알림 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            battle.Attacked -= OnAttacked;
        }

        /// <summary>
        /// 타격 위치와 공격 종류에 맞는 궤적을 풀에서 생성합니다.
        /// </summary>
        private void OnAttacked(Vector2 start, Vector2 end, bool ranged)
        {
            AttackTrace trace = SWPool.Instance.Spawn<AttackTrace>(tracePrefab.gameObject);
            trace.Show(start, end, ranged);
        }

        #endregion // 함수
    }
}
