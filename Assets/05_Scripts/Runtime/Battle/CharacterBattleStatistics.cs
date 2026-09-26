using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Stat;
using SW.Util;

using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 배치한 캐릭터 한 명의 기본 공격 기록과 결과 확정 시점의 능력치를 보관합니다.
    /// 사망·부활로 기록을 초기화하지 않으며 확정 후에는 변경하지 않습니다.
    /// </summary>
    public sealed class CharacterBattleStatistics
    {
        #region 필드
        private CharacterUnit unit;
        private float attackDamage;
        private float attackSpeed;
        private bool completed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 같은 클래스의 배치 순번을 포함한 개체 이름입니다.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// 결과 화면의 초상화입니다. 원본 그림이 없으면 null입니다.
        /// </summary>
        public Sprite Portrait { get; private set; }

        /// <summary>
        /// 실제로 감소시킨 적 체력의 누적 합입니다.
        /// </summary>
        public double DamageDealt { get; private set; }

        /// <summary>
        /// 마지막 타격으로 처치한 적 수입니다.
        /// </summary>
        public int KillCount { get; private set; }

        /// <summary>
        /// 피해 적용에 성공한 기본 공격의 적중 횟수입니다.
        /// </summary>
        public int BasicAttackHitCount { get; private set; }

        /// <summary>
        /// 결과 확정 시점의 현재 체력입니다.
        /// </summary>
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// 결과 확정 시점의 최대 체력입니다.
        /// </summary>
        public float MaximumHealth { get; private set; }

        /// <summary>
        /// 결과 확정 시점에 데이터가 설정한 순서대로 복사한 능력치입니다. 체력은 현재·최대를 함께 표시하므로 제외합니다.
        /// </summary>
        public IReadOnlyList<StatDisplay> Stats { get; private set; } = Array.Empty<StatDisplay>();

        /// <summary>
        /// 공격력과 공격 속도로 계산한 기본 공격 DPS입니다. 치명타·방어 등 전투 보조 능력치는 확률과 대상에 따라 달라지므로 포함하지 않습니다.
        /// </summary>
        public double BasicAttackDamagePerSecond => (double)attackDamage * attackSpeed;

        /// <summary>
        /// 현재 구현된 공격의 DPS 합계입니다. 등록된 스킬이 없으므로 기본 공격만 포함합니다.
        /// </summary>
        public double TotalDamagePerSecond => BasicAttackDamagePerSecond;

        /// <summary>
        /// 결과 확정 시점에 표시할 레벨입니다.
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// 결과 확정 시점에 표시할 현재 레벨의 경험치입니다.
        /// </summary>
        public double Experience { get; private set; }

        /// <summary>
        /// 다음 레벨의 경험치 기준이며 0이면 미설정입니다.
        /// </summary>
        public double RequiredExperience { get; private set; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 개체와 클래스 내 배치 순번을 연결합니다.
        /// </summary>
        private CharacterBattleStatistics(CharacterUnit character, int classNumber)
        {
            unit = character;
            DisplayName = character.Definition.DisplayName + " " + classNumber;
            Portrait = character.Definition.Portrait;
        }

        /// <summary>
        /// 초기화된 캐릭터의 기록을 만듭니다. 참조나 순번이 잘못되면 경고 후 null입니다.
        /// </summary>
        public static CharacterBattleStatistics Create(CharacterUnit character, int classNumber)
        {
            if (character == null || character.Definition == null || character.Health == null || classNumber < 1)
            {
                SWLog.LogWarning("[CharacterBattleStatistics] 생성 실패: 초기화된 캐릭터와 양수 순번이 필요합니다.");
                return null;
            }

            return new CharacterBattleStatistics(character, classNumber);
        }

        #endregion // 초기화

        #region 기록
        /// <summary>
        /// 유효한 기본 공격 한 번의 실제 피해와 처치를 누적합니다. 확정 후 또는 잘못된 피해는 무시합니다.
        /// </summary>
        internal void RecordHit(float damage, bool killed)
        {
            if (completed || !damage.ExIsPositive())
            {
                return;
            }

            DamageDealt += damage;
            BasicAttackHitCount++;
            if (killed)
            {
                KillCount++;
            }
        }

        /// <summary>
        /// 현재 능력치를 값으로 복사하고 개체 참조를 해제합니다. 반복 호출은 무시합니다.
        /// </summary>
        internal void Complete()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            if (unit != null && unit.Health != null && unit.Definition != null)
            {
                CurrentHealth = unit.Health.Current;
                MaximumHealth = unit.Health.Maximum;
                attackDamage = unit.AttackDamage;
                attackSpeed = unit.AttackSpeed;
                Level = unit.Definition.Level;
                Experience = unit.Definition.Experience;
                RequiredExperience = unit.Definition.RequiredExperience;
                Portrait = unit.Definition.Portrait;
                Stats = CaptureStats(unit);
            }

            unit = null;
        }

        /// <summary>
        /// 데이터가 설정한 순서대로 능력치 이름과 표시값을 복사합니다. 체력 스탯은 별도로 표시하므로 건너뜁니다.
        /// </summary>
        private static IReadOnlyList<StatDisplay> CaptureStats(CharacterUnit character)
        {
            if (character.Stats == null)
            {
                return Array.Empty<StatDisplay>();
            }

            SWStat healthStat = character.Definition.MaximumHealthStat;
            var captured = new List<StatDisplay>(character.Stats.RuntimeStats.Count);
            foreach (SWStat stat in character.Stats.RuntimeStats)
            {
                if (healthStat != null && stat.CodeName == healthStat.CodeName)
                {
                    continue;
                }

                captured.Add(new StatDisplay(stat.DisplayName, stat.GetDisplayValue()));
            }

            return captured;
        }

        #endregion // 기록

        #region 표시 기록
        /// <summary>
        /// 결과 화면에 한 줄로 표시할 능력치 이름과 값입니다.
        /// </summary>
        public readonly struct StatDisplay
        {
            /// <summary>
            /// 스탯 자산의 표시 이름입니다.
            /// </summary>
            public string Title { get; }

            /// <summary>
            /// 스탯 자산의 표시 형식을 적용한 값입니다. 백분율 스탯은 퍼센트로 표시합니다.
            /// </summary>
            public string Value { get; }

            /// <summary>
            /// 표시할 이름과 값을 보관합니다.
            /// </summary>
            public StatDisplay(string title, string value)
            {
                Title = title;
                Value = value;
            }
        }

        #endregion // 표시 기록
    }
}
