using System;
using System.IO;
using System.Linq;
using System.Text;
using ProjectT.Battle;
using ProjectT.Presentation;
using ProjectT.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>새 조작의 실제 전투 화면과 참조 상태를 Unity CLI에서 검증합니다.</summary>
public static class DeploymentInteractionReview
{
    private const string Output = "Logs/Development/2026-09-16/DeploymentInteraction/";

    /// <summary>체력이 줄어든 공방과 선택한 마법사의 사거리·이동 목적지를 관찰할 상태를 준비합니다.</summary>
    public static string ShowSelection()
    {
        BattleSession session = RequireSession();
        if (session.Allies.Count != 0) throw new InvalidOperationException("새 전투에서 실행해야 합니다.");
        session.TryDeploy(session.Definition.Classes[0], new Vector2(-24, 5), out _, out _);
        session.TryDeploy(session.Definition.Classes[1], new Vector2(-19, 4), out AllyUnit mage, out string reason);
        if (mage == null) throw new InvalidOperationException(reason);
        UnityEngine.Object.FindFirstObjectByType<BattleMouseCommand>().SelectUnit(mage);
        session.TryMove(mage, new Vector2(-8, -4));
        session.Workshop.Health.TakeDamage(90);
        Time.timeScale = 0;
        return "관찰: 공방 체력 210/300, 선택한 마법사 사거리 4.5, 목적지 (-8,-4)";
    }

        /// <summary>화면 확인용으로 전사 배치 미리보기를 고정합니다. 실제 입력 검증은 플레이 모드 테스트에서 수행합니다.</summary>
    public static string ShowPlacement()
    {
        BattleSession session = RequireSession();
        var placement = UnityEngine.Object.FindFirstObjectByType<BattlePlacementCommand>();
        placement.enabled = false;
        placement.BeginPlacement(session.Definition.Classes[0]);
        UnityEngine.Object.FindFirstObjectByType<DeploymentPreview>().Show(session.Definition.Classes[0], new Vector2(8, 5), true);
        return "관찰: 위치 (8,5)의 전사 배치 미리보기, 결제 전 재화 30";
    }

    /// <summary>표시 범위·체력·문자 넘침·누락 컴포넌트를 보고서로 저장합니다.</summary>
    public static string Inspect()
    {
        BattleSession session = RequireSession();
        Canvas.ForceUpdateCanvases();
        var report = new StringBuilder();
        report.AppendLine("Phase=" + session.CurrentPhase + " Allies=" + session.Allies.Count + " Balance=" + session.Wallet.Balance);
        report.AppendLine("WorkshopHealth=" + session.Workshop.Health.Current + "/" + session.Workshop.Health.Maximum);
        foreach (string name in new[] { "WorkshopHealthFill", "SelectedAttackRange", "MoveDestinationRing", "PreviewAttackRange" })
        {
            var line = GameObject.Find(name).GetComponent<LineRenderer>();
            report.AppendLine(name + " Enabled=" + line.enabled + " First=" + line.GetPosition(0) + " Last=" + line.GetPosition(line.positionCount - 1));
        }
        foreach (TMPro.TMP_Text label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
        {
            label.ForceMeshUpdate();
            if (label.isTextOverflowing) report.AppendLine("OVERFLOW=" + label.name + " " + label.text);
        }
        report.AppendLine("MissingScripts=" + SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)));
        Directory.CreateDirectory(Output);
        string nameSuffix = UnityEngine.Object.FindFirstObjectByType<BattlePlacementCommand>().IsPlacing ? "Placement" : "Selection";
        File.WriteAllText(Output + nameSuffix + ".txt", report.ToString());
        return report.ToString();
    }

    private static BattleSession RequireSession()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("실행 중인 전투가 필요합니다.");
        return UnityEngine.Object.FindFirstObjectByType<BattleSession>() ?? throw new InvalidOperationException("전투가 없습니다.");
    }
}
