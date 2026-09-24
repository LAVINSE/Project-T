using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;

using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 한 종류의 보유 아이템과 수량을 표시하고 마우스 올림·집기·끌기 입력을 전달합니다.
    /// </summary>
    public sealed class InventoryItemSlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region 필드
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text fallbackText;
        [SerializeField] private GameObject selectionObject;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 현재 표시 중인 보유 종류입니다. 빈 슬롯은 null입니다.
        /// </summary>
        public InventoryStack Item { get; private set; }

        /// <summary>
        /// 상세 표시 요청입니다. 빈 슬롯에서는 발생하지 않습니다.
        /// </summary>
        public event Action<InventoryItemSlotUI, PointerEventData> Hovered;

        /// <summary>
        /// 마우스가 슬롯을 벗어날 때 발생합니다.
        /// </summary>
        public event Action Exited;

        /// <summary>
        /// 왼쪽 클릭 또는 끌기 시작의 집기 요청입니다.
        /// </summary>
        public event Action<InventoryItemSlotUI, PointerEventData> Picked;

        /// <summary>
        /// 끌기를 끝낸 화면 위치를 전달합니다.
        /// </summary>
        public event Action<PointerEventData> Dropped;

        #endregion // 프로퍼티

        #region 표시
        /// <summary>
        /// 아이콘·수량을 갱신합니다. 빈 슬롯은 배경만 표시하고, 아이템에 아이콘이 없으면 이름을 대신 표시합니다.
        /// </summary>
        public void Present(InventoryStack item)
        {
            Item = item;
            iconImage.sprite = item?.Icon;
            iconImage.enabled = iconImage.sprite != null;
            countText.text = item == null ? string.Empty : item.Count.ToString("N0");
            fallbackText.text = item == null || iconImage.enabled || item.Definition?.Equipment != null
                ? string.Empty : item.DisplayName;
            Select(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 집은 아이템에 선택 표시를 켭니다.
        /// </summary>
        public void Select(bool selected)
        {
            selectionObject.SetActive(selected);
        }

        #endregion // 표시

        #region 입력
        /// <summary>
        /// 보유 아이템의 마우스 올림을 전달합니다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Item != null)
            {
                Hovered?.Invoke(this, eventData);
            }
        }

        /// <summary>
        /// 상세 표시 종료를 전달합니다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            Exited?.Invoke();
        }

        /// <summary>
        /// 왼쪽 클릭으로 아이템을 집습니다. 빈 슬롯을 누르면 기존에 집은 아이템을 인벤토리 안에 놓도록 전달합니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.dragging && eventData.button == PointerEventData.InputButton.Left)
            {
                if (Item != null)
                {
                    Picked?.Invoke(this, eventData);
                }
                else
                {
                    Dropped?.Invoke(eventData);
                }
            }
        }

        /// <summary>
        /// 왼쪽 끌기를 시작하면 아이템을 집습니다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Item != null)
            {
                Picked?.Invoke(this, eventData);
            }
        }

        /// <summary>
        /// 끌기 대상으로 유지합니다. 이동 표시는 인벤토리의 포인터 입력 한 곳에서 처리합니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 왼쪽 끌기를 끝내면 놓은 위치를 전달합니다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Item != null)
            {
                Dropped?.Invoke(eventData);
            }
        }

        #endregion // 입력
    }
}
