using System;
using System.Linq;
using ProjectT.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Unity CLI에서 현재 장면을 보관하고 배치 조작 변경을 적용합니다.</summary>
public static class DeploymentInteractionSetup
{
    /// <summary>저장 전 장면 복사본을 남기고 기존 객체를 보존하며 변경을 적용합니다.</summary>
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드가 필요합니다.");
        string backup = "Assets/Temp/Stage01_BeforeDeploymentInteraction.unity";
        if (!System.IO.File.Exists(backup)) EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), backup, true);
        return StageOneInteractionBuilder.Apply();
    }

    /// <summary>추가한 객체와 컴포넌트의 연결을 확인합니다.</summary>
    public static string Inspect()
    {
        string[] names = { "WorkshopHealthBar", "PreviewAttackRange", "SelectedAttackRange", "MoveDestinationRing",
            "MoveDestinationCross", "PurchaseWarriorButton", "PurchaseMageButton" };
        return string.Join("\n", names.Select(name =>
        {
            GameObject instance = GameObject.Find(name);
            return name + ": " + (instance == null ? "MISSING" : string.Join(", ", instance.GetComponents<Component>().Select(component => component.GetType().Name)));
        }));
    }
}
