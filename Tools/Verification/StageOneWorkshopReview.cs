using System;
using System.Linq;
using System.Text;
using ProjectT.Battle;
using ProjectT.Presentation;
using ProjectT.Units;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

/// <summary>공방 스테이지의 표시 비율과 화면 가독성을 Unity CLI에서 확인합니다.</summary>
public static class StageOneWorkshopReview
{
    /// <summary>세 전투 구간에 검토용 아군을 배치하고 실행 화면을 고정합니다.</summary>
    public static string Prepare()
    {
        var session=UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        if(session==null || session.Allies.Count!=0) throw new InvalidOperationException("새 전투 준비 상태가 필요합니다.");
        Vector2[] destinations={new Vector2(-24,5),new Vector2(5,-3),new Vector2(23.5f,7)};
        for(int index=0;index<destinations.Length;index++)
        {
            if(!session.TryDeploy(session.Definition.Classes[index==2?1:0],destinations[index],out AllyUnit ally,out string reason)) throw new InvalidOperationException(reason);
            ally.Movement.Advance(100f);
        }
        UnityEngine.Object.FindFirstObjectByType<BattleMouseCommand>().SelectUnit(session.Allies[0]);
        session.StartBattle();
        float deadline=Time.time+14;
        EditorApplication.update+=Freeze;
        return "세 전투 구간의 아군과 공방 화면을 14초 뒤 고정합니다.";
        void Freeze()
        {
            if(!Application.isPlaying) {EditorApplication.update-=Freeze;return;}
            if(Time.time<deadline) return;
            Time.timeScale=0;
            EditorApplication.update-=Freeze;
        }
    }
    /// <summary>카메라 비율과 글자 넘침, 장면 참조를 검사해 기록합니다.</summary>
    public static string Inspect()
    {
        var report=new StringBuilder();
        var camera=Camera.main;
        report.AppendLine("WorldView="+(camera.orthographicSize*2*camera.aspect)+" x "+(camera.orthographicSize*2));
        report.AppendLine("Reference="+camera.GetComponent<PixelPerfectCamera>().refResolutionX+"x"+camera.GetComponent<PixelPerfectCamera>().refResolutionY);
        float pixelsPerWorldUnit=1080/(camera.orthographicSize*2);
        var session=UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        foreach(var definition in session.Definition.Classes)
            report.AppendLine("IdleVisibleHeightAt1080 "+definition.name+"="+(definition.Appearance.HealthBarHeight-0.2f)*pixelsPerWorldUnit);
        foreach(var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            if(label.isTextOverflowing) report.AppendLine("OVERFLOW="+label.name+" "+label.text);
        report.AppendLine("WorkshopHealth="+session.Workshop.Health.Current+" Phase="+session.CurrentPhase);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var transforms=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();
        report.AppendLine("MissingScripts="+transforms.Sum(value=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject)));
        report.AppendLine("NonEnglish="+string.Join(",",transforms.Where(value=>value.name.Any(character=>character>127)).Select(value=>value.name)));
        System.IO.File.WriteAllText("Logs/Development/2026-09-16/WorkshopRevision/VisualReview.txt",report.ToString());
        return report.ToString();
    }
}
