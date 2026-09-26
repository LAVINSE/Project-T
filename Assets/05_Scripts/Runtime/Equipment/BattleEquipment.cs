using System;
using System.Collections.Generic;

using SW.Stat;
using SW.Util;

using ProjectT.Data;
using ProjectT.Inventory;
using ProjectT.Units;

namespace ProjectT.Equipment
{
    /// <summary>
    /// 전투 개체별 장비 점유와 효과를 함께 관리합니다. 종료하면 점유를 해제해 모든 장비를 돌려줍니다.
    /// </summary>
    public sealed class BattleEquipment : IDisposable
    {
        #region 필드
        private readonly InventoryStore inventory;
        private readonly Dictionary<CharacterUnit, Loadout> loadouts = new Dictionary<CharacterUnit, Loadout>();
        private bool changing;
        private bool disposed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 장착·교체·해제 후 화면에 알립니다.
        /// </summary>
        public event Action Changed;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 전투가 준비한 영구 보관소를 연결합니다.
        /// </summary>
        public BattleEquipment(InventoryStore inventory)
        {
            this.inventory = inventory;
        }

        #endregion // 초기화

        #region 조회와 장착
        /// <summary>
        /// 해당 개체 슬롯의 장착 아이템입니다. 빈 슬롯 또는 없는 개체이면 null입니다.
        /// </summary>
        public InventoryStack GetItem(CharacterUnit unit, int index)
        {
            return unit != null && index >= 0 && index < ProjectDefine.Battle.EquipmentSlotCount
                && loadouts.TryGetValue(unit, out Loadout loadout) ? loadout.Items[index] : null;
        }

        /// <summary>
        /// 한 개를 장착하거나 기존 장비를 교체합니다. null 식별자는 해제이며 실패 시 수량·효과를 유지합니다.
        /// </summary>
        public bool TryEquip(CharacterUnit unit, int index, string identifier, out string reason)
        {
            reason = "장비를 변경할 캐릭터와 슬롯을 확인하세요.";
            if (disposed || changing || unit == null || unit.Stats == null || index < 0 || index >= ProjectDefine.Battle.EquipmentSlotCount)
            {
                return false;
            }
            if (!loadouts.TryGetValue(unit, out Loadout loadout))
            {
                loadout = new Loadout();
                loadouts.Add(unit, loadout);
            }
            if (loadout.Items[index]?.Identifier == identifier)
            {
                reason = string.Empty;
                return true;
            }

            InventoryStack available = identifier == null ? null : inventory.Find(identifier);
            if (identifier != null && (available?.Definition?.Equipment == null || !available.Definition.IsValid))
            {
                reason = "보유 중인 유효한 장비만 장착할 수 있습니다.";
                return false;
            }
            if (HasDuplicate(loadout, index, available?.Definition?.Equipment))
            {
                reason = "같은 캐릭터에게 동일한 장비를 중복 장착할 수 없습니다.";
                return false;
            }

            var next = (InventoryStack[])loadout.Items.Clone();
            next[index] = available == null ? null
                : new InventoryStack(new InventoryQuantity(identifier, 1), available.Definition);
            if (!TryCollectBonuses(next, out Dictionary<SWStat, float> bonuses))
            {
                reason = "이 장비의 효과 설정 또는 아직 구현되지 않은 능력을 확인하세요.";
                return false;
            }

            changing = true;
            try
            {
                if (!inventory.TrySetReservation(loadout.Owners[index], identifier,
                    () => Apply(unit, loadout, next, bonuses), out reason))
                {
                    return false;
                }
                NotifyChanged();
                return true;
            }
            finally
            {
                changing = false;
            }
        }

