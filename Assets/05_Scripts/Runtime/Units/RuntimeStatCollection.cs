using System;
using System.Collections.Generic;
using UnityEngine;

using SW.Stat;
using SW.Util;

namespace ProjectT.Units
{
    /// <summary>
    /// 한 소유자의 SWStat 복제본을 관리합니다. 원본 자산을 변경하지 않으며 해제 후 조회는 0입니다.
    /// </summary>
    public sealed class RuntimeStatCollection : IDisposable
    {
        #region 필드
        private readonly Dictionary<SWStat, SWStat> stats = new Dictionary<SWStat, SWStat>();
        private readonly List<SWStat> order = new List<SWStat>();

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 복제본의 값이 변경되었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// 데이터에 설정한 순서대로 현재 복제본을 열거합니다. 이름과 표시 형식은 스탯 자산을 따릅니다.
        /// </summary>
        public IReadOnlyList<SWStat> RuntimeStats => order;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 설정만 Create를 통해 초기화합니다.
        /// </summary>
        private RuntimeStatCollection()
        {
        }

        /// <summary>
        /// 전체 참조·중복·범위를 먼저 검사합니다. 잘못된 설정이면 복제 없이 null입니다.
        /// </summary>
        public static RuntimeStatCollection Create(IEnumerable<SWStatOverride> settings)
        {
            if (settings == null)
            {
                SWLog.LogWarning("[RuntimeStatCollection] 생성 실패: 스탯 설정이 없습니다.");
                return null;
            }

            var entries = new List<SWStatOverride>(settings);
            var definitions = new HashSet<SWStat>();
            var identifiers = new HashSet<int>();
            foreach (SWStatOverride entry in entries)
            {
                SWStat definition = entry?.Stat;
                float value = entry.ExGetConfiguredValue();
                if (definition == null || !definitions.Add(definition)
                    || (definition.ID != 0 && !identifiers.Add(definition.ID))
                    || !value.ExIsFinite() || !definition.MinValue.ExIsFinite()
                    || !definition.MaxValue.ExIsFinite() || definition.MinValue > definition.MaxValue
                    || value < definition.MinValue || value > definition.MaxValue)
                {
                    SWLog.LogWarning("[RuntimeStatCollection] 생성 실패: 스탯 참조·중복·값 범위를 확인하세요.");
                    return null;
                }
            }

            var collection = new RuntimeStatCollection();
            foreach (SWStatOverride entry in entries)
            {
                SWStat runtime = entry.CreateStat();
                if (runtime == null)
                {
                    collection.Dispose();
                    return null;
                }

                collection.stats.Add(entry.Stat, runtime);
                collection.order.Add(runtime);
                runtime.OnValueChanged += collection.OnValueChanged;
            }

            return collection;
        }

        #endregion // 초기화

        #region 조회와 변경
        /// <summary>
        /// 원본 정의에 대응하는 현재 값을 반환합니다. 연결되지 않은 스탯은 0입니다.
        /// </summary>
        public float GetValue(SWStat definition)
        {
            return definition != null && stats.TryGetValue(definition, out SWStat runtime) ? runtime.Value : 0f;
        }

        /// <summary>
        /// 출처별 증가량을 교체합니다. 없는 스탯·출처·유한하지 않은 값은 적용하지 않고 false입니다.
        /// </summary>
        public bool TrySetBonus(SWStat definition, object source, object effect, float amount)
        {
            if (definition == null || source == null || effect == null || !amount.ExIsFinite()
                || !stats.TryGetValue(definition, out SWStat runtime))
            {
                SWLog.LogWarning("[RuntimeStatCollection] 증가량 적용 실패: 스탯·출처·값을 확인하세요.");
                return false;
            }

            float remaining = runtime.BonusValue - runtime.GetBonusValue(source, effect);
            float nextBonus = remaining + amount;
            if (!remaining.ExIsFinite() || !nextBonus.ExIsFinite() || !(runtime.DefaultValue + nextBonus).ExIsFinite())
            {
                SWLog.LogWarning("[RuntimeStatCollection] 증가량 적용 실패: 합계가 계산 가능한 범위를 벗어났습니다.");
                return false;
            }

            runtime.SetBonusValue(source, effect, amount);
            return true;
        }

        /// <summary>
        /// 해당 출처의 증가량을 모두 제거합니다. 없는 출처는 무시합니다.
        /// </summary>
        public void RemoveBonuses(object source)
        {
            if (source == null)
            {
                return;
            }

            foreach (SWStat runtime in stats.Values)
            {
                runtime.RemoveBonusValue(source);
            }
        }

        /// <summary>
        /// 스탯 알림을 소유자에게 전달합니다.
        /// </summary>
        private void OnValueChanged(SWStat stat, float current, float previous)
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
                    SWLog.LogError("[RuntimeStatCollection] 능력치 변경 알림 실패: " + exception);
                }
            }
        }

        /// <summary>
        /// 복제본과 이벤트를 해제합니다. 여러 번 호출해도 안전합니다.
        /// </summary>
        public void Dispose()
        {
            foreach (SWStat runtime in stats.Values)
            {
                runtime.OnValueChanged -= OnValueChanged;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(runtime);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(runtime);
                }
            }

            stats.Clear();
            order.Clear();
            Changed = null;
        }

        #endregion // 조회와 변경
    }
}
