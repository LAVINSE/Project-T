using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;

using ProjectT.Battle;

namespace ProjectT.UI
{
    /// <summary>
    /// 스테이지에서 구매할 수 있는 클래스 목록을 사용자 제작 슬롯으로 표시합니다.
    /// </summary>
    public sealed class CharacterSelectedUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private RectTransform content;
        [SerializeField] private CharacterSlotUI slotPrefab;
        [SerializeField] private CharacterSlotUI[] authoredSlots = Array.Empty<CharacterSlotUI>();
        private readonly List<CharacterSlotUI> activeSlots = new List<CharacterSlotUI>();
        private BattleManager battle;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 스테이지 클래스 수만큼 제작된 슬롯을 사용하고 부족하면 추가 슬롯을 만듭니다.
        /// </summary>
        public void Initialize(BattleManager battleManager, BattleInput input)
        {
            battle = battleManager;
            var classes = battle.Stage.Classes;
            for (int index = 0; index < classes.Count; index++)
            {
                CharacterSlotUI slot = index < authoredSlots.Length ? authoredSlots[index] : Instantiate(slotPrefab, content);
                slot.gameObject.SetActive(true);
                slot.Configure(classes[index], input);
                activeSlots.Add(slot);
            }

            for (int index = classes.Count; index < authoredSlots.Length; index++)
            {
                authoredSlots[index].gameObject.SetActive(false);
            }

            RefreshAvailability();
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 재화와 전투 정지 상태에 따라 구매 입력을 갱신합니다.
        /// </summary>
        public void RefreshAvailability()
        {
            foreach (CharacterSlotUI slot in activeSlots)
            {
                slot.SetAvailable(battle.CanCommand && battle.Wallet.Balance >= slot.UnitClass.DeploymentCost);
            }
        }

        #endregion // 표시
    }
}
