using System;
using UnityEngine;

using SW.Pooling;
using SW.Util;

using ProjectT.Data;
using ProjectT.Economy;
using ProjectT.Navigation;
using ProjectT.Units;

namespace ProjectT.Deployment
{
    /// <summary>
    /// 목적지와 잔액을 확인한 뒤 아군을 구매합니다. 클래스별 구매 횟수 제한을 두지 않습니다.
    /// </summary>
    public sealed class CharacterDeploymentService
    {
        #region 필드
        private readonly BattleDeploymentWallet wallet;
        private readonly WalkableBattlefield battlefield;
        private readonly SWPool pool;
        private readonly Transform parent;
        private bool purchasing;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 전투 하나의 지갑, 통행 영역과 개체 풀을 연결합니다.
        /// </summary>
        private CharacterDeploymentService(
            BattleDeploymentWallet battleWallet,
            WalkableBattlefield terrain,
            SWPool objectPool,
            Transform unitParent)
        {
            wallet = battleWallet;
            battlefield = terrain;
            pool = objectPool;
            parent = unitParent;
        }

        /// <summary>
        /// 배치에 필요한 참조를 검증하여 생성하며 누락 시 null을 반환합니다.
        /// </summary>
        public static CharacterDeploymentService Create(
            BattleDeploymentWallet battleWallet,
            WalkableBattlefield terrain,
            SWPool objectPool,
            Transform unitParent)
        {
            if (battleWallet == null || terrain == null || objectPool == null || unitParent == null)
            {
                SWLog.LogWarning("[CharacterDeploymentService] 생성 실패: 지갑·전장·풀·배치 부모 참조를 확인해 주세요.");
                return null;
            }

            return new CharacterDeploymentService(battleWallet, terrain, objectPool, unitParent);
        }

        /// <summary>
        /// 지정한 통행 가능 위치에 바로 생성합니다. 생성이 완료된 개체만 결제하며 실패 시 개체를 반납합니다.
        /// </summary>
        public bool TryDeploy(UnitClassData definition, Vector2 destination, out CharacterUnit unit, out string reason)
        {
            unit = null;
            reason = null;
            if (purchasing)
            {
                reason = "배치를 처리하고 있습니다.";
                return false;
            }

            if (definition == null || !definition.IsValid)
            {
                reason = "클래스 설정을 확인해 주세요.";
                return false;
            }

            if (wallet.Balance < definition.DeploymentCost)
            {
                reason = "배치 재화가 부족합니다.";
                return false;
            }

            if (!battlefield.IsWalkable(destination))
            {
                reason = "그곳에는 배치할 수 없습니다.";
                return false;
            }

            purchasing = true;
            GameObject instance = null;
            bool committed = false;
            try
            {
                instance = pool.Spawn(definition.Prefab, destination, Quaternion.identity, parent);
                if (instance == null)
                {
                    reason = "아군을 생성하지 못했습니다.";
                    return false;
                }

                CharacterUnit created = instance.GetComponent<CharacterUnit>();
                if (created == null || !created.Initialize(definition, battlefield, destination))
                {
                    reason = "아군을 초기화하지 못했습니다.";
                    return false;
                }

                if (!wallet.TrySpend(definition.DeploymentCost))
                {
                    reason = "배치 재화가 부족합니다.";
                    return false;
                }

                committed = true;
                unit = created;
                return true;
            }
            finally
            {
                if (!committed && instance != null)
                {
                    pool.Release(instance);
                }

                purchasing = false;
            }
        }

        #endregion // 함수
    }
}
