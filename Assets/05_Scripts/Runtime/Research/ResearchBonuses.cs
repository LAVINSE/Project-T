using System.Collections.Generic;

using SW.Stat;

using ProjectT.Units;

namespace ProjectT.Research
{
    /// <summary>
    /// 습득한 연구 노드가 제공하는 능력치 증가량을 모읍니다. 연구 효과가 기록하고 전투에 들어오는 아군이 생성될 때 읽습니다.
    /// </summary>
    public sealed class ResearchBonuses
    {
        #region 필드
        private readonly Dictionary<object, Entry> entries = new Dictionary<object, Entry>();

        #endregion // 필드

        #region 기록
        /// <summary>
        /// 한 효과 출처의 증가량을 교체합니다. 증가량이 0이거나 스탯이 없으면 그 출처를 제거합니다.
        /// </summary>
        public void Set(object source, SWStat stat, ResearchValueMode mode, float amount)
        {
            if (source == null)
            {
                return;
            }

            if (stat == null || amount == 0f)
            {
                entries.Remove(source);
                return;
            }

            entries[source] = new Entry(stat, mode, amount);
        }

        #endregion // 기록

        #region 적용
        /// <summary>
        /// 아군 한 명의 스탯 복제본에 연구 증가량을 한 번에 적용합니다. 비율은 그 아군의 클래스 기본값 기준이며 연결되지 않은 스탯은 건너뜁니다.
        /// </summary>
        public bool TryApply(RuntimeStatCollection stats)
        {
            if (stats == null)
            {
                return false;
            }

            var totals = new Dictionary<SWStat, float>();
            foreach (Entry entry in entries.Values)
            {
                if (!stats.Contains(entry.Stat))
                {
                    continue;
                }

                float amount = entry.Mode == ResearchValueMode.Percent
                    ? stats.GetDefaultValue(entry.Stat) * entry.Amount
                    : entry.Amount;
                totals.TryGetValue(entry.Stat, out float previous);
                totals[entry.Stat] = previous + amount;
            }

            return stats.TryReplaceBonuses(this, totals);
        }

        #endregion // 적용

        #region 항목
        /// <summary>
        /// 한 출처의 스탯·방식·증가량입니다.
        /// </summary>
        private readonly struct Entry
        {
            /// <summary>
            /// 올릴 스탯입니다.
            /// </summary>
            public SWStat Stat { get; }

            /// <summary>
            /// 고정값 또는 비율입니다.
            /// </summary>
            public ResearchValueMode Mode { get; }

            /// <summary>
            /// 레벨을 반영한 증가량입니다. 비율은 0.1이 10%입니다.
            /// </summary>
            public float Amount { get; }

            /// <summary>
            /// 값을 보관합니다.
            /// </summary>
            public Entry(SWStat stat, ResearchValueMode mode, float amount)
            {
                Stat = stat;
                Mode = mode;
                Amount = amount;
            }
        }

        #endregion // 항목
    }
}
