using System;
using System.Linq;
using System.Text;
using ProjectT.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬·프리팹 참조와 새 공방 애니메이션을 검증합니다.</summary>
public static class StructureValidation
{
    /// <summary>씬과 프리팹의 누락 스크립트·연결, 공방 그림 범위를 기록합니다.</summary>
    public static string Inspect()
    {
        var report = new StringBuilder();
        string original = SceneManager.GetActiveScene().path;
        foreach (string path in new[] { "Assets/01_Scenes/Main.unity", StageOneSceneBuilder.ScenePath })
        {
            var scene = EditorSceneManager.OpenScene(path);
            report.AppendLine(path);
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0) throw new Exception("Missing script: " + child.name);
                    if (child.parent == null || child.parent.parent == null || child.name.Contains("Tilemap") || child.name.Contains("Arcane"))
                        report.AppendLine(child.name + " parent=" + child.parent?.name + " pos=" + child.position);
                }
            var workshop = GameObject.Find("ArcaneWorkshop");
            if (workshop != null)
            {
                var sprite = workshop.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
                report.AppendLine("Workshop sprite bounds=" + sprite.bounds + " visible=" + SpriteVisibleBounds.Read(sprite.sprite));
            }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/04_Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("MobileArcane")) continue;
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0) throw new Exception("Missing prefab script: " + path);
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/02_Res/Animator/ArcaneWorkshop/ArcaneWorkshop_Animator.controller");
        foreach (var layer in controller.layers)
        {
            report.AppendLine("Default=" + layer.stateMachine.defaultState.name);
            foreach (var transition in layer.stateMachine.anyStateTransitions) WriteTransition("Any", transition);
            foreach (var state in layer.stateMachine.states)
            {
                report.AppendLine("State=" + state.state.name + " motion=" + state.state.motion?.name);
                foreach (var transition in state.state.transitions) WriteTransition(state.state.name, transition);
            }
        }
        EditorSceneManager.OpenScene(original);
        string result = report.ToString();
        System.IO.File.WriteAllText("Logs/Development/2026-09-18/StructureRevision/AfterInspection.txt", result);
        return result;

        void WriteTransition(string source, AnimatorStateTransition transition) => report.AppendLine(source + " -> " + transition.destinationState?.name + " exit=" + transition.isExit + " exitTime=" + transition.hasExitTime + ":" + transition.exitTime + " duration=" + transition.duration + " conditions=" + string.Join(",", transition.conditions.Select(value => value.parameter + ":" + value.mode)));
    }
}
