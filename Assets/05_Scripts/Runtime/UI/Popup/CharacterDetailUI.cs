using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

using SW.Base;

using ProjectT.Battle;

namespace ProjectT.UI
{
    /// <summary>
    /// 결과 시점의 성장·능력치와 공격별 DPS를 표시합니다. 능력치 줄 수는 데이터가 설정한 스탯 수를 따릅니다.
    /// </summary>
    public sealed class CharacterDetailUI : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text experienceText;
        [SerializeField] private Image experienceFillImage;
        [SerializeField] private TMP_Text totalDamagePerSecondText;
        [SerializeField] private InfoStatRowUI statRowPrefab;
        [SerializeField] private InfoStatRowUI[] statRows;
        [SerializeField] private SkillInfoUI basicAttack;
        [SerializeField] private SkillInfoUI[] skillSlots;
        [SerializeField] private ScrollRect statsScroll;
        [SerializeField] private ScrollRect attacksScroll;
        private readonly List<InfoStatRowUI> rows = new List<InfoStatRowUI>();

        #endregion // 필드

        #region 표시
        /// <summary>
        /// 확정된 기록을 표시하고 상세 스크롤을 처음으로 돌립니다.
        /// </summary>
        public void Present(CharacterBattleStatistics record)
        {
            nameText.text = record.DisplayName + " · Lv." + record.Level;
            portraitImage.sprite = record.Portrait;
            portraitImage.enabled = record.Portrait != null;
            experienceText.text = record.Experience.ExToExperienceText(record.RequiredExperience);
            experienceFillImage.fillAmount = record.Experience.ExToExperienceFraction(record.RequiredExperience);
            PresentStats(record);
            totalDamagePerSecondText.text = "총합 DPS  " + record.TotalDamagePerSecond.ToString("N2");
            basicAttack.Present("평타", record.BasicAttackDamagePerSecond, record.BasicAttackHitCount, true, record.Portrait);
            for (int index = 0; index < skillSlots.Length; index++)
            {
                skillSlots[index].Present("스킬 " + (index + 1) + " · 미설정", null, null, false, null);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(statsScroll.content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(attacksScroll.content);
            statsScroll.StopMovement();
            attacksScroll.StopMovement();
            statsScroll.verticalNormalizedPosition = 1f;
            attacksScroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 체력을 첫 줄로 표시하고 나머지 능력치를 기록 순서대로 이어 붙입니다. 줄이 모자라면 추가합니다.
        /// </summary>
        private void PresentStats(CharacterBattleStatistics record)
        {
            if (rows.Count == 0)
            {
                rows.AddRange(statRows);
            }

            int used = 0;
            PresentRow(used++, "체력",
                record.CurrentHealth.ToString("0.##") + " / " + record.MaximumHealth.ToString("0.##"));
            foreach (CharacterBattleStatistics.StatDisplay stat in record.Stats)
            {
                PresentRow(used++, stat.Title, stat.Value);
            }

            for (int index = used; index < rows.Count; index++)
            {
                rows[index].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 지정한 자리의 줄에 이름과 값을 표시합니다. 자리가 없으면 줄을 새로 만듭니다.
        /// </summary>
        private void PresentRow(int index, string title, string value)
        {
            if (index == rows.Count)
            {
                rows.Add(Instantiate(statRowPrefab, statsScroll.content));
            }

            InfoStatRowUI row = rows[index];
            row.Present(title, value);
            row.gameObject.SetActive(true);
        }

        #endregion // 표시
    }
}
