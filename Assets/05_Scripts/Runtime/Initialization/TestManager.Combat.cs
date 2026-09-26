using UnityEngine;

using SW.Attributes;
using SW.Stat;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Units;

namespace ProjectT.Initialization
{
    /// <summary>
    /// 전투 공식 확인용 수동 테스트입니다. 데이터 자산을 바꾸지 않고 실행 중 개체에만 임시 증가량을 더하며 전투가 끝나면 사라집니다.
    /// </summary>
    public sealed partial class TestManager
    {
        #region 전투 공식 테스트 필드
        [SWGroup("전투 공식 테스트")]
        [SerializeField, Min(0f)] private float allyCriticalChance = 1f;
        [SerializeField, Min(0f)] private float allyCriticalDamage;
        [SerializeField, Min(0f)] private float allyLifeSteal = 0.5f;
        [SerializeField, Min(0f)] private float allyArmorPenetration;
        [SerializeField, Min(0f)] private float allyDefense;
        [SerializeField, Min(0f)] private float allyEvasion;
        [SerializeField, Min(0f)] private float enemyDefense = 100f;
        [SerializeField, Min(0f)] private float enemyEvasion;
        [SerializeField] private bool logHits;
        [SerializeField, SWReadOnly, TextArea(3, 6)]
        private string combatTestStatus = "Play 후 아군을 선택하고 버튼으로 임시 능력치를 더합니다. 확률 1은 100%입니다.";
        private readonly object combatTestSource = new object();
        private bool enemyTestActive;

        #endregion // 전투 공식 테스트 필드

        #region 전투 공식 테스트 버튼
        /// <summary>
        /// 선택한 아군에게 입력한 임시 증가량을 적용합니다. 다시 누르면 현재 입력값으로 교체합니다.
        /// </summary>
        [SWButton("전투 공식: 선택 아군에 테스트 능력치 적용")]
        public void TestApplyAllyCombatStats()
        {
            if (!PrepareBattle())
            {
                return;
            }

            CharacterUnit unit = input != null ? input.Selection.SelectedUnit : null;
            if (unit == null || unit.Stats == null)
            {
                SetCombatTestStatus("먼저 필드에서 아군을 왼쪽 클릭해 선택하세요.");
                return;
            }

            bool applied = SetTestBonus(unit, unit.Definition.CriticalChanceStat, allyCriticalChance)
                & SetTestBonus(unit, unit.Definition.CriticalDamageStat, allyCriticalDamage)
                & SetTestBonus(unit, unit.Definition.LifeStealStat, allyLifeSteal)
                & SetTestBonus(unit, unit.Definition.ArmorPenetrationStat, allyArmorPenetration)
                & SetTestBonus(unit, unit.Data.DefenseStat, allyDefense)
                & SetTestBonus(unit, unit.Data.EvasionStat, allyEvasion);
            AttackProfile attack = unit.AttackProfile;
            SetCombatTestStatus((applied ? "" : "일부 적용 실패 · ") + unit.Definition.DisplayName
                + " 치명 " + Percent(attack.CriticalChance)
                + " (배율 " + (ProjectDefine.Battle.BaseCriticalMultiplier + attack.CriticalDamage).ToString("0.##") + ")"
                + " · 흡수 " + Percent(unit.LifeSteal)
                + " · 관통 " + attack.ArmorPenetration.ToString("0.##")
                + " · 방어 " + unit.Defense.ToString("0.##")
                + " · 회피 " + Percent(unit.Evasion));
        }

        /// <summary>
        /// 현재 적과 이후 생성되는 적에게 입력한 방어·회피 증가량을 적용합니다. 다시 누르면 현재 입력값으로 교체합니다.
        /// </summary>
        [SWButton("전투 공식: 모든 적에 테스트 능력치 적용 (이후 생성 포함)")]
        public void TestApplyEnemyCombatStats()
        {
            if (!PrepareBattle())
            {
                return;
            }

            enemyTestActive = true;
            foreach (EnemyUnit enemy in battle.Enemies)
            {
                ApplyEnemyTestBonuses(enemy);
            }

            float scale = ProjectDefine.Battle.DefenseScale;
            SetCombatTestStatus("적 방어 +" + enemyDefense.ToString("0.##") + " (관통 0이면 피해 "
                + Percent(scale / (scale + enemyDefense)) + ") · 회피 +" + Percent(enemyEvasion)
                + ". 현재 적 " + battle.Enemies.Count + "명과 이후 생성되는 적에 적용합니다.");
        }

