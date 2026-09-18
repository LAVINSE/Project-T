using System;
using System.IO;
using System.Linq;
using UnityEditor;

/// <summary>교체가 끝난 이전 공방과 이전 에이전트 임시 장면만 정리합니다.</summary>
public static class RemoveObsoleteProjectAssets
{
    /// <summary>참조를 검사한 뒤 구형 자산과 빈 분류 폴더를 제거합니다.</summary>
    public static string Apply()
    {
        string oldWorkshop = "Assets/04_Prefabs/Defense/MobileArcaneWorkshop.prefab";
        string oldHealth = "Assets/05_Scripts/Runtime/Presentation/WorkshopHealthPresentation.cs";
        string backupScene = "Assets/Temp/Stage01_BeforeDeploymentInteraction.unity";
        string archive = "Logs/Development/2026-09-18/StructureRevision/Stage01_BeforeDeploymentInteraction.unity";
        string[] activeAssets = new[] { "Assets/01_Scenes/Main.unity", "Assets/01_Scenes/Stage01_Grassland.unity" }
            .Concat(AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/04_Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).Where(path => path != oldWorkshop)).ToArray();
        foreach (string asset in activeAssets)
        {
            string[] dependencies = AssetDatabase.GetDependencies(asset, true);
            if (dependencies.Contains(oldWorkshop) || dependencies.Contains(oldHealth)) throw new Exception("이전 공방 참조가 남아 있습니다: " + asset);
        }
        if (File.Exists(backupScene))
        {
            if (!File.Exists(archive)) File.Copy(backupScene, archive);
            if (!AssetDatabase.DeleteAsset(backupScene)) throw new Exception("임시 장면 제거 실패");
        }
        foreach (string path in new[] { oldWorkshop, oldHealth })
            if (File.Exists(path) && !AssetDatabase.DeleteAsset(path)) throw new Exception("제거 실패: " + path);
        foreach (string root in new[] { "Assets/02_Res", "Assets/04_Prefabs", "Assets/05_Scripts" })
            foreach (string folder in Directory.GetDirectories(root, "*", SearchOption.AllDirectories).OrderByDescending(value => value.Length))
                if (!Directory.EnumerateFileSystemEntries(folder).Any()) AssetDatabase.DeleteAsset(folder.Replace('\\', '/'));
        var assets = AssetDatabase.FindAssets("", new[] { "Assets/02_Res/Data", "Assets/04_Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).Where(path => !AssetDatabase.IsValidFolder(path));
        AssetDatabase.ForceReserializeAssets(assets);
        AssetDatabase.SaveAssets();
        return "현재 자산 참조 검사 완료. 구형 공방·선 체력바 스크립트·이전 임시 장면과 빈 폴더 제거 완료.";
    }
}
