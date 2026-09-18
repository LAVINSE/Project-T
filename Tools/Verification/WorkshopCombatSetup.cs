using System;
using System.Linq;
using ProjectT.Battle;
using ProjectT.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Unity CLI에서 기존 장면을 보존하며 공방 전투 데이터와 참조를 연결합니다.</summary>
public static class WorkshopCombatSetup
{
    /// <summary>현재 장면과 연결할 공방·버튼의 실제 상태를 확인합니다.</summary>
    public static object Inspect()
    {
        Scene scene = SceneManager.GetActiveScene();
        return new
        {
            scene = scene.path,
            dirty = scene.isDirty,
            workshop = GameObject.Find("MobileArcaneWorkshop")?.transform.position,
            buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Select(button => new {button.name, text = button.GetComponentInChildren<TMPro.TMP_Text>(true)?.text}).ToArray()
        };
    }

    /// <summary>위임받은 첫 스테이지 조정값과 기존 공방 참조만 수정해 저장합니다.</summary>
    public static string Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("편집 모드에서 적용해야 합니다.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/01_Scenes/Stage01_Grassland.unity") throw new InvalidOperationException("스테이지 1 장면이 필요합니다.");
        var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        var workshop = GameObject.Find("MobileArcaneWorkshop");
        if (session == null || workshop == null) throw new InvalidOperationException("기존 전투와 공방을 찾지 못했습니다.");
        var stage = new SerializedObject(session.Definition);
        stage.FindProperty("workshopMaximumHealth").floatValue = 300;
        stage.ApplyModifiedProperties();
        var enemy = new SerializedObject(session.Definition.Enemy);
        enemy.FindProperty("workshopAttackDamage").floatValue = 10;
        enemy.FindProperty("workshopAttackInterval").floatValue = 1.4f;
        enemy.ApplyModifiedProperties();
        // 새 필드의 기본값과 설정값이 같아도 이전 자산 형식을 실제 파일에 갱신합니다.
        EditorUtility.SetDirty(session.Definition);
        EditorUtility.SetDirty(session.Definition.Enemy);
        var sessionSettings = new SerializedObject(session);
        sessionSettings.FindProperty("workshopTarget").objectReferenceValue = workshop.transform;
        sessionSettings.ApplyModifiedProperties();
        var defenseLabel = GameObject.Find("DefenseLabel").GetComponent<TMPro.TMP_Text>();
        Undo.RecordObject(defenseLabel, "공방 체력 안내 변경");
        defenseLabel.text = "공방 체력  300 / 300";
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("장면을 저장하지 못했습니다.");
        AssetDatabase.SaveAssets();
        return "공방 체력 300, 공방 타격 10·간격 1.4초, 기존 공방 참조 연결 및 장면 저장 완료";
    }
}
