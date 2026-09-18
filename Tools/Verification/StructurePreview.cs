using System;
using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Presentation;
using SW.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>최종 화면 검증용 상태를 실행 중에만 구성합니다.</summary>
public static class StructurePreview
{
    /// <summary>저장된 Main에서 실행을 시작합니다.</summary>
    public static string Start()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드가 필요합니다.");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("미저장 장면이 있습니다.");
        EditorSceneManager.OpenScene("Assets/01_Scenes/Main.unity");
        EditorApplication.isPlaying = true;
        return "Main Play 시작";
    }

    /// <summary>선택한 캐릭터의 범위와 이동 목적지, 아군·공방 체력바를 표시합니다.</summary>
    public static string Show()
    {
        var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        if (session == null || session.Wallet == null) throw new InvalidOperationException("스테이지 초기화 대기 중입니다.");
        session.TryDeploy(session.Definition.Classes[0], new Vector2(22, 7), out var warrior, out _);
        session.TryDeploy(session.Definition.Classes[1], new Vector2(17, 3), out var mage, out _);
        warrior.Health.TakeDamage(65);
        mage.Health.TakeDamage(35);
        session.Workshop.Health.TakeDamage(100);
        var commands = UnityEngine.Object.FindFirstObjectByType<BattleMouseCommand>();
        commands.SelectUnit(mage);
        commands.ExecuteWorldClick(new Vector2(23, 4), true);
        Time.timeScale = 0;
        return "Scene=" + SceneManager.GetActiveScene().name + " DataManager=" + DataManager.Instance.name
            + " PoolCount=" + UnityEngine.Object.FindObjectsByType<SWPool>(FindObjectsSortMode.None).Length;
    }

    /// <summary>실행을 종료하고 저장된 Main 편집 상태로 돌아갑니다.</summary>
    public static string Stop()
    {
        Time.timeScale = 1;
        EditorApplication.isPlaying = false;
        return "Play 종료 요청";
    }

    /// <summary>검증 이미지를 로그에 보관하고 프로젝트 안의 임시 캡처를 정리합니다.</summary>
    public static string ArchivePreview()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드가 필요합니다.");
        const string source = "Assets/Logs/StructureRevisionPreview.png";
        const string destination = "Logs/Development/2026-09-18/StructureRevision/FinalPreview.png";
        System.IO.File.Copy(source, destination, true);
        if (!AssetDatabase.DeleteAsset(source) && System.IO.File.Exists(source)) throw new InvalidOperationException("임시 캡처 제거 실패");
        if (System.IO.Directory.Exists("Assets/Logs") && !System.IO.Directory.EnumerateFileSystemEntries("Assets/Logs").GetEnumerator().MoveNext()) AssetDatabase.DeleteAsset("Assets/Logs");
        EditorSceneManager.OpenScene("Assets/01_Scenes/Main.unity");
        AssetDatabase.Refresh();
        return "Main 편집 상태 복귀, 이미지=" + destination;
    }
}
