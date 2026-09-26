using UnityEngine;

using SW.Pooling;

using ProjectT.Data;
using ProjectT.Navigation;
using ProjectT.Research;
using ProjectT.Units;
using ProjectT.View;

namespace ProjectT.Battle
{
    /// <summary>
    /// 스테이지 클래스·잔액·통행 영역을 확인한 뒤 아군을 생성하고 결제합니다. 실패하면 생성한 개체를 풀에 반환하고 비용을 차감하지 않습니다.
    /// </summary>
    public sealed class DeploymentService
    {
        #region 필드
        private readonly StageData stage;
        private readonly BattleWallet wallet;
        private readonly WalkableBattlefield battlefield;
        private readonly Transform unitParent;
        private readonly ColorData colors;
        private readonly ResearchBonuses research;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 한 전투의 스테이지·지갑·전장·배치 부모·표시 색상과 공통 연구 증가량을 연결합니다. 연구가 없으면 null입니다.
        /// </summary>
        public DeploymentService(
            StageData stage,
            BattleWallet wallet,
            WalkableBattlefield battlefield,
            Transform unitParent,
            ColorData colors,
            ResearchBonuses research)
        {
            this.stage = stage;
            this.wallet = wallet;
            this.battlefield = battlefield;
            this.unitParent = unitParent;
            this.colors = colors;
            this.research = research;
        }

        #endregion // 초기화

        #region 배치
        /// <summary>
        /// 클래스·잔액·위치로 배치할 수 있는지 확인합니다. 불가하면 안내 문구를 반환합니다.
        /// </summary>
        public bool CanDeploy(UnitClassData unitClass, Vector2 position, out string reason)
        {
            reason = string.Empty;
            if (!stage.HasClass(unitClass) || !unitClass.IsValid)
            {
                reason = "이 스테이지에서 사용할 수 없는 클래스입니다.";
            }
            else if (wallet.Balance < unitClass.DeploymentCost)
            {
                reason = "배치 재화가 부족합니다.";
            }
            else if (!battlefield.IsWalkable(position))
            {
                reason = "그곳에는 배치할 수 없습니다.";
            }

            return string.IsNullOrEmpty(reason);
        }

        /// <summary>
        /// 검사를 통과한 위치에 아군을 생성·초기화한 뒤 결제합니다. 실패하면 null입니다.
        /// </summary>
        public CharacterUnit Deploy(UnitClassData unitClass, Vector2 position, out string reason)
        {
            if (!CanDeploy(unitClass, position, out reason))
            {
                return null;
            }

            SWPool pool = SWPool.Instance;
            CharacterUnit unit = pool.Spawn<CharacterUnit>(unitClass.Prefab, position, Quaternion.identity, unitParent);
            if (unit == null || !unit.Initialize(unitClass, battlefield, position, research) || !wallet.TrySpend(unitClass.DeploymentCost))
            {
                if (unit != null)
                {
                    pool.Release(unit.gameObject);
                }

                reason = "아군을 생성하지 못했습니다.";
                return null;
            }

            unit.GetComponent<UnitView>().Initialize(colors);
            return unit;
        }

        #endregion // 배치
    }
}
