namespace ProjectT
{
    /// <summary>
    /// 스테이지 전투의 진행 단계입니다.
    /// </summary>
    public enum BattlePhase
    {
        Preparation,
        Fighting,
        RoundBreak,
        FinalRest,
        Victory,
        Defeat
    }

    /// <summary>
    /// 공유 색상 데이터에서 선택할 체력바 종류입니다.
    /// </summary>
    public enum HealthBarType
    {
        Workshop,
        Character,
        Enemy
    }

    /// <summary>
    /// 연구 효과가 능력치를 올리는 방식입니다. 계산이 달라지므로 코드가 값마다 다르게 처리합니다.
    /// </summary>
    public enum ResearchValueMode
    {
        /// <summary>능력치에 고정값을 더합니다.</summary>
        [UnityEngine.InspectorName("고정값")] Flat,

        /// <summary>클래스 기본값에 비율을 곱한 값을 더합니다. 0.1은 10%입니다.</summary>
        [UnityEngine.InspectorName("비율 (클래스 기본값 기준)")] Percent
    }

    /// <summary>
    /// 데이터 편집기가 다루는 데이터 종류입니다. 순서는 편집기 메뉴 순서와 같습니다.
    /// </summary>
    public enum DataKind
    {
        Class,
        Enemy,
        Stage,
        Route,
        Currency,
        Item,
        Equipment,
        EquipmentStatEffect,
        EquipmentCategory,
        Stat,
        CraftingRecipe,
        CraftingCost
    }

    /// <summary>
    /// 데이터 편집기 목록에 어떤 이름을 표시할지 고르는 기준입니다.
    /// </summary>
    public enum DataLabelMode
    {
        FileName,
        DisplayName,
        CodeName
    }

    /// <summary>
    /// 데이터 편집기 목록을 어떤 기준으로 정렬할지 고르는 기준입니다.
    /// </summary>
    public enum DataSortMode
    {
        FileName,
        DisplayName,
        CodeName,
        Identifier
    }
}