        /// <summary>
        /// 모든 아군과 적에서 테스트 증가량을 제거합니다. 장비 효과는 유지합니다.
        /// </summary>
        [SWButton("전투 공식: 테스트 능력치 모두 제거")]
        public void TestRemoveCombatStats()
        {
            if (!PrepareBattle())
            {
                return;
            }

            enemyTestActive = false;
            foreach (CharacterUnit ally in battle.Allies)
            {
                ally.Stats?.RemoveBonuses(combatTestSource);
            }

            foreach (EnemyUnit enemy in battle.Enemies)
            {
                enemy.Stats?.RemoveBonuses(combatTestSource);
            }

            SetCombatTestStatus("테스트 능력치를 모두 제거했습니다. 데이터와 장비 효과는 그대로입니다.");
        }

        #endregion // 전투 공식 테스트 버튼

        #region 전투 공식 테스트 연결
        /// <summary>
        /// 새 전투의 적 생성과 타격 계산 알림을 구독합니다.
        /// </summary>
        private void ConnectCombatTest()
        {
            enemyTestActive = false;
            battle.EnemySpawned += OnTestEnemySpawned;
            battle.HitCalculated += OnTestHitCalculated;
        }

        /// <summary>
        /// 이전 전투의 알림 구독을 해제합니다. 이미 파괴된 전투는 건너뜁니다.
        /// </summary>
        private void DisconnectCombatTest()
        {
            enemyTestActive = false;
            if (battle == null)
            {
                return;
            }

            battle.EnemySpawned -= OnTestEnemySpawned;
            battle.HitCalculated -= OnTestHitCalculated;
        }

        /// <summary>
        /// 적 테스트가 켜져 있으면 새로 생성한 적에도 같은 증가량을 적용합니다.
        /// </summary>
        private void OnTestEnemySpawned(EnemyUnit enemy)
        {
            if (enemyTestActive)
            {
                ApplyEnemyTestBonuses(enemy);
            }
        }

        /// <summary>
        /// 켜져 있을 때만 타격마다 입력 수치와 결과를 콘솔에 남깁니다.
        /// </summary>
        private void OnTestHitCalculated(UnitBase attacker, UnitBase target, DamageResult result, float healed)
        {
            if (!logHits)
            {
                return;
            }

            string penetration = attacker is CharacterUnit ally
                ? " · 관통 " + ally.AttackProfile.ArmorPenetration.ToString("0.##")
                : string.Empty;
            string outcome = result.Evaded
                ? "회피"
                : (result.Critical ? "치명타 " : string.Empty) + result.Amount.ToString("0.##");
            SWLog.Log("[전투 계산] " + attacker.Data.DisplayName + " → " + target.Data.DisplayName
                + " | 공격력 " + attacker.AttackDamage.ToString("0.##") + penetration
                + " | 대상 방어 " + target.Defense.ToString("0.##") + " · 회피 " + Percent(target.Evasion)
                + " | 결과 " + outcome
                + (healed > 0f ? " · 흡수 +" + healed.ToString("0.##") : string.Empty));
        }

        #endregion // 전투 공식 테스트 연결

        #region 전투 공식 테스트 적용
        /// <summary>
        /// 적 한 명에게 방어·회피 증가량을 적용합니다.
        /// </summary>
        private void ApplyEnemyTestBonuses(EnemyUnit enemy)
        {
            if (enemy == null || enemy.Stats == null)
            {
                return;
            }

            SetTestBonus(enemy, enemy.Data.DefenseStat, enemyDefense);
            SetTestBonus(enemy, enemy.Data.EvasionStat, enemyEvasion);
        }

        /// <summary>
        /// 테스트 출처의 증가량을 교체합니다. 없는 스탯이나 잘못된 값이면 false입니다.
        /// </summary>
        private bool SetTestBonus(UnitBase unit, SWStat stat, float amount)
        {
            return unit.Stats.TrySetBonus(stat, combatTestSource, combatTestSource, amount);
        }

        /// <summary>
        /// 1을 100%로 표시합니다.
        /// </summary>
        private static string Percent(float value)
        {
            return (value * 100f).ToString("0.#") + "%";
        }

        /// <summary>
        /// 마지막 전투 공식 테스트 결과를 인스펙터와 로그에 표시합니다.
        /// </summary>
        private void SetCombatTestStatus(string message)
        {
            combatTestStatus = message;
            SWLog.Log("[TestManager] " + message);
        }

        #endregion // 전투 공식 테스트 적용
    }
}