        /// <summary>
        /// 성능 등급과 관계없이 같은 장비 정의가 다른 슬롯에 있는지 확인합니다.
        /// </summary>
        private bool HasDuplicate(Loadout loadout, int targetIndex, EquipmentData equipment)
        {
            if (equipment == null)
            {
                return false;
            }
            for (int index = 0; index < loadout.Items.Length; index++)
            {
                if (index != targetIndex && loadout.Items[index]?.Definition?.Equipment == equipment)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 슬롯의 등급별 스탯과 추가 스탯 효과를 한 묶음으로 모읍니다.
        /// </summary>
        private bool TryCollectBonuses(InventoryStack[] items, out Dictionary<SWStat, float> bonuses)
        {
            bonuses = new Dictionary<SWStat, float>();
            foreach (InventoryStack item in items)
            {
                if (item == null)
                {
                    continue;
                }
                if (!item.Definition.Equipment.TryGetPerformanceGrade(item.Definition.PerformanceGrade, out var grade)
                    || !grade.TryResolveStatBonuses(out var resolved))
                {
                    return false;
                }
                AppendBonuses(resolved, bonuses);
                if (grade.AdditionalEffects == null)
                {
                    continue;
                }
                foreach (EquipmentEffectData effect in grade.AdditionalEffects)
                {
                    if (effect == null || !effect.TryGetStatBonuses(out var additional))
                    {
                        return false;
                    }
                    AppendBonuses(additional, bonuses);
                }
            }
            return true;
        }

        /// <summary>
        /// 같은 스탯의 증가량을 합산합니다. 최종 값의 유효성은 적용 전에 능력치 모듈이 검사합니다.
        /// </summary>
        private void AppendBonuses(IReadOnlyList<EquipmentStatBonus> source, Dictionary<SWStat, float> target)
        {
            foreach (EquipmentStatBonus bonus in source)
            {
                target.TryGetValue(bonus.Stat, out float current);
                target[bonus.Stat] = current + bonus.Amount;
            }
        }

        /// <summary>
        /// 완성된 스탯 묶음을 적용한 뒤 슬롯을 교체합니다.
        /// </summary>
        private bool Apply(CharacterUnit unit, Loadout loadout, InventoryStack[] next, Dictionary<SWStat, float> bonuses)
        {
            if (!unit.Stats.TryReplaceBonuses(loadout, bonuses))
            {
                return false;
            }
            loadout.Items = next;
            return true;
        }

        #endregion // 조회와 장착

        #region 정리
        /// <summary>
        /// 승패·재시작·장면 종료 때 모든 점유를 해제합니다. 저장된 총보유량을 더하거나 빼지 않습니다.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            foreach (var entry in loadouts)
            {
                if (entry.Key != null)
                {
                    entry.Key.Stats.TryReplaceBonuses(entry.Value, new Dictionary<SWStat, float>());
                }
                foreach (object owner in entry.Value.Owners)
                {
                    inventory.TrySetReservation(owner, null, null, out _);
                }
            }
            loadouts.Clear();
            NotifyChanged();
        }

        /// <summary>
        /// 화면 오류가 이미 반영한 장착이나 나머지 정리를 중단하지 않도록 알림만 격리합니다.
        /// </summary>
        private void NotifyChanged()
        {
            if (Changed == null)
            {
                return;
            }
            foreach (Action subscriber in Changed.GetInvocationList())
            {
                try
                {
                    subscriber();
                }
                catch (Exception exception)
                {
                    SWLog.LogError("[BattleEquipment] 장비 변경 알림 실패: " + exception);
                }
            }
        }

        /// <summary>
        /// 한 캐릭터의 슬롯별 점유 식별자와 아이템을 보관합니다.
        /// </summary>
        private sealed class Loadout
        {
            #region 필드
            /// <summary>
            /// 슬롯별 한 개 점유를 구분하는 식별자입니다.
            /// </summary>
            public readonly object[] Owners = new object[ProjectDefine.Battle.EquipmentSlotCount];
            /// <summary>
            /// 현재 장착된 아이템입니다. 빈 슬롯은 null입니다.
            /// </summary>
            public InventoryStack[] Items = new InventoryStack[ProjectDefine.Battle.EquipmentSlotCount];

            #endregion // 필드

            #region 초기화
            /// <summary>
            /// 슬롯마다 독립된 점유 식별자를 만듭니다.
            /// </summary>
            public Loadout()
            {
                for (int index = 0; index < Owners.Length; index++)
                {
                    Owners[index] = new object();
                }
            }

            #endregion // 초기화
        }

        #endregion // 정리
    }
}
