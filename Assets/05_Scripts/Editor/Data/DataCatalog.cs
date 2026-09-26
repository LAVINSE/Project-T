using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using SW.Base;
using SW.Stat;
using SW.EditorTools.Util;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 데이터 편집기가 다루는 종류·저장 폴더·한글 항목 설명과 검색·사용처·검사 기능을 제공합니다.
    /// </summary>
    public static class DataCatalog
    {
        #region 필드
        /// <summary>
        /// 편집기가 관리하는 데이터 루트 폴더입니다.
        /// </summary>
        public const string DataFolder = ProjectDefine.Data.Root;

        private static readonly Dictionary<DataKind, (Type Type, string Name, string Folder)> kinds = new Dictionary<DataKind, (Type, string, string)>
        {
            { DataKind.Class, (typeof(UnitClassData), "클래스", "Character") },
            { DataKind.Enemy, (typeof(UnitEnemyData), "적", "Enemy") },
            { DataKind.Stage, (typeof(StageData), "스테이지", "Stage") },
            { DataKind.Route, (typeof(EnemyRouteData), "경로", "Navigation") },
            { DataKind.Currency, (typeof(CurrencyData), "재화", "Currency") },
            { DataKind.Item, (typeof(ItemData), "아이템", "Items") },
            { DataKind.Equipment, (typeof(EquipmentData), "장비", "Equipment") },
            { DataKind.EquipmentStatEffect, (typeof(EquipmentStatEffectData), "장비 효과", "Equipment/Effects") },
            { DataKind.EquipmentCategory, (typeof(SWCategory), "장비 등급", "Equipment/Categories") },
            { DataKind.Stat, (typeof(SWStat), "스탯", "Stats") }
        };

        private static readonly string[] groupOrder =
        {
            "기본 정보", "전투 능력", "전투 보조 능력치", "공방 공격", "성장 표시", "보상", "보상 기본값",
            "전투 시작", "전투 경제", "라운드", "장비 설정", "장비 연결", "장비 효과", "고정 증가량",
            "아이템 정보", "스탯 정의", "참조", "경로", "외형", "표시 조정"
        };

        private static readonly string[] labelNames = { "파일 이름", "표시 이름", "코드명" };
        private static readonly string[] sortNames = { "파일 이름", "표시 이름", "코드명", "식별 번호" };

        private static readonly Dictionary<string, (string Label, string Description, string Group)> fields = new Dictionary<string, (string, string, string)>
        {
            { "codeName", ("코드명", "다른 정의와 겹치지 않는 식별용 이름입니다. 연결된 자산은 유지됩니다.", "기본 정보") },
            { "id", ("식별 번호", "0은 원본 자산으로 구분합니다. 0이 아닌 번호는 같은 종류끼리 중복되지 않아야 합니다.", "기본 정보") },
            { "categories", ("분류", "SWUtils에서 사용하는 분류 목록입니다.", "기본 정보") },
            { "spriteIcon", ("편집기 아이콘", "편집기 목록에 표시할 그림입니다.", "기본 정보") },
            { "isPercentType", ("백분율 표시", "1을 100%로 표시하는 스탯입니다. 증가량도 같은 단위를 사용합니다.", "스탯 정의") },
            { "minValue", ("최솟값", "능력치가 내려갈 수 있는 최솟값입니다.", "스탯 정의") },
            { "maxValue", ("최댓값", "능력치가 올라갈 수 있는 최댓값입니다.", "스탯 정의") },
            { "defaultValue", ("기본값", "최솟값과 최댓값 사이의 시작 값입니다.", "스탯 정의") },
            { "statBonuses", ("스탯 증가량", "스탯 정의와 고정 증가량을 추가합니다. 백분율 표시 스탯에서 0.05는 5%p입니다.", "장비 효과") },
            { "additionalEffects", ("추가 효과", "장비 능력 등 추가 효과 모듈을 연결할 자리입니다. 현재 구체적인 능력은 구현하지 않았습니다.", "장비 효과") },
            { "displayName", ("이름", "게임 화면에 표시할 이름입니다.", "기본 정보") },
            { "icon", ("아이콘", "선택적으로 표시할 재화·아이템 그림입니다.", "기본 정보") },
            { "sellPrice", ("판매가 (1개)", "0 이상의 유한한 값입니다. 장비 아이템은 연결한 장비의 판매가를 사용합니다. 기존 데이터의 초기값은 0입니다.", "기본 정보") },
            { "sellCurrency", ("판매 재화", "판매 대금으로 받을 CurrencyData입니다. 미지정이면 판매가를 표시하지 않으며 장비 아이템은 장비 설정을 따릅니다.", "기본 정보") },
            { "category", ("아이템 분류", "영구 인벤토리 필터에 사용할 SWCategory입니다. 미연결이면 기타로 표시합니다.", "아이템 정보") },
            { "specialLoot", ("특별 전리품", "자동 보관과 저장에 성공하면 인벤토리 아이콘에 획득 연출을 표시합니다. 분류와 무관하게 지정합니다.", "아이템 정보") },
            { "description", ("설명", "아이템에 마우스를 올렸을 때 표시할 설명입니다.", "아이템 정보") },
            { "equipment", ("장비 정의", "장비 아이템이면 종류·희귀도·성능 등급을 정의한 장비 데이터를 연결합니다. 일반 아이템은 비워 둡니다.", "장비 연결") },
            { "performanceGrade", ("성능 등급", "장비 정의의 등급 목록에 등록된 분류를 연결합니다. 같은 장비·등급은 하나의 아이템으로 보관합니다.", "장비 연결") },
            { "rarity", ("희귀도", "일반·희귀 등 원하는 분류 자산을 연결합니다. 성능 등급과 별개이며 자동 수치 배율은 없습니다.", "장비 설정") },
            { "performanceGrades", ("성능 등급 목록", "원하는 등급을 추가하고 화살표로 표시 순서를 바꿉니다. 공유 효과·스탯 포함 여부·수치와 추첨 가중치를 등급별로 설정합니다.", "장비 설정") },
            { "attackDamageBonus", ("공격력 증가량", "기본 공격력에 더할 고정값입니다. 0은 변화 없음입니다.", "고정 증가량") },
            { "attackSpeedBonus", ("공격 속도 증가량 (회/초)", "초당 공격 횟수에 더할 고정값입니다. 백분율이 아닙니다.", "고정 증가량") },
            { "moveSpeedBonus", ("이동속도 증가량", "초당 이동 거리에 더할 고정값입니다.", "고정 증가량") },
            { "attackRangeBonus", ("사거리 증가량", "공격 사거리에 더할 월드 거리입니다.", "고정 증가량") },
            { "deploymentCost", ("배치 비용", "캐릭터 한 명을 구매할 때 필요한 전투 재화입니다. 0 이상이어야 합니다.", "전투 능력") },
            { "defaultAmount", ("기본 수량", "보상 항목에서 수량 덮어쓰기를 끄면 사용하는 기본값입니다. 소유 잔액이 아닙니다.", "보상 기본값") },
            { "maximumHealth", ("최대 체력", "새 생명에 부여하는 체력입니다. 0보다 커야 합니다.", "전투 능력") },
            { "moveSpeed", ("이동속도", "게임 시간 1초당 이동하는 월드 거리입니다.", "전투 능력") },
            { "attackDamage", ("공격력", "상대 유닛에게 한 번 타격할 때 주는 피해입니다.", "전투 능력") },
            { "attackRange", ("사거리", "월드 거리 단위의 공격 범위입니다.", "전투 능력") },
            { "attackSpeed", ("공격 속도 (회/초)", "게임 시간 1초당 공격 횟수입니다. 0보다 커야 합니다.", "전투 능력") },
            { "level", ("레벨", "표시할 설정 레벨입니다. 자동 레벨업은 아직 적용하지 않습니다.", "성장 표시") },
            { "experience", ("현재 경험치", "현재 레벨의 설정 경험치입니다. 획득·저장은 후속 기능입니다.", "성장 표시") },
            { "requiredExperience", ("다음 레벨 필요 경험치", "0이면 경험치 기준 미설정으로 표시합니다.", "성장 표시") },
            { "defense", ("방어력", "받는 피해 × 100 ÷ (100 + 방어력 − 상대 방어 관통)으로 줄입니다. 100이면 피해가 절반입니다.", "전투 능력") },
            { "evasion", ("회피", "상대 기본 공격을 피할 확률입니다. 0.2는 20%이며 최대 75%까지 적용합니다.", "전투 능력") },
            { "criticalChance", ("치명타 확률", "기본 공격이 치명타가 될 확률입니다. 0.25는 25%이며 최대 100%입니다.", "전투 보조 능력치") },
            { "criticalDamage", ("치명타 피해", "기본 치명타 배율 1.5에 더하는 값입니다. 0.3이면 치명타가 1.8배 피해입니다.", "전투 보조 능력치") },
            { "lifeSteal", ("생명력 흡수", "기본 공격으로 실제 줄인 체력 중 회복하는 비율입니다. 0.1은 10%이며 최대 체력을 넘지 않습니다.", "전투 보조 능력치") },
            { "armorPenetration", ("방어 관통", "대상 방어력에서 빼는 고정값입니다. 방어력은 0 아래로 내려가지 않습니다.", "전투 보조 능력치") },
            { "skillHaste", ("스킬 가속", "스킬 대기시간 감소용 능력치입니다. 스킬 기능 구현 전에는 전투에 적용하지 않습니다.", "전투 보조 능력치") },
            { "attackAction", ("공격 동작", "공격할 때 재생할 동작 자산입니다. 프리팹의 UnitAnimation 동작 목록에 같은 동작을 등록하세요.", "전투 능력") },
            { "blockCapacity", ("동시 저지 수", "동시에 붙잡을 적 수입니다. 0이면 적을 저지하지 않습니다.", "전투 능력") },
            { "revivalSeconds", ("부활 대기 (초)", "사망 후 무료 부활까지의 게임 시간입니다.", "전투 능력") },
            { "workshopAttackDamage", ("공방 공격 피해", "공방에 한 번 타격할 때 주는 피해입니다. 아군 대상 피해와 별개입니다.", "공방 공격") },
            { "workshopAttackInterval", ("공방 공격 간격 (초)", "공방 공격 시작 사이의 게임 시간입니다.", "공방 공격") },
            { "rewards", ("처치 보상", "각 항목의 획득 확률을 독립 판정합니다. 100%는 항상, 0%는 지급하지 않습니다.", "보상") },
            { "startingCurrency", ("시작 재화", "새 전투에 지급하는 배치 재화입니다. 0 이상이어야 합니다.", "전투 시작") },
            { "workshopMaximumHealth", ("공방 최대 체력", "새 전투에서 공방에 부여하는 체력입니다.", "전투 시작") },
            { "deploymentCurrency", ("배치 재화", "이 자산에 해당하는 보상만 현재 전투 지갑에 지급합니다. 시작 금액과 배치 비용도 이 재화를 사용합니다.", "전투 경제") },
            { "spawnInterval", ("적 생성 간격 (초)", "한 라운드에서 적을 생성하는 게임 시간 간격입니다.", "라운드") },
            { "enemiesPerRound", ("라운드별 적 수", "목록의 순서가 라운드 순서입니다. 각 라운드는 적이 한 명 이상이어야 합니다.", "라운드") },
            { "enemyRoute", ("적 경로", "입구에서 공방까지 이동할 경로입니다.", "참조") },
            { "enemy", ("기본 적", "현재 구조는 스테이지당 기본 적 한 종류를 사용합니다.", "참조") },
            { "classes", ("구매 가능 클래스", "이 전투의 구매 목록입니다. 순서를 바꾸면 구매 목록 순서도 바뀝니다.", "참조") },
            { "points", ("경유점 (월드 좌표)", "입구부터 공방까지의 순서입니다. 두 점 이상이며 인접 점은 달라야 합니다.", "경로") },
            { "prefab", ("유닛 프리팹", "이 종류의 프리팹입니다. 애니메이터와 타격 이벤트는 프리팹 및 클립에서 설정합니다.", "외형") },
            { "portrait", ("초상화 (선택)", "비어 있으면 프리팹의 기본 그림을 사용합니다.", "외형") },
            { "tint", ("표시 색상", "원본 그림에 곱할 색상입니다.", "표시 조정") },
            { "feetOffset", ("발 높이 보정", "월드 거리 단위의 그림 위치 보정입니다.", "표시 조정") },
            { "healthBarHeight", ("체력바 높이", "발 위치로부터 체력바까지의 월드 높이입니다.", "표시 조정") },
            { "spritesFaceRight", ("원본이 오른쪽을 향함", "기본 그림 방향에 맞춰 좌우 뒤집기를 계산합니다.", "표시 조정") },
            { "attackOriginOffset", ("공격 시작 위치", "오른쪽을 향할 때 발 위치에서 공격 효과가 출발하는 상대 좌표입니다.", "표시 조정") }
        };

        #endregion // 필드

        #region 종류
        /// <summary>
        /// 종류의 데이터 타입입니다.
        /// </summary>
        public static Type GetDataType(DataKind kind)
        {
            return kinds[kind].Type;
        }

        /// <summary>
        /// 종류의 한글 이름입니다.
        /// </summary>
        public static string GetName(DataKind kind)
        {
            return kinds[kind].Name;
        }

        /// <summary>
        /// 종류의 저장 폴더입니다.
        /// </summary>
        public static string GetFolder(DataKind kind)
        {
            return DataFolder + "/" + kinds[kind].Folder;
        }

        /// <summary>
        /// 편집 화면에서 그룹을 표시할 순서입니다. 등록하지 않은 그룹은 뒤에 붙습니다.
        /// </summary>
        public static int GetGroupOrder(string group)
        {
            int index = Array.IndexOf(groupOrder, group);
            return index < 0 ? groupOrder.Length : index;
        }

        /// <summary>
        /// 목록 표시 기준의 한글 이름입니다.
        /// </summary>
        public static string GetName(DataLabelMode mode)
        {
            return labelNames[(int)mode];
        }

        /// <summary>
        /// 목록 정렬 기준의 한글 이름입니다.
        /// </summary>
        public static string GetName(DataSortMode mode)
        {
            return sortNames[(int)mode];
        }

        /// <summary>
        /// 자산의 종류를 찾습니다. 편집기에서 지원하지 않는 자산이면 false입니다.
        /// </summary>
        public static bool TryGetKind(UnityEngine.Object asset, out DataKind kind)
        {
            kind = default;
            if (asset == null)
            {
                return false;
            }
            if (asset is SWCategory)
            {
                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path) && !path.StartsWith(GetFolder(DataKind.EquipmentCategory) + "/", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var pair in kinds)
            {
                if (pair.Value.Type == asset.GetType())
                {
                    kind = pair.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 편집기에서 지원하는 데이터 자산인지 확인합니다.
        /// </summary>
        public static bool IsSupported(UnityEngine.Object asset)
        {
            return TryGetKind(asset, out _);
        }

        #endregion // 종류

        #region 조회
        /// <summary>
        /// 종류의 저장 데이터를 이름순으로 검색합니다. 종류가 없으면 모든 지원 데이터를 검색합니다.
        /// </summary>
        public static List<ScriptableObject> Find(DataKind? kind = null, string search = "")
        {
            var results = new List<ScriptableObject>();
            foreach (var pair in kinds)
            {
                if (kind.HasValue && kind.Value != pair.Key)
                {
                    continue;
                }

                string folder = pair.Key == DataKind.EquipmentCategory ? GetFolder(pair.Key) : DataFolder;
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }
                foreach (string guid in AssetDatabase.FindAssets("t:" + pair.Value.Type.Name, new[] { folder }))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                    string identity = asset is SWIdentifiedObject identified ? identified.CodeName + " " + identified.ID : string.Empty;
                    if (asset != null && SWEditorUtils.MatchesFilter(asset.name + " " + GetDisplayName(asset) + " " + identity + " " + pair.Value.Name, search))
                    {
                        results.Add(asset);
                    }
                }
            }

            return results.OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 게임 표시 이름을 읽습니다. 이름 필드가 없거나 비어 있으면 파일 이름을 사용합니다.
        /// </summary>
        public static string GetDisplayName(ScriptableObject asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            using (var serialized = new SerializedObject(asset))
            {
                var property = serialized.FindProperty("displayName");
                return property != null && !string.IsNullOrWhiteSpace(property.stringValue) ? property.stringValue : asset.name;
            }
        }

        /// <summary>
        /// 항목의 한글 이름·설명·그룹을 반환합니다. 미등록 필드는 원래 이름과 기타 그룹입니다.
        /// </summary>
        public static (string Label, string Description, string Group) Describe(string path)
        {
            string root = path.Split('.')[0];
            return fields.TryGetValue(root, out var description) ? description : (root, string.Empty, "기타");
        }

        /// <summary>
        /// 데이터·장면·프리팹에서 직접 참조하는 사용처를 찾습니다. 저장되지 않은 대상이면 빈 목록입니다.
        /// </summary>
        public static List<string> FindUsages(UnityEngine.Object target)
        {
            string targetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(targetPath))
            {
                return new List<string>();
            }

            return AssetDatabase.FindAssets("t:ScriptableObject t:Prefab t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != targetPath)
                .Where(path => AssetDatabase.GetDependencies(path, false).Contains(targetPath))
                .Distinct()
                .OrderBy(path => path)
                .ToList();
        }

        #endregion // 조회

        #region 검사
        /// <summary>
        /// 데이터와 연결된 지원 데이터를 모두 검사합니다. 항목의 한글 이름을 붙인 문제 목록을 반환합니다.
        /// </summary>
        public static List<DataIssue> CollectIssues(ScriptableObject asset, ScriptableObject source = null)
        {
            var issues = new List<DataIssue>();
            Collect(asset, issues, new HashSet<ScriptableObject>());
            if (asset is ItemData item && item.Equipment != null)
            {
                item.ValidateEquipmentIdentity(Find(DataKind.Item).OfType<ItemData>(), source as ItemData, issues);
            }
            if (asset is SWIdentifiedObject identified)
            {
                foreach (var other in Find().OfType<SWIdentifiedObject>())
                {
                    if (other == asset || other == source || other.GetType() != asset.GetType())
                    {
                        continue;
                    }

                    if (other.CodeName == identified.CodeName)
                    {
                        issues.Add(new DataIssue(asset, "codeName", "코드명이 중복되었습니다: " + other.name));
                    }
                    if (identified.ID != 0 && other.ID == identified.ID)
                    {
                        issues.Add(new DataIssue(asset, "id", "식별 번호가 중복되었습니다: " + other.name));
                    }
                }
            }
            for (int index = 0; index < issues.Count; index++)
            {
                DataIssue issue = issues[index];
                issues[index] = new DataIssue(issue.Asset, issue.PropertyPath, Describe(issue.PropertyPath).Label + ": " + issue.Message);
            }

            return issues;
        }

        /// <summary>
        /// 이미 검사한 데이터는 건너뛰어 순환 참조를 방지합니다.
        /// </summary>
        private static void Collect(ScriptableObject asset, List<DataIssue> issues, HashSet<ScriptableObject> visited)
        {
            if (asset == null || !visited.Add(asset))
            {
                return;
            }

            if (asset is ProjectData projectData)
            {
                projectData.Validate(issues);
            }
            if (asset is SWIdentifiedObject identified)
            {
                if (string.IsNullOrWhiteSpace(identified.CodeName))
                {
                    issues.Add(new DataIssue(asset, "codeName", "코드명을 입력하세요."));
                }
                using (var identity = new SerializedObject(asset))
                {
                    if (string.IsNullOrWhiteSpace(identity.FindProperty("displayName").stringValue))
                    {
                        issues.Add(new DataIssue(asset, "displayName", "표시 이름을 입력하세요."));
                    }
                }
            }
            if (asset is SWStat stat)
            {
                if (float.IsNaN(stat.MinValue) || float.IsInfinity(stat.MinValue)
                    || float.IsNaN(stat.MaxValue) || float.IsInfinity(stat.MaxValue) || stat.MinValue > stat.MaxValue)
                {
                    issues.Add(new DataIssue(asset, "maxValue", "유한한 최솟값·최댓값을 순서에 맞게 입력하세요."));
                }
                if (float.IsNaN(stat.DefaultValue) || float.IsInfinity(stat.DefaultValue)
                    || stat.DefaultValue < stat.MinValue || stat.DefaultValue > stat.MaxValue)
                {
                    issues.Add(new DataIssue(asset, "defaultValue", "기본값을 허용 범위 안에 입력하세요."));
                }
            }
            using (var serialized = new SerializedObject(asset))
            {
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference
                        && iterator.objectReferenceValue is ScriptableObject reference
                        && IsSupported(reference))
                    {
                        Collect(reference, issues, visited);
                    }
                }
            }
        }

        #endregion // 검사
    }
}
