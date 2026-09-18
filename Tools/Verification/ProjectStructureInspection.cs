using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>정리 전 사용자 프리팹과 장면 구조를 변경 없이 기록합니다.</summary>
public static class ProjectStructureInspection
{
    /// <summary>Main 기준 장면과 전투 장면, 새 프리팹의 실제 구성을 반환합니다.</summary>
    public static string Inspect()
    {
        var report = new StringBuilder();
        Scene original = SceneManager.GetActiveScene();
        report.AppendLine("Active=" + original.path + " Dirty=" + original.isDirty + " Playing=" + EditorApplication.isPlaying);
        foreach (string path in new[] { "Assets/01_Scenes/Main.unity", "Assets/01_Scenes/Stage01_Grassland.unity" })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool temporary = !scene.IsValid() || !scene.isLoaded;
            if (temporary) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            report.AppendLine("SCENE " + path + " Dirty=" + scene.isDirty);
            foreach (GameObject root in scene.GetRootGameObjects()) Describe(root.transform, 0, path.Contains("Main") ? 5 : 2, report);
            if (temporary) EditorSceneManager.CloseScene(scene, true);
        }
        SceneManager.SetActiveScene(original);
        foreach (string path in new[] { "Assets/04_Prefabs/ArcaneWorkshop.prefab", "Assets/04_Prefabs/ArcaneHealthBar.prefab",
            "Assets/04_Prefabs/CommonHealthBar.prefab", "Assets/04_Prefabs/SWPool Variant.prefab", "Assets/04_Prefabs/SWPoolRegistry Variant.prefab" })
        {
            report.AppendLine("PREFAB " + path);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Describe(prefab.transform, 0, 8, report);
            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component is Animator animator)
                {
                    var controller = animator.runtimeAnimatorController as AnimatorController;
                    report.AppendLine("Animator=" + AssetDatabase.GetAssetPath(controller));
                    if (controller != null)
                    {
                        report.AppendLine("Parameters=" + string.Join(", ", controller.parameters.Select(value => value.name + ":" + value.type)));
                        foreach (AnimatorControllerLayer layer in controller.layers)
                            foreach (ChildAnimatorState state in layer.stateMachine.states)
                                report.AppendLine("State=" + state.state.name + " Motion=" + state.state.motion?.name + " Transitions="
                                    + string.Join("; ", state.state.transitions.Select(transition => transition.destinationState?.name + " "
                                        + string.Join(",", transition.conditions.Select(condition => condition.parameter + " " + condition.mode + " " + condition.threshold)))));
                    }
                }
                if (component is UnityEngine.UI.Image image) report.AppendLine("Image=" + image.name + " Type=" + image.type + " Size=" + image.rectTransform.sizeDelta + " Scale=" + image.transform.lossyScale + " Raycast=" + image.raycastTarget);
                if (component is MonoBehaviour || component is Canvas)
                {
                    if (component == null) { report.AppendLine("MISSING SCRIPT"); continue; }
                    report.AppendLine(EditorJsonUtility.ToJson(component));
                }
            }
        }
        report.AppendLine("BUILD=" + string.Join(",", EditorBuildSettings.scenes.Select(value => value.path + " enabled=" + value.enabled)));
        Directory.CreateDirectory("Logs/Development/2026-09-18/StructureRevision");
        File.WriteAllText("Logs/Development/2026-09-18/StructureRevision/BeforeInspection.txt", report.ToString());
        return report.ToString();
    }

    private static void Describe(Transform current, int depth, int maximum, StringBuilder report)
    {
        if (depth > maximum) return;
        report.AppendLine(new string(' ', depth * 2) + current.name + " position=" + current.localPosition + " scale=" + current.localScale
            + " [" + string.Join(",", current.GetComponents<Component>().Select(value => value != null ? value.GetType().Name : "MISSING")) + "]");
        for (int index = 0; index < current.childCount; index++) Describe(current.GetChild(index), depth + 1, maximum, report);
    }
}
