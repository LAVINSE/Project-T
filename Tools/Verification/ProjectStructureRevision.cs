using ProjectT.Editor;

/// <summary>Unity CLI에서 구조 개편을 실행합니다.</summary>
public static class ProjectStructureRevision
{
    /// <summary>관리자와 장면·프리팹 연결을 갱신합니다.</summary>
    public static string Apply() => ProjectSceneSetup.Apply();

    /// <summary>유닛 프리팹의 공통 체력바 인스턴스 크기와 참조만 갱신합니다.</summary>
    public static string RefreshUnitHealthBars()
    {
        foreach (string name in new[] { "AllyUnit", "EnemyUnit" })
        {
            string path = "Assets/04_Prefabs/Units/" + name + ".prefab";
            var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
            try
            {
                HealthBarPrefabSetup.ConfigureUnit(root);
                UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
        }
        UnityEditor.AssetDatabase.SaveAssets();
        return "유닛 체력바 크기·참조 갱신 완료";
    }
}
