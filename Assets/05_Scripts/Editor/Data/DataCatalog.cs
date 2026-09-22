using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
        public const string DataFolder = "Assets/02_Res/Data";

        private static readonly Dictionary<DataKind, (Type Type, string Name, string Folder)> kinds = new Dictionary<DataKind, (Type, string, string)>
        {
            { DataKind.Class, (typeof(UnitClassData), "클래스", "Character") },
            { DataKind.Enemy, (typeof(UnitEnemyData), "적", "Enemy") },
            { DataKind.Stage, (typeof(StageData), "스테이지", "Stage") },
            { DataKind.Route, (typeof(EnemyRouteData), "경로", "Navigation") },
            { DataKind.Currency, (typeof(CurrencyData), "재화", "Currency") },
            { DataKind.Item, (typeof(ItemData), "아이템", "Items") }
        };

        private static readonly Dictionary<string, (string Label, string Description, string Group)> fields = new Dictionary<string, (string, string, string)>
        {
            { "displayName", ("이름", "게임 화면에 표시할 이름입니다.", "기본 정보") },
            { "icon", ("아이콘", "선택적으로 표시할 재화·아이템 그림입니다.", "기본 정보") },
            { "deploymentCost", ("배치 비용", "캐릭터 한 명을 구매할 때 필요한 전투 재화입니다. 0 이상이어야 합니다.", "기본 정보") },
            { "defaultAmount", ("기본 수량", "보상 항목에서 수량 덮어쓰기를 끄면 사용하는 기본값입니다. 소유 잔액이 아닙니다.", "보상 기본값") },
            { "maximumHealth", ("최대 체력", "새 생명에 부여하는 체력입니다. 0보다 커야 합니다.", "전투 능력") },
            { "moveSpeed", ("이동속도", "게임 시간 1초당 이동하는 월드 거리입니다.", "전투 능력") },
            { "attackDamage", ("공격 피해", "상대 유닛에게 한 번 타격할 때 주는 피해입니다.", "전투 능력") },
            { "attackRange", ("공격 거리", "월드 거리 단위의 공격 범위입니다.", "전투 능력") },
            { "attackInterval", ("공격 간격 (초)", "공격 시작 사이의 게임 시간입니다. 0보다 커야 합니다.", "전투 능력") },
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
        /// 자산의 종류를 찾습니다. 편집기에서 지원하지 않는 자산이면 false입니다.
        /// </summary>
        public static bool TryGetKind(UnityEngine.Object asset, out DataKind kind)
        {
            kind = default;
            if (asset == null)
            {
                return false;
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
        public static List<ProjectData> Find(DataKind? kind = null, string search = "")
        {
            var results = new List<ProjectData>();
            foreach (var pair in kinds)
            {
                if (kind.HasValue && kind.Value != pair.Key)
                {
                    continue;
                }

                foreach (string guid in AssetDatabase.FindAssets("t:" + pair.Value.Type.Name, new[] { DataFolder }))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ProjectData>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset != null && SWEditorUtils.MatchesFilter(asset.name + " " + GetDisplayName(asset) + " " + pair.Value.Name, search))
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

            string[] folders = { DataFolder, "Assets/01_Scenes", "Assets/04_Prefabs" };
            return AssetDatabase.FindAssets(string.Empty, folders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != targetPath && (path.EndsWith(".asset") || path.EndsWith(".unity") || path.EndsWith(".prefab")))
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
        public static List<DataIssue> CollectIssues(ProjectData asset)
        {
            var issues = new List<DataIssue>();
            Collect(asset, issues, new HashSet<ProjectData>());
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
        private static void Collect(ProjectData asset, List<DataIssue> issues, HashSet<ProjectData> visited)
        {
            if (asset == null || !visited.Add(asset))
            {
                return;
            }

            asset.Validate(issues);
            using (var serialized = new SerializedObject(asset))
            {
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference
                        && iterator.objectReferenceValue is ProjectData reference
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
