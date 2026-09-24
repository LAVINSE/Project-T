using System.Collections.Generic;

using SW.Util;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 전투의 실제 경과 시간과 개체별 기록을 수집합니다. 결과 확정 후 모든 수치를 고정하며 저장·보상 지급은 하지 않습니다.
    /// </summary>
    public sealed class BattleStatistics
    {
        #region 필드
        private readonly List<CharacterBattleStatistics> characters = new List<CharacterBattleStatistics>();
        private readonly Dictionary<CharacterUnit, CharacterBattleStatistics> records =
            new Dictionary<CharacterUnit, CharacterBattleStatistics>();
        private readonly Dictionary<UnitClassData, int> classCounts = new Dictionary<UnitClassData, int>();
        private double previousClock;
        private bool hasClock;
        private bool counting;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 배치 순서로 정렬한 개체별 기록입니다. 외부에서 목록을 수정할 수 없습니다.
        /// </summary>
        public IReadOnlyList<CharacterBattleStatistics> Characters { get; }

        /// <summary>
        /// 정지·준비·휴식을 제외한 실제 초입니다. 배속을 곱하지 않습니다.
        /// </summary>
        public double ElapsedSeconds { get; private set; }

        /// <summary>
        /// 승패와 능력치가 확정되었는지 반환합니다.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 확정된 승패입니다. 완료 전에는 결과로 사용하지 않습니다.
        /// </summary>
        public BattlePhase Outcome { get; private set; }

        /// <summary>
        /// 결과를 확정한 스테이지 이름입니다.
        /// </summary>
        public string StageName { get; private set; }

        /// <summary>
        /// 마지막으로 진입한 라운드입니다.
        /// </summary>
        public int RoundNumber { get; private set; }

        /// <summary>
        /// 스테이지의 전체 라운드 수입니다.
        /// </summary>
        public int RoundCount { get; private set; }

        /// <summary>
        /// 전체 처치 수입니다.
        /// </summary>
        public int KilledCount { get; private set; }

        /// <summary>
        /// 스테이지가 정의한 전체 적 수입니다.
        /// </summary>
        public int TotalEnemyCount { get; private set; }

        /// <summary>
        /// 결과 확정 시 남은 공방 체력입니다.
        /// </summary>
        public float WorkshopHealth { get; private set; }

        /// <summary>
        /// 결과 확정 시 공방 최대 체력입니다.
        /// </summary>
        public float WorkshopMaximumHealth { get; private set; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 새 전투의 빈 기록과 외부 읽기 전용 목록을 준비합니다.
        /// </summary>
        public BattleStatistics()
        {
            Characters = characters.AsReadOnly();
        }

        #endregion // 초기화

        #region 수집
        /// <summary>
        /// 새로 배치한 개체를 한 번만 등록합니다. 생성 실패 시 목록과 순번을 바꾸지 않습니다.
        /// </summary>
        public void Register(CharacterUnit character)
        {
            if (IsCompleted || character == null || records.ContainsKey(character))
            {
                return;
            }

            if (character.Definition == null)
            {
                SWLog.LogWarning("[BattleStatistics] 등록 실패: 캐릭터 정의가 없습니다.");
                return;
            }

            classCounts.TryGetValue(character.Definition, out int count);
            CharacterBattleStatistics record = CharacterBattleStatistics.Create(character, count + 1);
            if (record == null)
            {
                return;
            }

            classCounts[character.Definition] = count + 1;
            records.Add(character, record);
            characters.Add(record);
        }

        /// <summary>
        /// 등록한 개체의 타격 결과만 반영합니다. 결과 확정 후에는 무시합니다.
        /// </summary>
        public void RecordHit(CharacterUnit character, float damage, bool killed)
        {
            if (!IsCompleted && character != null && records.TryGetValue(character, out CharacterBattleStatistics record))
            {
                record.RecordHit(damage, killed);
            }
        }

        /// <summary>
        /// 상태 전환 시 이전 구간의 실제 시간을 누적합니다. 비정상 시각은 기존 시간을 보존합니다.
        /// </summary>
        public void SynchronizeClock(double currentClock, bool shouldCount)
        {
            if (IsCompleted || !currentClock.ExIsNonNegative())
            {
                return;
            }

            if (hasClock && counting && currentClock >= previousClock)
            {
                ElapsedSeconds += currentClock - previousClock;
            }

            previousClock = currentClock;
            hasClock = true;
            counting = shouldCount;
        }

        /// <summary>
        /// 유효한 전투 결과와 종료 능력치를 한 번만 확정합니다. 입력이 없거나 승패가 아니면 false입니다.
        /// </summary>
        public bool Complete(
            BattlePhase outcome,
            StageData stage,
            int roundNumber,
            int killedCount,
            Workshop workshop)
        {
            if (IsCompleted)
            {
                return true;
            }

            if (stage == null || workshop == null
                || (outcome != BattlePhase.Victory && outcome != BattlePhase.Defeat))
            {
                SWLog.LogWarning("[BattleStatistics] 결과 확정 실패: 스테이지·공방·승패를 확인하세요.");
                return false;
            }

            Outcome = outcome;
            StageName = stage.DisplayName;
            RoundNumber = roundNumber;
            RoundCount = stage.RoundCount;
            KilledCount = killedCount;
            TotalEnemyCount = stage.TotalEnemyCount;
            WorkshopHealth = workshop.Health.Current;
            WorkshopMaximumHealth = workshop.Health.Maximum;
            foreach (CharacterBattleStatistics character in characters)
            {
                character.Complete();
            }

            records.Clear();
            classCounts.Clear();
            counting = false;
            IsCompleted = true;
            return true;
        }

        #endregion // 수집
    }
}
