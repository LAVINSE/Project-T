using UnityEngine;

namespace ProjectT
{
    /// <summary>
    /// 여러 스크립트가 함께 사용하는 애니메이터 파라미터·전투 상수·표시 색상을 한 곳에서 관리합니다.
    /// </summary>
    public static class ProjectDefine
    {
        #region 애니메이터 파라미터
        /// <summary>
        /// 애니메이터 파라미터 해시입니다. 문자열 변환을 한 번만 수행합니다.
        /// </summary>
        public static class AnimatorHash
        {
            /// <summary>
            /// 유닛 이동 상태 Bool입니다.
            /// </summary>
            public static readonly int Walk = Animator.StringToHash("Walk");

            /// <summary>
            /// 유닛 사망 상태 Bool입니다.
            /// </summary>
            public static readonly int Death = Animator.StringToHash("Death");

            /// <summary>
            /// 공방 피격 Trigger입니다.
            /// </summary>
            public static readonly int Hit = Animator.StringToHash("Hit");

            /// <summary>
            /// 공방 선택지 알림 Bool입니다.
            /// </summary>
            public static readonly int Alert = Animator.StringToHash("Alert");
        }

        #endregion // 애니메이터 파라미터

        #region 유닛 동작
        /// <summary>
        /// 코드가 직접 재생하는 고정 유닛 동작의 SWCategory 코드명입니다. 그 밖의 동작은 SWCategory 자산만 추가합니다.
        /// </summary>
        public static class UnitAction
        {
            /// <summary>
            /// 동작 SWCategory 자산 폴더입니다.
            /// </summary>
            public const string Folder = "Assets/02_Res/Data/UnitAction";

            /// <summary>
            /// 대기 동작입니다.
            /// </summary>
            public const string Idle = "Idle";

            /// <summary>
            /// 이동 동작입니다.
            /// </summary>
            public const string Walk = "Walk";

            /// <summary>
            /// 기본 근접 공격 동작입니다.
            /// </summary>
            public const string MeleeAttack = "MeleeAttack";

            /// <summary>
            /// 사망 동작입니다.
            /// </summary>
            public const string Death = "Death";

            /// <summary>
            /// 모든 유닛이 반드시 가져야 하는 동작입니다.
            /// </summary>
            public static readonly string[] Fixed = { Idle, Walk, MeleeAttack, Death };
        }

        #endregion // 유닛 동작

        #region 장면
        /// <summary>
        /// 장면 경로와 편집기 시작 장면 기록 키입니다.
        /// </summary>
        public static class Scene
        {
            /// <summary>
            /// 공통 관리자를 준비하는 시작 장면입니다.
            /// </summary>
            public const string MainPath = "Assets/01_Scenes/Main.unity";

            /// <summary>
            /// 편집기에서 Play 직전에 열려 있던 장면 경로를 보관하는 SessionState 키입니다.
            /// </summary>
            public const string EditorReturnSceneKey = "ProjectT.EditorReturnScene";
        }

        #endregion // 장면

        #region 전투
        /// <summary>
        /// 전투 규칙과 표시 크기에 사용하는 상수입니다.
        /// </summary>
        public static class Battle
        {
            /// <summary>
            /// 배속 버튼이 순환하는 최대 배속입니다.
            /// </summary>
            public const int MaximumSpeedMultiplier = 3;

            /// <summary>
            /// 사망한 아군 그림의 투명도입니다.
            /// </summary>
            public const float DeadUnitAlpha = 0.55f;

            /// <summary>
            /// 유닛 선택 원의 가로·세로 반지름입니다.
            /// </summary>
            public static readonly Vector2 SelectionRingRadius = new Vector2(0.55f, 0.23f);

            /// <summary>
            /// 배치 위치 원의 반지름입니다.
            /// </summary>
            public const float PlacementRingRadius = 0.5f;

            /// <summary>
            /// 이동 목적지 원의 반지름입니다.
            /// </summary>
            public const float DestinationRingRadius = 0.48f;

            /// <summary>
            /// 이동 목적지 십자 표시의 길이입니다.
            /// </summary>
            public const float DestinationCrossLength = 0.72f;

            /// <summary>
            /// 클릭으로 아군을 선택하는 가로 반폭입니다.
            /// </summary>
            public const float SelectionHalfWidth = 0.65f;

            /// <summary>
            /// 클릭으로 아군을 선택할 때 발 아래로 허용하는 높이입니다.
            /// </summary>
            public const float SelectionFootMargin = 0.3f;

            /// <summary>
            /// 스프라이트 원본의 기준 픽셀 밀도입니다.
            /// </summary>
            public const float PixelsPerUnit = 32f;

            /// <summary>
            /// 지면 높이로 계산하는 그리기 순서의 최대 절댓값입니다.
            /// </summary>
            public const int SortingOrderLimit = 1800;
        }

        #endregion // 전투

        #region 색상
        /// <summary>
        /// 전장 표시에 사용하는 고정 색상입니다. 체력바 색상은 ColorData에서 관리합니다.
        /// </summary>
        public static class Palette
        {
            /// <summary>
            /// 근접 공격 궤적의 시작 색상입니다.
            /// </summary>
            public static readonly Color MeleeTrace = new Color(1f, 0.86f, 0.45f);

            /// <summary>
            /// 원거리 공격 궤적의 시작 색상입니다.
            /// </summary>
            public static readonly Color RangedTrace = new Color(0.3f, 1f, 0.8f);

            /// <summary>
            /// 교전 가능한 아군의 공격 범위 색상입니다.
            /// </summary>
            public static readonly Color ActiveRange = new Color(1f, 0.86f, 0.4f, 0.8f);

            /// <summary>
            /// 이동·부활 중인 아군의 공격 범위 색상입니다.
            /// </summary>
            public static readonly Color InactiveRange = new Color(0.7f, 0.75f, 0.8f, 0.6f);

            /// <summary>
            /// 배치 가능한 위치의 미리보기 색상입니다.
            /// </summary>
            public static readonly Color ValidPlacement = new Color(0.4f, 1f, 0.72f, 0.8f);

            /// <summary>
            /// 배치할 수 없는 위치의 미리보기 색상입니다.
            /// </summary>
            public static readonly Color InvalidPlacement = new Color(1f, 0.3f, 0.28f, 0.8f);

            /// <summary>
            /// 배치 가능한 위치에서 반투명하게 표시하는 캐릭터 색상입니다.
            /// </summary>
            public static readonly Color PreviewCharacter = new Color(1f, 1f, 1f, 0.8f);

            /// <summary>
            /// 파괴된 공방 그림의 색상입니다.
            /// </summary>
            public static readonly Color DestroyedWorkshop = new Color(0.42f, 0.43f, 0.46f);
        }

        #endregion // 색상
    }
}
