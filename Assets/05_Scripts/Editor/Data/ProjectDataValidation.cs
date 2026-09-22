using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 오류가 발생한 데이터와 항목을 기록합니다. 원본 데이터를 변경하지 않습니다.
    /// </summary>
    public readonly struct ProjectDataIssue
    {
        #region 프로퍼티
        /// <summary>
        /// 오류가 발생한 데이터입니다.
        /// </summary>
        public ScriptableObject Asset { get; }

        /// <summary>
        /// 오류 위치의 직렬화 경로입니다.
        /// </summary>
        public string PropertyPath { get; }

        /// <summary>
        /// 수정 방법을 포함한 한글 설명입니다.
        /// </summary>
        public string Message { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검사 결과의 위치와 설명을 보관합니다.
        /// </summary>
        public ProjectDataIssue(ScriptableObject asset, string propertyPath, string message)
        {
            Asset = asset;
            PropertyPath = propertyPath;
            Message = message;
        }

        #endregion // 초기화
    }

    /// <summary>
    /// 현재 전투에서 사용하는 수치·참조·애니메이션·경로를 검사합니다. 오류 목록만 반환하며 수치를 보정하지 않습니다.
    /// </summary>
    public static class ProjectDataValidation
    {
        #region 검사
        /// <summary>
        /// 데이터와 연결된 지원 데이터를 검사합니다. null 또는 미지원 데이터는 오류로 반환합니다.
        /// </summary>
        public static List<ProjectDataIssue> Validate(ScriptableObject asset)
        {
            var issues = new List<ProjectDataIssue>();
            Inspect(asset, issues, new HashSet<ScriptableObject>());
            return issues;
        }

        /// <summary>
        /// 이미 방문한 참조는 건너뛰어 중복 검사와 순환을 방지합니다.
        /// </summary>
        private static void Inspect(
            ScriptableObject asset,
            List<ProjectDataIssue> issues,
            HashSet<ScriptableObject> visited)
        {
            if (asset == null || ProjectDataCatalog.GetKind(asset) < 0)
            {
                issues.Add(new ProjectDataIssue(asset, string.Empty, "지원하는 데이터가 필요합니다."));
                return;
            }

            if (!visited.Add(asset))
            {
                return;
            }

            using (var serialized = new SerializedObject(asset))
            {
                if (asset is UnitClassData || asset is UnitEnemyData)
                {
                    foreach (string path in new[]
                    {
                        "maximumHealth",
                        "moveSpeed",
                        "attackDamage",
                        "attackRange",
                        "attackInterval"
                    })
                    {
                        Number(serialized, path, false, issues);
                    }

                    Name(serialized, issues);
                    Required(serialized, "prefab", issues);
                    var unit = (UnitData)asset;
                    if (unit.Animation == null || !unit.Animation.IsConfigured)
                    {
                        Add(serialized, "prefab", "유닛 애니메이터·클립·타격 이벤트 연결을 확인하세요.", issues);
                    }

                    if (unit.Prefab != null
                        && (unit is UnitClassData
                        && unit.Prefab.GetComponent<CharacterUnit>() == null
                        || unit is UnitEnemyData
                        && unit.Prefab.GetComponent<EnemyUnit>() == null))
                    {
                        Add(serialized, "prefab", "데이터 종류에 맞는 캐릭터 또는 적 컴포넌트가 필요합니다.", issues);
                    }

                    Appearance(serialized, issues);
                }

                if (asset is UnitClassData)
                {
                    Name(serialized, issues);
                    Number(serialized, "deploymentCost", true, issues);
                    Number(serialized, "blockCapacity", true, issues);
                    Number(serialized, "revivalSeconds", false, issues);
                }
                else if (asset is UnitEnemyData)
                {
                    Number(serialized, "workshopAttackDamage", false, issues);
                    Number(serialized, "workshopAttackInterval", false, issues);
                    Rewards(serialized, issues);
                }
                else if (asset is StageData)
                {
                    Name(serialized, issues);
                    Number(serialized, "startingCurrency", true, issues);
                    Required(serialized, "deploymentCurrency", issues);
                    Number(serialized, "workshopMaximumHealth", false, issues);
                    Number(serialized, "spawnInterval", false, issues);
                    Required(serialized, "enemyRoute", issues);
                    Required(serialized, "enemy", issues);
                    var rounds = serialized.FindProperty("enemiesPerRound");
                    if (rounds.arraySize == 0)
                    {
                        Add(serialized, rounds.propertyPath, "라운드를 한 개 이상 추가하세요.", issues);
                    }

                    for (int index = 0; index < rounds.arraySize; index++)
                    {
                        Number(serialized, rounds.GetArrayElementAtIndex(index).propertyPath, false, issues);
                    }

                    ReferenceArray(serialized, "classes", true, issues);
                }
                else if (asset is RewardData)
                {
                    Name(serialized, issues);
                    Number(serialized, "defaultAmount", true, issues);
                }
                else if (asset is EnemyRouteData)
                {
                    Route(serialized, issues);
                }

                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference
                        && iterator.objectReferenceValue is ScriptableObject reference
                        && ProjectDataCatalog.GetKind(reference) >= 0)
                    {
                        Inspect(reference, issues, visited);
                    }
                }
            }
        }

        /// <summary>
        /// 복수 보상의 참조와 개별 수량·독립 확률을 검사합니다. 보상이 없는 적은 허용합니다.
        /// </summary>
        private static void Rewards(SerializedObject serialized, List<ProjectDataIssue> issues)
        {
            var rewards = serialized.FindProperty("rewards");
            for (int index = 0; index < rewards.arraySize; index++)
            {
                var entry = rewards.GetArrayElementAtIndex(index);
                Required(serialized, entry.FindPropertyRelative("definition").propertyPath, issues);
                if (entry.FindPropertyRelative("useAmountOverride").boolValue)
                {
                    Number(serialized, entry.FindPropertyRelative("overrideAmount").propertyPath, true, issues);
                }

                var probability = entry.FindPropertyRelative("acquisitionProbability");
                float value = probability.floatValue;
                if (!Finite(value) || value < 0f || value > 100f)
                {
                    Add(serialized, probability.propertyPath, "획득 확률을 0부터 100 사이로 입력하세요.", issues);
                }
            }
        }

        /// <summary>
        /// 표시 이름의 빈 문자열을 검사합니다.
        /// </summary>
        private static void Name(SerializedObject serialized, List<ProjectDataIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(serialized.FindProperty("displayName").stringValue))
            {
                Add(serialized, "displayName", "표시 이름을 입력하세요.", issues);
            }
        }

        /// <summary>
        /// 실수와 정수의 유한성 및 허용 하한을 검사합니다.
        /// </summary>
        private static void Number(
            SerializedObject serialized,
            string path,
            bool allowZero,
            List<ProjectDataIssue> issues)
        {
            var property = serialized.FindProperty(path);
            double value = property.propertyType == SerializedPropertyType.Integer ? property.longValue : property.doubleValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0d : value <= 0d))
            {
                Add(serialized, path, allowZero ? "0 이상의 유한한 수를 입력하세요." : "0보다 큰 유한한 수를 입력하세요.", issues);
            }
        }

        /// <summary>
        /// 필수 객체 참조의 누락을 검사합니다.
        /// </summary>
        private static void Required(SerializedObject serialized, string path, List<ProjectDataIssue> issues)
        {
            if (serialized.FindProperty(path).objectReferenceValue == null)
            {
                Add(serialized, path, "필수 참조를 연결하세요.", issues);
            }
        }

        /// <summary>
        /// 목록의 최소 개수와 비어 있는 참조를 검사합니다.
        /// </summary>
        private static void ReferenceArray(
            SerializedObject serialized,
            string path,
            bool required,
            List<ProjectDataIssue> issues)
        {
            var property = serialized.FindProperty(path);
            if (required && property.arraySize == 0)
            {
                Add(serialized, path, "항목을 한 개 이상 추가하세요.", issues);
            }

            for (int index = 0; index < property.arraySize; index++)
            {
                Required(serialized, property.GetArrayElementAtIndex(index).propertyPath, issues);
            }
        }

        /// <summary>
        /// 경로의 최소 개수·좌표·인접 구간 길이를 원본 변경 없이 검사합니다.
        /// </summary>
        private static void Route(SerializedObject serialized, List<ProjectDataIssue> issues)
        {
            var points = serialized.FindProperty("points");
            if (points.arraySize < 2)
            {
                Add(serialized, "points", "경유점은 두 개 이상 필요합니다.", issues);
            }

            float total = 0f;
            Vector2 previous = Vector2.zero;
            for (int index = 0; index < points.arraySize; index++)
            {
                var property = points.GetArrayElementAtIndex(index);
                Vector2 point = property.vector2Value;
                if (!Finite(point.x) || !Finite(point.y))
                {
                    Add(serialized, property.propertyPath, "좌표는 유한한 수여야 합니다.", issues);
                }

                if (index > 0)
                {
                    float nextTotal = total + Vector2.Distance(previous, point);
                    if (!Finite(nextTotal) || nextTotal <= total)
                    {
                        Add(serialized, property.propertyPath, "인접 점을 구분하고 계산 가능한 경로 길이를 지정하세요.", issues);
                    }

                    total = nextTotal;
                }

                previous = point;
            }
        }

        /// <summary>
        /// 유닛 표시 위치와 색상에 유한한 값이 설정되었는지 검사합니다.
        /// </summary>
        private static void Appearance(SerializedObject serialized, List<ProjectDataIssue> issues)
        {
            foreach (string path in new[]
            {
                "feetOffset",
                "healthBarHeight"
            })
            {
                if (!Finite(serialized.FindProperty(path).floatValue))
                {
                    Add(serialized, path, "유한한 수를 입력하세요.", issues);
                }
            }

            Vector2 offset = serialized.FindProperty("attackOriginOffset").vector2Value;
            Color tint = serialized.FindProperty("tint").colorValue;
            if (!Finite(offset.x) || !Finite(offset.y))
            {
                Add(serialized, "attackOriginOffset", "좌표는 유한한 수여야 합니다.", issues);
            }

            if (!Finite(tint.r) || !Finite(tint.g) || !Finite(tint.b) || !Finite(tint.a))
            {
                Add(serialized, "tint", "색상은 유한한 수여야 합니다.", issues);
            }
        }

        /// <summary>
        /// 오류에 한글 항목명을 붙여 결과 목록에 추가합니다.
        /// </summary>
        private static void Add(SerializedObject serialized, string path, string message, List<ProjectDataIssue> issues)
        {
            issues.Add(new ProjectDataIssue(serialized.targetObject as ScriptableObject, path, ProjectDataCatalog.Describe(path)[0] + ": " + message));
        }

        /// <summary>
        /// 좌표와 표시 수치의 유한성을 확인합니다.
        /// </summary>
        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion // 검사
    }
}
