using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Base;
using SW.Util;

using ProjectT.Battle;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 스테이지에서 구매할 수 있는 클래스 목록을 사용자 제작 슬롯으로 표시합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT", "ProjectT.Runtime", "CharacterSelectedUI")]
    public sealed class CharacterSelectedUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private RectTransform content;
        [SerializeField] private CharacterPurchaseSlot slotPrefab;
        [SerializeField] private CharacterPurchaseSlot[] authoredSlots = Array.Empty<CharacterPurchaseSlot>();
        private readonly List<CharacterPurchaseSlot> activeSlots = new List<CharacterPurchaseSlot>();
        private BattleSession session;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 목록 생성에 필요한 참조입니다.
        /// </summary>
        public bool HasRequiredReferences
        {
            get
            {
                if (content == null || slotPrefab == null || !slotPrefab.HasRequiredReferences)
                {
                    return false;
                }

                foreach (CharacterPurchaseSlot slot in authoredSlots)
                {
                    if (slot == null || !slot.HasRequiredReferences)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 현재 스테이지에서 실제 구매 가능한 슬롯 목록입니다.
        /// </summary>
        public IReadOnlyList<CharacterPurchaseSlot> ActiveSlots => activeSlots;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 클래스 목록을 검증한 후 기존 슬롯과 필요한 추가 슬롯을 연결합니다.
        /// </summary>
        public bool Initialize(BattleSession battle, BattlePlacementCommand placement)
        {
            if (!HasRequiredReferences || battle == null || placement == null || activeSlots.Count > 0)
            {
                SWLog.LogWarning("[CharacterSelectedUI] 초기화 실패: 참조 또는 중복 초기화를 확인해 주세요.");
                return false;
            }

            foreach (var definition in battle.Definition.Classes)
            {
                if (definition == null || !definition.IsValid)
                {
                    SWLog.LogWarning("[CharacterSelectedUI] 초기화 실패: 구매 클래스 데이터가 올바르지 않습니다.");
                    return false;
                }
            }

            session = battle;
            for (int index = 0; index < battle.Definition.Classes.Count; index++)
            {
                CharacterPurchaseSlot slot = index < authoredSlots.Length ? authoredSlots[index] : Instantiate(slotPrefab, content);
                slot.gameObject.SetActive(true);
                slot.Configure(battle.Definition.Classes[index], placement);
                activeSlots.Add(slot);
            }

            for (int index = battle.Definition.Classes.Count; index < authoredSlots.Length; index++)
            {
                authoredSlots[index].gameObject.SetActive(false);
            }

            RefreshAvailability();
            return true;
        }

        /// <summary>
        /// 재화와 전투 정지 상태에 따라 구매 입력을 갱신합니다.
        /// </summary>
        public void RefreshAvailability()
        {
            if (session == null || session.Wallet == null)
            {
                return;
            }

            foreach (CharacterPurchaseSlot slot in activeSlots)
            {
                slot.SetAvailable(session.CanCommand && session.Wallet.Balance >= slot.Definition.DeploymentCost);
            }
        }

        #endregion // 초기화
    }
}
