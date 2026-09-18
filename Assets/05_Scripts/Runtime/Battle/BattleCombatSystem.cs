using System;
using System.Collections.Generic;
using UnityEngine;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Battle
{
    /// <summary>
    /// 근접 저지와 자동 공격 대상을 결정합니다. 이동 명령과 사망은 저지를 즉시 해제합니다.
    /// </summary>
    public sealed class BattleCombatSystem
    {
        #region 필드
        private readonly List<AllyUnit> allies;
        private readonly List<EnemyUnit> enemies;
        private readonly WorkshopObjective workshop;
        private readonly Dictionary<AllyUnit, int> blockedCounts = new Dictionary<AllyUnit, int>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 피해를 적용한 공격의 시각 효과를 요청합니다.
        /// </summary>
        public event Action<UnityEngine.Vector2, UnityEngine.Vector2, bool> Attacked;

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 현재 전투의 개체 목록을 연결합니다.
        /// </summary>
        public BattleCombatSystem(List<AllyUnit> allyUnits, List<EnemyUnit> enemyUnits, WorkshopObjective objective)
        {
            allies = allyUnits;
            enemies = enemyUnits;
            workshop = objective;
        }

        /// <summary>
        /// 휴식과 결과 진입 시 미발생 타격과 공격 동작을 정리합니다.
        /// </summary>
        public void CancelAttacks()
        {
            foreach (AllyUnit ally in allies)
            {
                ally.Attack.Cancel();
                ally.AttackTarget = null;
            }

            foreach (EnemyUnit enemy in enemies)
            {
                enemy.Attack.Cancel();
                enemy.AttackTarget = null;
            }
        }

        /// <summary>
        /// 이동을 시작하거나 사망한 아군의 저지를 같은 명령 안에서 해제합니다.
        /// </summary>
        public void ReleaseUnavailableBlocker(AllyUnit ally)
        {
            if (ally.CanFight)
            {
                return;
            }

            ally.Attack.Cancel();
            ally.AttackTarget = null;
            foreach (EnemyUnit enemy in enemies)
            {
                if (enemy.Blocker == ally)
                {
                    enemy.SetBlocker(null);
                }
            }
        }

        /// <summary>
        /// 현재 게임 시각을 기준으로 저지와 공격을 진행합니다.
        /// </summary>
        public void Tick(float time)
        {
            if (!workshop.Health.IsAlive)
            {
                return;
            }

            blockedCounts.Clear();
            foreach (AllyUnit ally in allies)
            {
                blockedCounts[ally] = 0;
            }

            foreach (EnemyUnit enemy in enemies)
            {
                AllyUnit blocker = enemy.Blocker;
                if (blocker == null)
                {
                    continue;
                }

                if (!enemy.IsActive
                    || !blocker.CanFight
                    || Distance(blocker, enemy) > blocker.Definition.AttackRange
                    || blockedCounts[blocker] >= blocker.Definition.BlockCapacity)
                {
                    enemy.SetBlocker(null);
                }
                else
                {
                    blockedCounts[blocker]++;
                }
            }

            foreach (EnemyUnit enemy in enemies)
            {
                if (!enemy.IsActive)
                {
                    continue;
                }

                AllyUnit blocker = enemy.Blocker;
                if (blocker != null)
                {
                    continue;
                }

                if (blocker == null)
                {
                    float closest = float.PositiveInfinity;
                    foreach (AllyUnit ally in allies)
                    {
                        if (!ally.CanFight || ally.Definition.BlockCapacity <= blockedCounts[ally])
                        {
                            continue;
                        }

                        float distance = Distance(ally, enemy);
                        if (distance > ally.Definition.AttackRange || distance >= closest)
                        {
                            continue;
                        }

                        blocker = ally;
                        closest = distance;
                    }
                }

                enemy.SetBlocker(blocker);
                if (blocker != null)
                {
                    blockedCounts[blocker]++;
                }
            }

            foreach (AllyUnit ally in allies)
            {
                if (!ally.CanFight)
                {
                    ally.Attack.Cancel();
                    continue;
                }

                ResolveAllyAttack(ally, time);
                if (!ally.Attack.CanStart(time))
                {
                    continue;
                }

                EnemyUnit target = null;
                float remaining = float.PositiveInfinity;
                foreach (EnemyUnit enemy in enemies)
                {
                    if (!enemy.IsActive || Distance(ally, enemy) > ally.Definition.AttackRange)
                    {
                        continue;
                    }

                    if (enemy.Blocker == ally)
                    {
                        target = enemy;
                        break;
                    }

                    if (enemy.Movement.RemainingDistance < remaining)
                    {
                        target = enemy;
                        remaining = enemy.Movement.RemainingDistance;
                    }
                }

                if (target == null)
                {
                    continue;
                }

                ally.AttackTarget = target;
                Begin(
                    ally.Attack,
                    ally.Definition.Appearance,
                    ally.Definition.AttackInterval,
                    time,
                    target.transform.position,
                    target.Health);
                ResolveAllyAttack(ally, time);
            }

            foreach (EnemyUnit enemy in enemies)
            {
                if (!enemy.IsActive)
                {
                    enemy.Attack.Cancel();
                    continue;
                }

                ResolveEnemyAttack(enemy, time);
                if (!workshop.Health.IsAlive)
                {
                    return;
                }

                if (!enemy.Attack.CanStart(time))
                {
                    continue;
                }

                AllyUnit target = enemy.Blocker;
                float closest = enemy.Definition.AttackRange;
                if (target == null && !enemy.HasReachedWorkshop)
                {
                    foreach (AllyUnit ally in allies)
                    {
                        if (!ally.Health.IsAlive)
                        {
                            continue;
                        }

                        float distance = Distance(ally, enemy);
                        if (distance > closest)
                        {
                            continue;
                        }

                        target = ally;
                        closest = distance;
                    }
                }

                if (target == null && !enemy.HasReachedWorkshop)
                {
                    continue;
                }

                enemy.AttackTarget = target;
                if (target != null)
                {
                    Begin(
                        enemy.Attack,
                        enemy.Definition.Appearance,
                        enemy.Definition.AttackInterval,
                        time,
                        target.transform.position,
                        target.Health);
                }
                else
                {
                    Begin(
                        enemy.Attack,
                        enemy.Definition.Appearance,
                        enemy.Definition.WorkshopAttackInterval,
                        time,
                        workshop.Position,
                        workshop.Health);
                }

                ResolveEnemyAttack(enemy, time);
                if (!workshop.Health.IsAlive)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 외형의 재생 길이와 타격 비율로 공격 동작을 시작합니다.
        /// </summary>
        private static void Begin(
            UnitAttackSequence attack,
            UnitAppearance appearance,
            float interval,
            float time,
            Vector2 position,
            CombatHealth health)
        {
            attack.Begin(time, interval, appearance.GetAttackDuration(interval), appearance.AttackImpactRatio, position, health);
        }

        /// <summary>
        /// 아군의 공격 대상과 사거리를 확인한 뒤 타격 시점에 피해를 적용합니다.
        /// </summary>
        private void ResolveAllyAttack(AllyUnit ally, float time)
        {
            UnitAttackSequence attack = ally.Attack;
            if (!attack.HasPendingImpact && !attack.IsPlaying(time))
            {
                return;
            }

            EnemyUnit target = ally.AttackTarget;
            if (target == null || !target.IsActive || !attack.MatchesTarget(target.Health))
            {
                if (attack.HasPendingImpact)
                {
                    attack.Cancel();
                }

                return;
            }

            attack.AimAt(target.transform.position);
            if (attack.HasPendingImpact && Distance(ally, target) > ally.Definition.AttackRange)
            {
                attack.Cancel();
                return;
            }

            if (!attack.TryImpact(time))
            {
                return;
            }

            EmitImpact(
                ally.transform.position,
                target.transform.position,
                ally.Definition.Appearance,
                target.Definition.Appearance,
                ally.Definition.BlockCapacity == 0);
            target.Health.TakeDamage(ally.Definition.AttackDamage);
        }

        /// <summary>
        /// 적의 현재 공격을 아군 또는 공방에 연결하고 타격을 처리합니다.
        /// </summary>
        private void ResolveEnemyAttack(EnemyUnit enemy, float time)
        {
            UnitAttackSequence attack = enemy.Attack;
            if (!attack.HasPendingImpact && !attack.IsPlaying(time))
            {
                return;
            }

            AllyUnit target = enemy.AttackTarget;
            if (target == null && enemy.HasReachedWorkshop && enemy.Blocker == null)
            {
                ResolveWorkshopAttack(enemy, time);
                return;
            }

            if (target == null || !attack.MatchesTarget(target.Health))
            {
                if (attack.HasPendingImpact)
                {
                    attack.Cancel();
                }

                return;
            }

            attack.AimAt(target.transform.position);
            if (attack.HasPendingImpact && Distance(target, enemy) > enemy.Definition.AttackRange)
            {
                attack.Cancel();
                return;
            }

            if (!attack.TryImpact(time))
            {
                return;
            }

            EmitImpact(
                enemy.transform.position,
                target.transform.position,
                enemy.Definition.Appearance,
                target.Definition.Appearance,
                false);
            target.Health.TakeDamage(enemy.Definition.AttackDamage);
        }

        /// <summary>
        /// 유효한 공방 공격의 타격 효과와 피해를 한 번 적용합니다.
        /// </summary>
        private void ResolveWorkshopAttack(EnemyUnit enemy, float time)
        {
            UnitAttackSequence attack = enemy.Attack;
            if (!attack.MatchesTarget(workshop.Health))
            {
                if (attack.HasPendingImpact)
                {
                    attack.Cancel();
                }

                return;
            }

            attack.AimAt(workshop.Position);
            if (!attack.TryImpact(time))
            {
                return;
            }

            Vector2 offset = enemy.Definition.Appearance.AttackOriginOffset;
            offset.x *= workshop.Position.x < enemy.transform.position.x ? -1f : 1f;
            Attacked?.Invoke((Vector2)enemy.transform.position + offset, workshop.Position, false);
            workshop.Health.TakeDamage(enemy.Definition.WorkshopAttackDamage);
        }

        /// <summary>
        /// 공격자와 대상의 표시 위치를 보정해 타격 효과를 요청합니다.
        /// </summary>
        private void EmitImpact(
            Vector2 source,
            Vector2 target,
            UnitAppearance sourceAppearance,
            UnitAppearance targetAppearance,
            bool ranged)
        {
            Vector2 offset = sourceAppearance.AttackOriginOffset;
            offset.x *= target.x < source.x ? -1f : 1f;
            Attacked?.Invoke(source + offset, target + Vector2.up * targetAppearance.HealthBarHeight * 0.45f, ranged);
        }

        /// <summary>
        /// 아군과 적 사이의 평면 거리를 계산합니다.
        /// </summary>
        private static float Distance(AllyUnit ally, EnemyUnit enemy)
        {
            return UnityEngine.Vector2.Distance(ally.transform.position, enemy.transform.position);
        }

        #endregion // 함수
    }
}
