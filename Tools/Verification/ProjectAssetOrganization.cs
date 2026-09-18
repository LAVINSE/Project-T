using System;
using System.IO;
using System.Text;
using UnityEditor;

/// <summary>Unity 자산 식별자를 보존하면서 종류별 폴더로 이동합니다.</summary>
public static class ProjectAssetOrganization
{
    /// <summary>공통 데이터·전투 데이터·재질·프리팹·스크립트를 역할에 따라 정리합니다.</summary>
    public static string MoveAssets()
    {
        var report = new StringBuilder();
        foreach (string path in new[] { "Assets/02_Res/Materials", "Assets/02_Res/Data/Stage", "Assets/02_Res/Data/Navigation",
            "Assets/02_Res/Data/Units", "Assets/02_Res/Data/Appearance", "Assets/02_Res/Data/Pooling",
            "Assets/04_Prefabs/Units", "Assets/04_Prefabs/Effects", "Assets/04_Prefabs/Workshop", "Assets/04_Prefabs/Health",
            "Assets/04_Prefabs/Pooling", "Assets/04_Prefabs/Popup", "Assets/04_Prefabs/Data", "Assets/04_Prefabs/Initialization",
            "Assets/05_Scripts/Runtime/Data/Units", "Assets/05_Scripts/Runtime/Data/Appearance", "Assets/05_Scripts/Runtime/Data/Stage",
            "Assets/05_Scripts/Runtime/Data/Navigation", "Assets/05_Scripts/Runtime/Data/Common", "Assets/05_Scripts/Editor/Stage" }) Folder(path);
        AssetDatabase.StartAssetEditing();
        try
        {
            Move("Assets/02_Res/Defense/Stage01/BattleLine.mat", "Assets/02_Res/Materials/BattleLine.mat");
            Move("Assets/02_Res/Defense/Stage01/Stage01.asset", "Assets/02_Res/Data/Stage/Stage01.asset");
            Move("Assets/02_Res/Defense/Stage01/EnemyRoute.asset", "Assets/02_Res/Data/Navigation/Stage01EnemyRoute.asset");
            foreach (string name in new[] { "Warrior", "Mage", "Skeleton" })
            {
                Move("Assets/02_Res/Defense/Stage01/" + name + ".asset", "Assets/02_Res/Data/Units/" + name + ".asset");
                Move("Assets/02_Res/Defense/Stage01/" + name + "Appearance.asset", "Assets/02_Res/Data/Appearance/" + name + "Appearance.asset");
            }
            foreach (string name in new[] { "AllyUnit", "EnemyUnit" })
                Move("Assets/04_Prefabs/Defense/" + name + ".prefab", "Assets/04_Prefabs/Units/" + name + ".prefab");
            Move("Assets/04_Prefabs/Defense/AttackTrace.prefab", "Assets/04_Prefabs/Effects/AttackTrace.prefab");
            Move("Assets/04_Prefabs/ArcaneWorkshop.prefab", "Assets/04_Prefabs/Workshop/ArcaneWorkshop.prefab");
            foreach (string name in new[] { "ArcaneHealthBar", "CommonHealthBar" })
                Move("Assets/04_Prefabs/" + name + ".prefab", "Assets/04_Prefabs/Health/" + name + ".prefab");
            Move("Assets/04_Prefabs/SWPool Variant.prefab", "Assets/04_Prefabs/Pooling/SWPool.prefab");
            Move("Assets/04_Prefabs/SWPoolRegistry Variant.prefab", "Assets/04_Prefabs/Pooling/SWPoolRegistry.prefab");
            Move("Assets/04_Prefabs/SWPopupManager Variant.prefab", "Assets/04_Prefabs/Popup/SWPopupManager.prefab");
            foreach (string name in new[] { "AllyClassDefinition", "EnemyDefinition" })
                Move("Assets/05_Scripts/Runtime/Units/" + name + ".cs", "Assets/05_Scripts/Runtime/Data/Units/" + name + ".cs");
            Move("Assets/05_Scripts/Runtime/Units/UnitAppearance.cs", "Assets/05_Scripts/Runtime/Data/Appearance/UnitAppearance.cs");
            Move("Assets/05_Scripts/Runtime/Battle/StageDefinition.cs", "Assets/05_Scripts/Runtime/Data/Stage/StageDefinition.cs");
            Move("Assets/05_Scripts/Runtime/Navigation/EnemyRouteDefinition.cs", "Assets/05_Scripts/Runtime/Data/Navigation/EnemyRouteDefinition.cs");
            Move("Assets/05_Scripts/Runtime/Data/ColorData.cs", "Assets/05_Scripts/Runtime/Data/Common/ColorData.cs");
            foreach (string path in Directory.GetFiles("Assets/05_Scripts/Editor", "*.cs"))
            {
                string name = Path.GetFileName(path);
                Move(path.Replace('\\', '/'), "Assets/05_Scripts/Editor/Stage/" + (name == "MobileWorkshopBuilder.cs" ? "ArcaneWorkshopBuilder.cs" : name));
            }
            foreach (string path in Directory.GetFiles("Assets/05_Scripts", "*.asmdef", SearchOption.AllDirectories))
                Move(path.Replace('\\', '/'), path.Replace('\\', '/').Replace("ProjectT.Defense.", "ProjectT."));
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Development/2026-09-18/StructureRevision/AssetMoves.txt", report.ToString());
        return report.ToString();

        void Move(string source, string destination)
        {
            if (source == destination || !File.Exists(source)) return;
            Folder(Path.GetDirectoryName(destination).Replace('\\', '/'));
            string metadata = File.ReadAllText(source + ".meta");
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (File.ReadAllText(destination + ".meta") != metadata) throw new InvalidOperationException("자산 메타 정보가 변경되었습니다: " + destination);
            report.AppendLine(source + " -> " + destination);
        }
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
