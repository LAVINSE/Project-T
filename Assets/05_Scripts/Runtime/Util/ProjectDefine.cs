using UnityEngine;

namespace ProjectT
{
    /// <summary>
    /// 여러 스크립트가 함께 사용하는 애니메이터 파라미터·전투 상수·표시 색상을 한 곳에서 관리합니다.
    /// </summary>
    public static class ProjectDefine
    {
        #region 자산 경로
        /// <summary>
        /// 프로젝트 데이터 자산이 모여 있는 폴더입니다. 하위 폴더 경로는 여기에 이어 붙입니다.
        /// </summary>
        public static class Data
        {
            /// <summary>
            /// 모든 프로젝트 데이터 자산의 최상위 폴더입니다.
            /// </summary>
            public const string Root = "Assets/02_Res/Data";
        }

        #endregion // 자산 경로

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
            public const string Folder = Data.Root + "/UnitAction";

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

        #region 영구 저장
        /// <summary>
        /// 자산 이름·경로와 독립적인 영구 저장 슬롯과 형식 버전입니다.
        /// </summary>
        public static class Save
        {
            /// <summary>
            /// 영구 아이템 수량과 지급 기록을 저장하는 로컬 슬롯입니다.
            /// </summary>
            public const string InventorySlot = "ProjectT_Inventory";

            /// <summary>
            /// 현재 지원하는 영구 인벤토리 저장 형식입니다.
            /// </summary>
            public const int InventoryVersion = 1;

            /// <summary>
            /// 소울 잔액과 지급 기록을 함께 저장하는 로컬 슬롯입니다.
            /// </summary>
            public const string SoulSlot = "ProjectT_Soul";

            /// <summary>
            /// 현재 지원하는 소울 저장 형식입니다.
            /// </summary>
            public const int SoulVersion = 1;
        }

        #endregion // 영구 저장

        #region 인벤토리
        /// <summary>
        /// 인벤토리 목록과 정확한 정수 수량 처리에 사용하는 공통 설정입니다.
        /// </summary>
        public static class Inventory
        {
            /// <summary>
            /// 특별 전리품 획득 시 아이콘의 확대·복귀를 합친 실제 시간입니다.
            /// </summary>
            public const float AcquisitionEffectDuration = 0.5f;

            /// <summary>
            /// 특별 전리품 획득 시 원래 아이콘 크기에 곱하는 최대 확대 배율입니다.
            /// </summary>
            public const float AcquisitionEffectScale = 1.2f;

            /// <summary>
            /// 아이템 보유 여부와 관계없이 표시하는 기본 슬롯 수입니다. 보관 한도가 아닙니다.
            /// </summary>
            public const int MinimumVisibleSlots = 36;

            /// <summary>
            /// 분류 없는 아이템을 표시하는 기타 분류 코드입니다.
            /// </summary>
            public const string OtherCategory = "ItemOther";

            /// <summary>
            /// 편집기가 아이템 자산을 연결하는 실행용 목록 경로입니다.
            /// </summary>
            public const string CatalogPath = Data.Root + "/Common/ItemCatalogData.asset";

            /// <summary>
            /// double 보상 수량에서 정수 정밀도를 보장하는 최대값입니다.
            /// </summary>
            public const double MaximumRewardCount = 9007199254740991d;

            /// <summary>
            /// 메인 스레드에서 한 보상 요청으로 수행할 개별 장비 추첨의 안전 한도입니다. 보유 수량 한도가 아닙니다.
            /// </summary>
            public const int MaximumEquipmentRollCount = 10000;
        }

        #endregion // 인벤토리

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
