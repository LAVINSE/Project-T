using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;

/// <summary>코드 스타일 변경의 컴파일 환경과 로그 설정을 확인합니다.</summary>
public static class CodeStyleVerification
{
    /// <summary>현재 개발 대상의 다른 심볼을 보존하면서 SWUtils 로그를 활성화합니다.</summary>
    public static string EnableDebugLog()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols);
        if (!symbols.Contains("SW_DEBUG_MODE"))
        {
            PlayerSettings.SetScriptingDefineSymbols(target, symbols.Concat(new[] { "SW_DEBUG_MODE" }).ToArray());
            AssetDatabase.SaveAssets();
        }
        return "Target=" + target.TargetName + "; SW_DEBUG_MODE=True";
    }

    /// <summary>현재 장면과 활성 타겟의 SWUtils 로그 사용 여부를 반환합니다.</summary>
    public static string Inspect()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string symbols = PlayerSettings.GetScriptingDefineSymbols(target);
        return "Scene=" + EditorSceneManager.GetActiveScene().path + "; Dirty=" + EditorSceneManager.GetActiveScene().isDirty
            + "; Playing=" + EditorApplication.isPlaying + "; SW_DEBUG_MODE=" + symbols.Split(';').Contains("SW_DEBUG_MODE");
    }
}
