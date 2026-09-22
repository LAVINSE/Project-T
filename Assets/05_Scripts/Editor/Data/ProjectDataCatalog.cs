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
    /// 기존 전투 데이터의 종류, 제작 폴더와 한글 편집 항목을 제공합니다. 지원하지 않는 종류는 반환하지 않습니다.
    /// </summary>
    public static class ProjectDataCatalog
    {
        #region 필드
        public const string DataFolder = "Assets/02_Res/Data";
        private static readonly Type[] supportedTypes =
        {
            typeof(UnitClassData),
            typeof(UnitEnemyData),
            typeof(StageData),
            typeof(EnemyRouteData),
            typeof(CurrencyData),
            typeof(ItemData)
        };
        private static readonly string[] kindNames =
        {
            "클래스",
            "적",
            "스테이지",
            "경로",
            "재화",
            "아이템"
        };
        private static readonly Dictionary<string, string[]> fieldDescriptions = new Dictionary<string, string[]>
        {
            {
                "prefab",
                new[]
                {
                    "유닛 프리팹",
                    "이 종류의 프리팹입니다. 애니메이터와 타격 이벤트는 프리팹 및 클립에서 설정합니다.",
                    "외형"
                }
            },
            {
                "deploymentCurrency",
                new[]
                {
                    "배치 재화",
                    "이 자산에 해당하는 보상만 현재 전투 지갑에 지급합니다. 시작 금액과 배치 비용도 이 재화를 사용합니다.",
                    "전투 경제"
                }
            },
            {
                "defaultAmount",
                new[]
                {
                    "기본 수량",
                    "보상 항목에서 수량 덮어쓰기를 끄면 사용하는 기본값입니다. 소유 잔액이 아닙니다.",
                    "보상 기본값"
                }
            },
            {
                "icon",
                new[]
                {
                    "아이콘",
                    "선택적으로 표시할 재화·아이템 그림입니다.",
                    "기본 정보"
                }
            },
            {
                "displayName",
                new[]
                {
                    "이름",
                    "게임 화면에 표시할 이름입니다.",
                    "기본 정보"
                }
            },
            {
                "deploymentCost",
                new[]
                {
                    "배치 비용",
                    "캐릭터 한 명을 구매할 때 필요한 전투 재화입니다. 0 이상이어야 합니다.",
                    "기본 정보"
                }
            },
            {
                "maximumHealth",
                new[]
                {
                    "최대 체력",
                    "새 생명에 부여하는 체력입니다. 0보다 커야 합니다.",
                    "전투 능력"
                }
            },
            {
                "moveSpeed",
                new[]
                {
                    "이동속도",
                    "게임 시간 1초당 이동하는 월드 거리입니다.",
                    "전투 능력"
                }
            },
            {
                "attackDamage",
                new[]
                {
                    "공격 피해",
                    "상대 유닛에게 한 번 타격할 때 주는 피해입니다.",
                    "전투 능력"
                }
            },
            {
                "attackRange",
                new[]
                {
                    "공격 거리",
                    "월드 거리 단위의 공격 범위입니다.",
                    "전투 능력"
                }
            },
            {
                "attackInterval",
                new[]
                {
                    "공격 간격 (초)",
                    "공격 시작 사이의 게임 시간입니다. 0보다 커야 합니다.",
                    "전투 능력"
                }
            },
            {
                "blockCapacity",
                new[]
                {
                    "동시 저지 수",
                    "동시에 붙잡을 적 수입니다. 0이면 적을 저지하지 않습니다.",
                    "전투 능력"
                }
            },
            {
                "revivalSeconds",
                new[]
                {
                    "부활 대기 (초)",
                    "사망 후 무료 부활까지의 게임 시간입니다.",
                    "전투 능력"
                }
            },
            {
                "workshopAttackDamage",
                new[]
                {
                    "공방 공격 피해",
                    "공방에 한 번 타격할 때 주는 피해입니다. 아군 대상 피해와 별개입니다.",
                    "공방 공격"
                }
            },
            {
                "workshopAttackInterval",
                new[]
                {
                    "공방 공격 간격 (초)",
                    "공방 공격 시작 사이의 게임 시간입니다.",
                    "공방 공격"
                }
            },
            {
                "rewards",
                new[]
                {
                    "처치 보상",
                    "각 항목의 획득 확률을 독립 판정합니다. 100%는 항상, 0%는 지급하지 않습니다.",
                    "보상"
                }
            },
            {
                "startingCurrency",
                new[]
                {
                    "시작 재화",
                    "새 전투에 지급하는 배치 재화입니다. 0 이상이어야 합니다.",
                    "전투 시작"
                }
            },
            {
                "workshopMaximumHealth",
                new[]
                {
                    "공방 최대 체력",
                    "새 전투에서 공방에 부여하는 체력입니다.",
                    "전투 시작"
                }
            },
            {
                "spawnInterval",
                new[]
                {
                    "적 생성 간격 (초)",
                    "한 라운드에서 적을 생성하는 게임 시간 간격입니다.",
                    "라운드"
                }
            },
            {
                "enemiesPerRound",
                new[]
                {
                    "라운드별 적 수",
                    "목록의 순서가 라운드 순서입니다. 각 라운드는 적이 한 명 이상이어야 합니다.",
                    "라운드"
                }
            },
            {
                "enemyRoute",
                new[]
                {
                    "적 경로",
                    "입구에서 공방까지 이동할 경로입니다.",
                    "참조"
                }
            },
            {
                "enemy",
                new[]
                {
                    "기본 적",
                    "현재 구조는 스테이지당 기본 적 한 종류를 사용합니다.",
                    "참조"
                }
            },
            {
                "classes",
                new[]
                {
                    "구매 가능 클래스",
                    "이 전투의 구매 목록입니다. 순서를 바꾸면 구매 목록 순서도 바뀝니다.",
                    "참조"
                }
            },
            {
                "points",
                new[]
                {
                    "경유점 (월드 좌표)",
                    "입구부터 공방까지의 순서입니다. 두 점 이상이며 인접 점은 달라야 합니다.",
                    "경로"
                }
            },
            {
                "portrait",
                new[]
                {
                    "초상화 (선택)",
                    "비어 있으면 프리팹의 기본 그림을 사용합니다.",
                    "외형"
                }
            },
            {
                "tint",
                new[]
                {
                    "표시 색상",
                    "원본 그림에 곱할 색상입니다.",
                    "표시 조정"
                }
            },
            {
                "feetOffset",
                new[]
                {
                    "발 높이 보정",
                    "월드 거리 단위의 그림 위치 보정입니다.",
                    "표시 조정"
                }
            },
            {
                "healthBarHeight",
                new[]
                {
                    "체력바 높이",
                    "발 위치로부터 체력바까지의 월드 높이입니다.",
                    "표시 조정"
                }
            },
            {
                "spritesFaceRight",
                new[]
                {
                    "원본이 오른쪽을 향함",
                    "기본 그림 방향에 맞춰 좌우 뒤집기를 계산합니다.",
                    "표시 조정"
                }
            },
            {
                "attackOriginOffset",
                new[]
                {
                    "공격 시작 위치",
                    "오른쪽을 향할 때 발 위치에서 공격 효과가 출발하는 상대 좌표입니다.",
                    "표시 조정"
                }
            }
        };

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 편집기에서 지원하는 전투·재화·아이템 데이터 종류입니다.
        /// </summary>
        public static IReadOnlyList<Type> SupportedTypes => supportedTypes;

        /// <summary>
        /// 종류 선택에 사용할 표시 이름입니다.
        /// </summary>
        public static IReadOnlyList<string> KindNames => kindNames;

        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 데이터 종류의 순서를 찾습니다. 지원하지 않는 객체는 -1을 반환합니다.
        /// </summary>
        public static int GetKind(UnityEngine.Object asset)
        {
            return asset == null ? -1 : Array.IndexOf(supportedTypes, asset.GetType());
        }

        /// <summary>
        /// 종류에 맞는 기존 제작 폴더를 반환합니다. 알 수 없는 종류는 null입니다.
        /// </summary>
        public static string GetFolder(Type type)
        {
            if (type == typeof(UnitClassData))
            {
                return DataFolder + "/Character";
            }

            if (type == typeof(UnitEnemyData))
            {
                return DataFolder + "/Enemy";
            }

            if (type == typeof(StageData))
            {
                return DataFolder + "/Stage";
            }

            if (type == typeof(EnemyRouteData))
            {
                return DataFolder + "/Navigation";
            }

            if (type == typeof(CurrencyData))
            {
                return DataFolder + "/Currency";
            }

            if (type == typeof(ItemData))
            {
                return DataFolder + "/Items";
            }

            return null;
        }

        /// <summary>
        /// 관리 폴더 안의 지원 데이터를 검색합니다. 종류 -1은 전체를 의미하며 검색 결과는 이름순입니다.
        /// </summary>
        public static List<ScriptableObject> Find(int kind = -1, string search = "")
        {
            var results = new List<ScriptableObject>();
            for (int index = 0; index < supportedTypes.Length; index++)
            {
                if (kind >= 0 && kind != index)
                {
                    continue;
                }

                foreach (string identifier in AssetDatabase.FindAssets("t:" + supportedTypes[index].Name, new[] { DataFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(identifier);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset != null
                        && SWEditorUtils.MatchesFilter(asset.name + " " + GetDisplayName(asset) + " " + kindNames[index], search))
                    {
                        results.Add(asset);
                    }
                }
            }

            return results.OrderBy(asset => asset.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 게임 표시 이름을 읽습니다. 이름 필드가 없으면 파일 이름을 사용합니다.
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
        /// 항목의 한글 이름·설명·그룹을 반환합니다. 미등록 필드는 원래 이름으로 표시합니다.
        /// </summary>
        public static string[] Describe(string path)
        {
            string root = path.Split('.')[0];
            return fieldDescriptions.TryGetValue(root, out string[] description)
                ? description
                : new[]
            {
                root,
                string.Empty,
                "기타"
            };
        }

        /// <summary>
        /// 데이터·장면·프리팹에서 직접 참조하는 사용처를 찾습니다. 대상이 저장되지 않았으면 빈 목록입니다.
        /// </summary>
        public static List<string> FindUsages(UnityEngine.Object target)
        {
            string targetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(targetPath))
            {
                return new List<string>();
            }

            string[] folders =
            {
                DataFolder,
                "Assets/01_Scenes",
                "Assets/04_Prefabs"
            };
            return AssetDatabase.FindAssets("", folders).Select(AssetDatabase.GUIDToAssetPath).Where(path => path != targetPath && (path.EndsWith(".asset") || path.EndsWith(".unity") || path.EndsWith(".prefab"))).Where(path => AssetDatabase.GetDependencies(path, false).Contains(targetPath)).Distinct().OrderBy(path => path).ToList();
        }

        #endregion // 조회
    }
}
