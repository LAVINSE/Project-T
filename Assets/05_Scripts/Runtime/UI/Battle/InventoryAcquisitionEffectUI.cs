using UnityEngine;

using DG.Tweening;

using SW.Base;
using SW.Util;

using ProjectT.Inventory;

namespace ProjectT.UI
{
    /// <summary>
    /// 저장이 완료된 특별 전리품 획득을 받아 인벤토리 아이콘을 확대·복귀합니다. 지급과 저장은 변경하지 않습니다.
    /// </summary>
    public sealed class InventoryAcquisitionEffectUI : SWMonoBehaviour
    {
        #region 필드
        private InventoryStore inventory;
        private RectTransform iconTransform;
        private Vector3 originalScale;
        private Sequence acquisitionSequence;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 보관소의 획득 알림에 아이콘 연출을 연결합니다. 보관소가 없으면 연출을 켜지 않습니다.
        /// </summary>
        public void Initialize(InventoryStore source)
        {
            if (source == null)
            {
                SWLog.LogWarning("[InventoryAcquisitionEffectUI] 연출 중단: 보관소가 준비되지 않았습니다.");
                return;
            }

            var target = (RectTransform)transform;
            Release();
            inventory = source;
            iconTransform = target;
            originalScale = target.localScale;
            float halfDuration = ProjectDefine.Inventory.AcquisitionEffectDuration * 0.5f;
            acquisitionSequence = DOTween.Sequence();
            acquisitionSequence.SetAutoKill(false);
            acquisitionSequence.SetRecyclable(false);
            acquisitionSequence.SetUpdate(true);
            acquisitionSequence.Append(target.DOScale(
                originalScale * ProjectDefine.Inventory.AcquisitionEffectScale,
                halfDuration).SetEase(Ease.OutQuad));
            acquisitionSequence.Append(target.DOScale(originalScale, halfDuration).SetEase(Ease.InQuad));
            acquisitionSequence.Pause();
            inventory.SpecialLootAcquired += PlayAcquisition;
        }

        #endregion // 초기화

        #region 연출
        /// <summary>
        /// 새 획득마다 원래 크기부터 다시 재생합니다. 숨겨진 아이콘이나 초기화되지 않은 연출은 무시합니다.
        /// </summary>
        private void PlayAcquisition()
        {
            if (!isActiveAndEnabled || iconTransform == null || acquisitionSequence == null)
            {
                return;
            }

            ResetEffect();
            acquisitionSequence.Restart();
        }

        /// <summary>
        /// 진행 중인 연출을 되감고 원래 크기를 복원합니다. 초기화 전에는 아무것도 변경하지 않습니다.
        /// </summary>
        private void ResetEffect()
        {
            if (acquisitionSequence != null)
            {
                acquisitionSequence.Rewind();
            }

            if (iconTransform != null)
            {
                iconTransform.localScale = originalScale;
            }
        }

        #endregion // 연출

        #region 정리
        /// <summary>
        /// 화면이 숨겨지면 확대 상태를 남기지 않고 연출을 정지합니다.
        /// </summary>
        private void OnDisable()
        {
            ResetEffect();
        }

        /// <summary>
        /// 화면 제거 시 영구 인벤토리의 구독과 DOTween 연출을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 기존 구독·연출을 해제하고 아이콘을 복원합니다. 초기화 전이나 반복 호출에서도 안전합니다.
        /// </summary>
        private void Release()
        {
            if (inventory != null)
            {
                inventory.SpecialLootAcquired -= PlayAcquisition;
                inventory = null;
            }

            ResetEffect();
            if (acquisitionSequence != null)
            {
                acquisitionSequence.Kill();
                acquisitionSequence = null;
            }

            iconTransform = null;
        }

        #endregion // 정리
    }
}
