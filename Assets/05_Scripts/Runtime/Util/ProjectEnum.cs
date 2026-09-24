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
        Stat
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
