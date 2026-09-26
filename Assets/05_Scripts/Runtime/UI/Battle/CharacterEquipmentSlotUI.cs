using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using ProjectT.Data;
using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// HUD의 장비 슬롯을 표시하고 장착 요청과 우클릭 해제를 조립 지점에 전달합니다.
    /// </summary>
    public sealed class CharacterEquipmentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        #region 필드
        [SerializeField] private Image gradeBoxImage;
        [SerializeField] private Image itemImage;
        private Func<int, string, bool> equip;
        private int slotIndex;
        private Sprite emptySprite;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 슬롯 번호와 선택 캐릭터의 장비 변경 요청을 연결합니다.
        /// </summary>
        public void Initialize(int index, Func<int, string, bool> equipRequested)
        {
            slotIndex = index;
            equip = equipRequested;
            emptySprite = gradeBoxImage.sprite;
        }

        #endregion // 초기화

        #region 표시와 입력
        /// <summary>
        /// 현재 장비의 아이콘과 희귀도 외형을 표시합니다. 빈 슬롯은 일반 상자로 표시합니다.
        /// </summary>
        public void Present(InventoryStack item, SpriteData sprites)
        {
            itemImage.sprite = item?.Icon;
            itemImage.enabled = itemImage.sprite != null;
            gradeBoxImage.sprite = item == null ? emptySprite : sprites.GetGradeBox(item.Definition.Equipment.Rarity);
        }

        /// <summary>
        /// 인벤토리에서 놓은 한 개를 장착합니다. 연결 전 또는 장착 불가이면 false입니다.
        /// </summary>
        public bool TryEquip(string identifier)
        {
            return equip != null && equip(slotIndex, identifier);
        }

        /// <summary>
        /// 우클릭으로 선택 슬롯의 장비를 해제하여 인벤토리로 반환합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                TryEquip(null);
            }
        }

        #endregion // 표시와 입력
    }
}
