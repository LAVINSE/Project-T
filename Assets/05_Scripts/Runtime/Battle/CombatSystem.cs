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
    public sealed class CombatSystem
    {
        #region 필드
        private readonly List<CharacterUnit> allies;
        private readonly List<EnemyUnit> enemies;
        private readonly Workshop workshop;
        private readonly Dictionary<CharacterUnit, int> blockedCounts = new Dictionary<CharacterUnit, int>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 피해를 적용한 공격의 시각 효과를 요청합니다.
        /// </summary>
        public event Action<Vector2, Vector2, bool> Attacked;

        /// <summary>
        /// 아군 기본 공격의 실제 체력 감소량과 마지막 타격 처치 여부를 전달합니다.
        /// </summary>
        public event Action<CharacterUnit, float, bool> AllyHitResolved;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 현재 전투의 개체 목록을 연결합니다.
        /// </summary>
        public CombatSystem(
            List<CharacterUnit> allyUnits,
            List<EnemyUnit> enemyUnits,
            Workshop objective)
        {
            allies = allyUnits;
            enemies = enemyUnits;
            workshop = objective;
        }

        #endregion // 초기화

        #region 함수

        /// <summary>
        /// 휴식과 결과 진입 시 미발생 타격과 공격 동작을 정리합니다.
        /// </summary>
        public void CancelAttacks()
        {
            foreach (CharacterUnit ally in allies)
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
        public void ReleaseUnavailableBlocker(CharacterUnit ally)
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
        /// 현재 게임 시각을 기준으로 저지와 공격을 순서대로 진행합니다.
        /// </summary>
        public void Tick(float time)
        {
            if (!workshop.Health.IsAlive)
            {
                return;
            }

            ReleaseInvalidBlockers();
            AssignBlockers();
            TickAllyAttacks(time);
            TickEnemyAttacks(time);
        }

        /// <summary>
        /// 저지 수를 다시 세고 사거리를 벗어나거나 싸울 수 없게 된 저지를 해제합니다.
        /// </summary>
        private void ReleaseInvalidBlockers()
        {
            blockedCounts.Clear();
            foreach (CharacterUnit ally in allies)
            {
                blockedCounts[ally] = 0;
            }

            foreach (EnemyUnit enemy in enemies)
            {
                CharacterUnit blocker = enemy.Blocker;
                if (blocker == null)
                {
                    continue;
                }

                if (!enemy.IsActive
                    || !blocker.CanFight
                    || Distance(blocker, enemy) > blocker.AttackRange
                    || blockedCounts[blocker] >= blocker.BlockCapacity)
                {
                    enemy.SetBlocker(null);
                }
                else
                {
                    blockedCounts[blocker]++;
                }
            }
        }

        /// <summary>
        /// 저지가 없는 적마다 사거리 안에서 가장 가까운 여유 있는 아군을 배정합니다.
        /// </summary>
        private void AssignBlockers()
        {
            foreach (EnemyUnit enemy in enemies)
            {
                if (!enemy.IsActive || enemy.Blocker != null)
                {
                    continue;
                }

                CharacterUnit blocker = FindBlocker(enemy);
                enemy.SetBlocker(blocker);
                if (blocker != null)
                {
                    blockedCounts[blocker]++;
                }
            }
        }

        /// <summary>
        /// 적을 저지할 수 있는 가장 가까운 아군을 찾습니다. 없으면 null입니다.
        /// </summary>
        private CharacterUnit FindBlocker(EnemyUnit enemy)
        {
            CharacterUnit blocker = null;
            float closest = float.PositiveInfinity;
            foreach (CharacterUnit ally in allies)
            {
                if (!ally.CanFight || ally.BlockCapacity <= blockedCounts[ally])
                {
                    continue;
                }

                float distance = Distance(ally, enemy);
                if (distance > ally.AttackRange || distance >= closest)
                {
                    continue;
                }

                blocker = ally;
                closest = distance;
            }

            return blocker;
        }

        /// <summary>
        /// 싸울 수 있는 아군의 예약된 타격을 적용하고 새 공격을 시작합니다.
        /// </summary>
        private void TickAllyAttacks(float time)
        {
            foreach (CharacterUnit ally in allies)
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

                EnemyUnit target = FindAllyTarget(ally);
                if (target == null)
                {
                    continue;
                }

                ally.AttackTarget = target;
                Begin(
                    ally.Attack,
                    ally.Definition,
                    ally.AttackInterval,
                    time,
                    target.transform.position,
                    target.Health);
                ResolveAllyAttack(ally, time);
            }
        }

        /// <summary>
        /// 자기가 저지 중인 적을 우선하고, 없으면 공방에 가장 가까운 적을 고릅니다.
        /// </summary>
        private EnemyUnit FindAllyTarget(CharacterUnit ally)
        {
            EnemyUnit target = null;
            float remaining = float.PositiveInfinity;
            foreach (EnemyUnit enemy in enemies)
            {
                if (!enemy.IsActive || Distance(ally, enemy) > ally.AttackRange)
                {
                    continue;
                }

                if (enemy.Blocker == ally)
                {
                    return enemy;
                }

                if (enemy.Movement.RemainingDistance < remaining)
                {
                    target = enemy;
                    remaining = enemy.Movement.RemainingDistance;
                }
            }

            return target;
        }

        /// <summary>
        /// 살아 있는 적의 예약된 타격을 적용하고 아군 또는 공방 공격을 시작합니다. 공방이 파괴되면 즉시 멈춥니다.
        /// </summary>
        private void TickEnemyAttacks(float time)
        {
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

                CharacterUnit target = FindEnemyTarget(enemy);
                if (target == null && !enemy.HasReachedWorkshop)
                {
                    continue;
                }

                enemy.AttackTarget = target;
                if (target != null)
                {
                    Begin(
                        enemy.Attack,
                        enemy.Definition,
                        enemy.AttackInterval,
                        time,
                        target.transform.position,
                        target.Health);
                }
                else
                {
                    Begin(
                        enemy.Attack,
                        enemy.Definition,
                        enemy.WorkshopAttackInterval,
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
        /// 저지 중인 아군을 우선하고, 없으면 사거리 안에서 가장 가까운 살아 있는 아군을 고릅니다.
        /// 공방에 도착한 적은 아군을 새로 찾지 않습니다.
        /// </summary>
        private CharacterUnit FindEnemyTarget(EnemyUnit enemy)
        {
            CharacterUnit target = enemy.Blocker;
            if (target != null || enemy.HasReachedWorkshop)
            {
                return target;
            }

            float closest = enemy.AttackRange;
            foreach (CharacterUnit ally in allies)
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

            return target;
        }

        /// <summary>
        /// 데이터의 공격 재생 길이로 공격 동작을 시작합니다.
        /// </summary>
        private static void Begin(
            UnitAttack attack,
            UnitData data,
            float interval,
            float time,
            Vector2 position,
            Health health)
        {
            attack.Begin(time, interval, data.GetAttackDuration(interval), position, health);
        }

        /// <summary>
        /// 아군의 공격 대상과 사거리를 확인한 뒤 타격 시점에 피해를 적용합니다.
        /// </summary>
        private void ResolveAllyAttack(CharacterUnit ally, float time)
        {
            UnitAttack attack = ally.Attack;
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
            if (attack.HasPendingImpact && Distance(ally, target) > ally.AttackRange)
            {
                attack.Cancel();
                return;
            }

            if (!attack.TryImpact())
            {
                return;
            }

            EmitImpact(
                ally.transform.position,
                target.transform.position,
                ally.Definition,
                target.Definition,
                ally.BlockCapacity == 0);
            Health targetHealth = target.Health;
            float previousHealth = targetHealth.Current;
            if (targetHealth.TakeDamage(ally.AttackDamage))
            {
                AllyHitResolved?.Invoke(ally, previousHealth - targetHealth.Current, !targetHealth.IsAlive);
            }
        }

        /// <summary>
        /// 적의 현재 공격을 아군 또는 공방에 연결하고 타격을 처리합니다.
        /// </summary>
        private void ResolveEnemyAttack(EnemyUnit enemy, float time)
        {
            UnitAttack attack = enemy.Attack;
            if (!attack.HasPendingImpact && !attack.IsPlaying(time))
            {
                return;
            }

            CharacterUnit target = enemy.AttackTarget;
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
            if (attack.HasPendingImpact && Distance(target, enemy) > enemy.AttackRange)
            {
                attack.Cancel();
                return;
            }

            if (!attack.TryImpact())
            {
                return;
            }

            EmitImpact(enemy.transform.position, target.transform.position, enemy.Definition, target.Definition, false);
            target.Health.TakeDamage(enemy.AttackDamage);
        }

        /// <summary>
        /// 유효한 공방 공격의 타격 효과와 피해를 한 번 적용합니다.
        /// </summary>
        private void ResolveWorkshopAttack(EnemyUnit enemy, float time)
        {
            UnitAttack attack = enemy.Attack;
            if (!attack.MatchesTarget(workshop.Health))
            {
                if (attack.HasPendingImpact)
                {
                    attack.Cancel();
                }

                return;
            }

            attack.AimAt(workshop.Position);
            if (!attack.TryImpact())
            {
                return;
            }

            Vector2 offset = enemy.Definition.AttackOriginOffset;
            offset.x *= workshop.Position.x < enemy.transform.position.x ? -1f : 1f;
            Attacked?.Invoke((Vector2)enemy.transform.position + offset, workshop.Position, false);
            workshop.Health.TakeDamage(enemy.WorkshopAttackDamage);
        }

        /// <summary>
        /// 공격자와 대상의 표시 위치를 보정해 타격 효과를 요청합니다.
        /// </summary>
        private void EmitImpact(
            Vector2 source,
            Vector2 target,
            UnitData sourceData,
            UnitData targetData,
            bool ranged)
        {
            Vector2 offset = sourceData.AttackOriginOffset;
            offset.x *= target.x < source.x ? -1f : 1f;
            Attacked?.Invoke(source + offset, target + Vector2.up * targetData.HealthBarHeight * 0.45f, ranged);
        }

        /// <summary>
        /// 아군과 적 사이의 평면 거리를 계산합니다.
        /// </summary>
        private static float Distance(CharacterUnit ally, EnemyUnit enemy)
        {
            return Vector2.Distance(ally.transform.position, enemy.transform.position);
        }

        #endregion // 함수
    }
}
