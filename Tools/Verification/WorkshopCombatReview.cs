using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectT.Battle;
using ProjectT.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Unity CLI에서 실제 전투 화면과 휴식 버튼을 확인하고 증거를 기록합니다.</summary>
public static class WorkshopCombatReview
{
    private const string Output = "Logs/Development/2026-09-16/WorkshopCombat/";

    /// <summary>실제 공격 수치로 전사 둘과 마법사 하나의 자연 교전을 시작합니다. 배치 이동과 관찰 시간만 단축합니다.</summary>
    public static string StartNaturalBattle()
    {
        BattleSession session = RequireSession();
        if (session.CurrentPhase != BattleSession.Phase.Preparation || session.Allies.Count != 0)
            throw new InvalidOperationException("새 전투 준비 상태가 필요합니다.");
        Vector2[] positions = {new Vector2(-24, 5), new Vector2(-24, 3), new Vector2(-21, 5)};
        for (int index = 0; index < positions.Length; index++)
        {
            if (!session.TryDeploy(session.Definition.Classes[index == 2 ? 1 : 0], positions[index], out AllyUnit ally, out string reason))
                throw new InvalidOperationException(reason);
            ally.Movement.Advance(100);
        }
        Time.timeScale = 12;
        GameObject.Find("StartBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "자연 교전 시작: 적 체력·공격력 원본 유지, 배치 이동과 시간 배율만 단축";
    }

    /// <summary>실제 화면의 다음 라운드 또는 결과 보기 버튼을 한 번 누릅니다.</summary>
    public static string Continue()
    {
        BattleSession session = RequireSession();
        GameObject.Find("StartBattleButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "Phase=" + session.CurrentPhase + " Round=" + session.RoundNumber;
    }

    /// <summary>화면 상태·가독성·누락 참조·입력 구성을 현재 상태 그대로 기록합니다.</summary>
    public static string Inspect(string name)
    {
        BattleSession session = RequireSession();
        Canvas.ForceUpdateCanvases();
        var report = new StringBuilder();
        report.AppendLine("Phase=" + session.CurrentPhase + " Round=" + session.RoundNumber + " Killed=" + session.KilledCount);
        report.AppendLine("WorkshopHealth=" + session.Workshop.Health.Current + "/" + session.Workshop.Health.Maximum + " Enemies=" + session.Enemies.Count);
        report.AppendLine("Paused=" + session.IsPaused + " CanCommand=" + session.CanCommand + " TimeScale=" + Time.timeScale);
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
        {
            label.ForceMeshUpdate();
            if (label.isTextOverflowing) report.AppendLine("OVERFLOW=" + label.name + " " + label.text);
        }
        foreach (string labelName in new[] {"RoundLabel", "DefenseLabel", "StartBattleButton"})
        {
            var label = GameObject.Find(labelName).GetComponentInChildren<TMPro.TMP_Text>();
            report.AppendLine(labelName + "=" + label.text);
        }
        var transforms = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true));
        report.AppendLine("MissingScripts=" + transforms.Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)));
        report.AppendLine("EventSystems=" + UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length);
        report.AppendLine("Raycasters=" + UnityEngine.Object.FindObjectsByType<UnityEngine.UI.GraphicRaycaster>(FindObjectsSortMode.None).Length);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + name + ".txt", report.ToString());
        return report.ToString();
    }

    /// <summary>새 전투의 적 한 명을 공방까지 진행시켜 실제 타격과 체력 표시를 관찰합니다.</summary>
    public static async Task<string> StartWorkshopAssault()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("실행 중에 검증해야 합니다.");
        Time.timeScale = 1;
        AsyncOperation load = SceneManager.LoadSceneAsync("Stage01_Grassland");
        while (!load.isDone) await Task.Delay(50);
        await Task.Delay(150);
        BattleSession session = RequireSession();
        Time.timeScale = 1;
        session.StartBattle();
        while (session.Enemies.Count == 0) await Task.Delay(50);
        session.Enemies[0].Movement.Advance(1000);
        await Task.Delay(1800);
        return Inspect("WorkshopAssault");
    }

    /// <summary>공방 앞 실제 지형을 걸어서 접근한 전사가 공격 중인 적을 저지하고 처치하는지 확인합니다.</summary>
    public static async Task<string> InterceptWorkshopAssault()
    {
        BattleSession session = RequireSession();
        EnemyUnit enemy = session.Enemies.First(value => value.IsActive && value.HasReachedWorkshop);
        if (!session.TryDeploy(session.Definition.Classes[0], new Vector2(29, 8.2f), out AllyUnit warrior, out string reason))
            throw new InvalidOperationException(reason);
        float deadline = Time.realtimeSinceStartup + 15;
        while (enemy.IsActive && enemy.Blocker != warrior && Time.realtimeSinceStartup < deadline) await Task.Delay(50);
        if (enemy.Blocker != warrior) throw new InvalidOperationException("공방 앞 전사가 실제 저지를 시작하지 못했습니다.");
        float protectedHealth = session.Workshop.Health.Current;
        while (enemy.IsActive && Time.realtimeSinceStartup < deadline) await Task.Delay(50);
        if (enemy.IsActive || session.Workshop.Health.Current != protectedHealth)
            throw new InvalidOperationException("저지 이후 공방 보호 또는 적 처치 검증에 실패했습니다.");
        return Inspect("WorkshopInterception");
    }

    private static BattleSession RequireSession()
    {
        var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        if (!Application.isPlaying || session == null || session.Workshop == null)
            throw new InvalidOperationException("스테이지 1의 실행 중인 전투가 필요합니다.");
        return session;
    }
}
