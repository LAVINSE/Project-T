using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Popup;

using ProjectT.Battle;
using ProjectT.Data;

namespace ProjectT.UI
{
    /// <summary>
    /// 확정된 전투 결과의 목록과 개체 상세를 같은 팝업에서 전환합니다. 거점 연결 전에는 이동 버튼을 비활성화합니다.
    /// </summary>
    public sealed class BattleResultPopupUI : SWPopupBase
    {
        #region 필드
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMP_Text outcomeText;
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text workshopText;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private ScrollRect characterScroll;
        [SerializeField] private RectTransform characterContent;
        [SerializeField] private CharacterDPSRowUI characterRowPrefab;
        [SerializeField] private CharacterDetailUI characterDetail;
        [SerializeField] private Button backButton;
        [SerializeField] private Button hubButton;
        [SerializeField] private TMP_Text hubText;
        [SerializeField] private CharacterDPSRowUI[] authoredRows;
        [SerializeField] private Button damageSortButton;
        [SerializeField] private Button killsSortButton;
        [SerializeField] private Image resultImage;
        [SerializeField] private Image[] swordsImages;
        [SerializeField] private Sprite selectedSortSprite;
        [SerializeField] private Sprite unselectedSortSprite;
        private readonly List<CharacterBattleStatistics> orderedCharacters = new List<CharacterBattleStatistics>();
        private readonly List<CharacterDPSRowUI> rows = new List<CharacterDPSRowUI>();
        private SpriteData spriteData;
        private BattleStatistics displayedResult;
        private float listPosition = 1f;
        private Action returnToHub;
        private bool subscribed;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 버튼 알림을 연결하고 제작된 행을 목록에 등록합니다. 거점 이동은 조립 지점이 처리합니다. 이미 연결했으면 다시 구독하지 않습니다.
        /// </summary>
        public void Initialize(SpriteData sprites, Action hubRequested)
        {
            spriteData = sprites;
            if (subscribed)
            {
                return;
            }

            returnToHub = hubRequested;
            hubButton.onClick.AddListener(RequestHub);
            backButton.onClick.AddListener(ShowSummary);
            damageSortButton.onClick.AddListener(SortByDamage);
            killsSortButton.onClick.AddListener(SortByKills);
            foreach (CharacterDPSRowUI row in authoredRows)
            {
                if (!rows.Contains(row))
                {
                    rows.Add(row);
                }
            }

            hubText.text = "거점으로 돌아가기";
            subscribed = true;
            Hide();
        }

        /// <summary>
        /// 거점 이동을 한 번만 요청합니다. 장면 전환 중 반복 클릭은 무시합니다.
        /// </summary>
        private void RequestHub()
        {
            hubButton.interactable = false;
            returnToHub?.Invoke();
        }

        #endregion // 초기화

        #region 결과 표시
        /// <summary>
        /// 확정된 결과만 한 번 구성하여 엽니다. 같은 결과를 반복 요청하면 페이지와 스크롤을 유지합니다.
        /// </summary>
        public void Present(BattleStatistics statistics)
        {
            if (statistics == null || !statistics.IsCompleted)
            {
                return;
            }

            if (displayedResult == statistics)
            {
                if (!IsVisible)
                {
                    Show();
                }

                return;
            }

            displayedResult = statistics;
            bool victory = statistics.Outcome == BattlePhase.Victory;
            if (spriteData != null)
            {
                resultImage.sprite = spriteData.GetResultSprite(victory);
                foreach (Image swords in swordsImages)
                {
                    swords.sprite = spriteData.GetSwordsIcon(victory);
                }
            }

            outcomeText.text = victory ? "승리" : "패배";
            stageText.text = statistics.StageName;
            roundText.text = statistics.RoundNumber + " / " + statistics.RoundCount;
            killsText.text = statistics.KilledCount + " / " + statistics.TotalEnemyCount;
            timeText.text = statistics.ElapsedSeconds.ExToElapsedTimeText();
            workshopText.text = statistics.WorkshopHealth.ToString("0.##")
                + " / " + statistics.WorkshopMaximumHealth.ToString("0.##");
            SortByDamage();

            emptyText.gameObject.SetActive(statistics.Characters.Count == 0);
            listPosition = 1f;
            Show();
            transform.SetAsLastSibling();
            ShowSummary();
        }

