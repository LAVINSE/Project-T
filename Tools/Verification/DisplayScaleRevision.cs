using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Editor;
using ProjectT.Navigation;
using ProjectT.Presentation;

/// <summary>
/// 기준 화면 확대를 편집기에 적용하고 실제 화면과 참조 상태를 검증합니다.
/// </summary>
public static class DisplayScaleRevision
{
    private const string Output = "Logs/Development/2026-09-18/DisplayScaleRevision/";

    /// <summary>
    /// 편집 중인 장면을 원본 저장 없이 별도 보관합니다.
    /// </summary>
    public static string BackupUnsavedScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != StageOneSceneBuilder.ScenePath)
        {
            return "실패: 스테이지 편집 상태가 필요합니다.";
        }
        string path = "Assets/UnsavedStageDisplayBackup.unity";
        if (!EditorSceneManager.SaveScene(scene, path, true))
        {
            return "실패: 장면 사본을 저장하지 못했습니다.";
        }
        File.Copy(path, Output + "UnsavedStageDisplayBackup.unity", true);
        AssetDatabase.DeleteAsset(path);
        return "저장되지 않은 장면 사본 보관 완료";
    }

    /// <summary>
    /// 사본 비교가 끝난 미리보기의 화면 배치를 갱신하고 현재 장면을 저장합니다.
    /// </summary>
    public static string SaveComparedScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != StageOneSceneBuilder.ScenePath)
        {
            return "실패: 스테이지 편집 상태가 필요합니다.";
        }
        Canvas.ForceUpdateCanvases();
        return "Saved=" + EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// 저장된 스테이지의 경로·지형·공방과 카메라를 같은 표시 범위로 갱신합니다.
    /// </summary>
    public static string Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.isDirty || scene.path != StageOneSceneBuilder.ScenePath)
        {
            return "실패: 저장된 스테이지 1의 편집 상태가 필요합니다.";
        }
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
        {
            return "실패: Universal Render Pipeline 설정을 확인해 주세요.";
        }

        var definition = AssetDatabase.LoadAssetAtPath<EnemyRouteDefinition>(ProjectAssetPaths.EnemyRoute);
        if (definition == null || !definition.TryCreateRoute(out FixedRoute previousRoute, out string reason))
        {
            return "실패: 기존 경로를 확인하지 못했습니다.";
        }

        StageOneBattlefieldLayout.Apply();
        string terrainResult = StageOneTerrainBuilder.Improve();
        if (terrainResult.StartsWith("실패:"))
        {
            return terrainResult;
        }
        ArcaneWorkshopBuilder.Apply();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        definition.TryCreateRoute(out FixedRoute currentRoute, out _);
        string result = "Output=1920x1080; Reference=1536x864; WorldView=48x27; PixelsPerUnit=32; ScaleIncreaseFromOriginal=50%; RouteLength="
            + previousRoute.Length + " -> " + currentRoute.Length + "; " + terrainResult;
        File.WriteAllText(Output + "Applied.txt", result);
        return result;
    }

    /// <summary>
    /// 확대된 화면에서 체력바·사거리·목적지를 비교할 상태를 준비합니다.
    /// </summary>
    public static string Prepare()
    {
        var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        if (!EditorApplication.isPlaying || session == null || session.Wallet == null || session.Allies.Count != 0)
        {
            return "실패: 새 전투의 준비 상태가 필요합니다.";
        }

        if (!session.TryDeploy(session.Definition.Classes[0], new Vector2(14, 3), out var warrior, out string warriorReason))
        {
            return "실패: " + warriorReason;
        }
        if (!session.TryDeploy(session.Definition.Classes[1], new Vector2(10, -2), out var mage, out string mageReason))
        {
            return "실패: " + mageReason;
        }
        warrior.Health.TakeDamage(65);
        mage.Health.TakeDamage(35);
        session.Workshop.Health.TakeDamage(100);
        var commands = UnityEngine.Object.FindFirstObjectByType<BattleMouseCommand>();
        commands.SelectUnit(mage);
        commands.ExecuteWorldClick(new Vector2(17, 2), true);
        Time.timeScale = 0;
        return Inspect();
    }

    /// <summary>
    /// 1080 화면의 예상 픽셀 크기와 경로·공방의 화면 포함 여부를 검사합니다.
    /// </summary>
    public static string Inspect()
    {
        var report = new StringBuilder();
        Camera camera = Camera.main;
        var pixelCamera = camera.GetComponent<PixelPerfectCamera>();
        var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
        report.AppendLine("Pipeline=" + GraphicsSettings.currentRenderPipeline.GetType().Name);
        report.AppendLine("Reference=" + pixelCamera.refResolutionX + "x" + pixelCamera.refResolutionY
            + "; PixelsPerUnit=" + pixelCamera.assetsPPU + "; Grid=" + pixelCamera.gridSnapping
            + "; Crop=" + pixelCamera.cropFrame + "; Filter="
            + (PixelPerfectCamera.PixelPerfectFilterMode)new SerializedObject(pixelCamera).FindProperty("m_FilterMode").intValue);
        report.AppendLine("OrthographicSize=" + camera.orthographicSize + "; OutputPixelsPerWorldUnit=" + 1080f / (2 * camera.orthographicSize));
        report.AppendLine("CameraHDR=" + camera.allowHDR + "; CameraMSAA=" + camera.allowMSAA
            + "; DynamicResolution=" + camera.allowDynamicResolution + "; PipelineRenderScale=" + ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).renderScale);
        foreach (AllyClassDefinition definition in session.Definition.Classes)
        {
            Sprite sprite = definition.Appearance.GetFrames(true, false, false)[0];
            float height = SpriteVisibleBounds.Read(sprite).size.y * sprite.pixelsPerUnit / 32f;
            report.AppendLine(definition.name + ".VisibleHeightAt1080=" + height * 1080f / (2 * camera.orthographicSize)
                + "; SourceFilter=" + sprite.texture.filterMode + "; TextureMipmaps=" + sprite.texture.mipmapCount);
        }
        session.Definition.EnemyRoute.TryCreateRoute(out FixedRoute route, out _);
        Rect visible = new Rect(-StageOneBattlefieldLayout.ViewWidth / 2, -StageOneBattlefieldLayout.ViewHeight / 2,
            StageOneBattlefieldLayout.ViewWidth, StageOneBattlefieldLayout.ViewHeight);
        bool routeInside = Enumerable.Range(0, route.PointCount).All(index => visible.Contains(route.GetPoint(index)));
        report.AppendLine("RouteInsideFrame=" + routeInside);
        report.AppendLine("RouteLength=" + route.Length);
        var workshop = UnityEngine.Object.FindFirstObjectByType<WorkshopObjectivePresentation>();
        var renderer = workshop.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
        Bounds localBounds = SpriteVisibleBounds.Read(renderer.sprite);
        Vector3 minimum = renderer.transform.TransformPoint(localBounds.min);
        Vector3 maximum = renderer.transform.TransformPoint(localBounds.max);
        report.AppendLine("WorkshopVisibleMinimum=" + minimum + "; Maximum=" + maximum
            + "; WorkshopInsideFrame=" + (visible.Contains(minimum) && visible.Contains(maximum)));
        report.AppendLine("WorkshopBelowHeader=" + (maximum.y < visible.yMax - 64f * visible.height / 1080f));
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
        {
            label.ForceMeshUpdate();
            if (label.isTextOverflowing)
            {
                report.AppendLine("TextOverflow=" + label.name);
            }
        }
        File.WriteAllText(Output + "RuntimeInspection.txt", report.ToString());
        return report.ToString();
    }

    /// <summary>
    /// 캡처를 개발 로그로 옮기고 Unity에 가져온 임시 이미지 자산을 제거합니다.
    /// </summary>
    public static string ArchiveCaptures()
    {
        foreach (string name in new[] { "Before", "After", "After720" })
        {
            string source = "Assets/" + Output + name + ".png";
            if (File.Exists(source))
            {
                File.Copy(source, Output + name + ".png", true);
                AssetDatabase.DeleteAsset(source);
            }
        }
        return "검증 이미지를 " + Output + "에 보관했습니다.";
    }
}
