using System;

using ProjectT.Data;

namespace ProjectT.Editor
{
    /// <summary>
    /// 프로젝트 제작 도구에서 사용하는 종류별 자산 경로입니다.
    /// </summary>
    public static class ProjectAssetPaths
    {
        #region 필드
        /// <summary>
        /// 공방 프리팹 경로입니다.
        /// </summary>
        public const string Workshop = "Assets/04_Prefabs/Workshop/ArcaneWorkshop.prefab";

        /// <summary>
        /// 공통 캐릭터 체력바 경로입니다.
        /// </summary>
        public const string CommonHealthBar = "Assets/04_Prefabs/Health/CommonHealthBar.prefab";

        /// <summary>
        /// 공방 체력바 경로입니다.
        /// </summary>
        public const string ArcaneHealthBar = "Assets/04_Prefabs/Health/ArcaneHealthBar.prefab";

        /// <summary>
        /// 전장 선 표시 머티리얼 경로입니다.
        /// </summary>
        public const string BattleLine = "Assets/02_Res/Materials/BattleLine.mat";

        /// <summary>
        /// 적 이동 경로 데이터 경로입니다.
        /// </summary>
        public const string EnemyRoute = "Assets/02_Res/Data/Navigation/Stage01EnemyRoute.asset";

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 데이터 종류에 맞는 저장 위치를 반환합니다.
        /// </summary>
        public static string Data(Type type, string name)
        {
            string category = type == typeof(StageDefinition)
                ? "Stage"
                : type == typeof(UnitAppearance)
                    ? "Appearance"
                    : type == typeof(EnemyRouteDefinition)
                        ? "Navigation"
                        : "Units";
            return "Assets/02_Res/Data/" + category + "/" + name + ".asset";
        }

        #endregion // 함수
    }
}