        /// <summary>
        /// 정렬된 개체 기록을 제작된 행에 연결하고 부족한 행만 추가합니다.
        /// </summary>
        private void RefreshRows()
        {
            double highestDamage = 0d;
            foreach (CharacterBattleStatistics character in orderedCharacters)
            {
                highestDamage = System.Math.Max(highestDamage, character.DamageDealt);
            }

            for (int index = 0; index < orderedCharacters.Count; index++)
            {
                if (index == rows.Count)
                {
                    rows.Add(Instantiate(characterRowPrefab, characterContent));
                }

                CharacterDPSRowUI row = rows[index];
                row.Present(orderedCharacters[index], ShowCharacter, highestDamage);
                row.gameObject.SetActive(true);
            }

            for (int index = orderedCharacters.Count; index < rows.Count; index++)
            {
                rows[index].gameObject.SetActive(false);
            }

            listPosition = 1f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(characterContent);
            characterScroll.StopMovement();
            characterScroll.verticalNormalizedPosition = listPosition;
        }

        /// <summary>
        /// 누적 피해가 큰 순서로 표시합니다. 동점이면 원래 배치 순서를 유지합니다.
        /// </summary>
        private void SortByDamage()
        {
            ApplySort(false);
        }

        /// <summary>
        /// 처치 수가 큰 순서로 표시합니다. 동점이면 원래 배치 순서를 유지합니다.
        /// </summary>
        private void SortByKills()
        {
            ApplySort(true);
        }

        /// <summary>
        /// 선택한 기록 기준으로 안정 정렬하고 버튼의 선택 상태와 목록을 갱신합니다.
        /// </summary>
        private void ApplySort(bool byKills)
        {
            if (displayedResult == null)
            {
                return;
            }

            orderedCharacters.Clear();
            orderedCharacters.AddRange(displayedResult.Characters);
            for (int index = 1; index < orderedCharacters.Count; index++)
            {
                CharacterBattleStatistics current = orderedCharacters[index];
                double value = byKills ? current.KillCount : current.DamageDealt;
                int position = index - 1;
                while (position >= 0
                    && (byKills ? orderedCharacters[position].KillCount : orderedCharacters[position].DamageDealt) < value)
                {
                    orderedCharacters[position + 1] = orderedCharacters[position];
                    position--;
                }

                orderedCharacters[position + 1] = current;
            }

            if (selectedSortSprite != null && unselectedSortSprite != null)
            {
                if (damageSortButton.targetGraphic is Image damageImage)
                {
                    damageImage.sprite = byKills ? unselectedSortSprite : selectedSortSprite;
                }

                if (killsSortButton.targetGraphic is Image killsImage)
                {
                    killsImage.sprite = byKills ? selectedSortSprite : unselectedSortSprite;
                }
            }

            RefreshRows();
        }

        /// <summary>
        /// 목록 위치를 보관하고 선택한 개체의 상세 페이지로 바꿉니다.
        /// </summary>
        private void ShowCharacter(CharacterBattleStatistics character)
        {
            characterDetail.Present(character);
            characterScroll.StopMovement();
            listPosition = characterScroll.verticalNormalizedPosition;
            summaryPanel.SetActive(false);
            detailPanel.SetActive(true);
        }

        /// <summary>
        /// 통계 목록으로 돌아가 이전 스크롤 위치를 복원합니다.
        /// </summary>
        public void ShowSummary()
        {
            detailPanel.SetActive(false);
            summaryPanel.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(characterContent);
            characterScroll.StopMovement();
            characterScroll.verticalNormalizedPosition = listPosition;
        }

        #endregion // 결과 표시

        #region 정리
        /// <summary>
        /// 팝업이 제거되면 버튼 연결과 결과 참조를 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (subscribed)
            {
                hubButton.onClick.RemoveListener(RequestHub);
                backButton.onClick.RemoveListener(ShowSummary);
                damageSortButton.onClick.RemoveListener(SortByDamage);
                killsSortButton.onClick.RemoveListener(SortByKills);
            }

            rows.Clear();
            orderedCharacters.Clear();
            displayedResult = null;
        }

        #endregion // 정리
    }
}
