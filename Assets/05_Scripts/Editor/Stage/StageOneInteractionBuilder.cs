using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Util;

using ProjectT.Battle;
using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 기존 전투 장면에 배치 입력과 전장 표시를 추가합니다. 지형·캐릭터·기존 화면은 유지합니다.
    /// </summary>
    public static class StageOneInteractionBuilder
    {
        #region 함수
        /// <summary>
        /// 현재 스테이지의 입력 참조·선택 표시·배치 미리보기·공방 체력바를 연결하고 저장합니다.
        /// </summary>
        public static string Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/01_Scenes/Stage01_Grassland.unity")
            {
                SWLog.LogWarning("[StageOneInteractionBuilder] 작업 중단: " + "스테이지 1 편집 모드에서 실행해야 합니다.");
                return "실패: " + "스테이지 1 편집 모드에서 실행해야 합니다.";
            }

            var session = UnityEngine.Object.FindFirstObjectByType<BattleSession>();
            var commands = UnityEngine.Object.FindFirstObjectByType<BattleMouseCommand>();
            var screen = UnityEngine.Object.FindFirstObjectByType<BattleScreen>();
            if (session == null || commands == null || screen == null)
            {
                SWLog.LogWarning("[StageOneInteractionBuilder] 작업 중단: " + "기존 전투 화면이 필요합니다.");
                return "실패: " + "기존 전투 화면이 필요합니다.";
            }

            var pointer = GetOrAdd<BattlefieldPointer>(commands.gameObject);
            var placement = GetOrAdd<BattlePlacementCommand>(commands.gameObject);
            StageOneGameplayBuilder.Set(pointer, "worldCamera", Camera.main);
            var previewRoot = Child(commands.transform, "DeploymentPreview");
            var preview = GetOrAdd<DeploymentPreview>(previewRoot);
            var character = GetOrAdd<SpriteRenderer>(Child(previewRoot.transform, "PreviewCharacter"));
            character.sortingOrder = 3001;
            character.enabled = false;
            var previewRange = Line(previewRoot.transform, "PreviewAttackRange", 64, 0.07f, 999, true);
            var previewRing = Line(previewRoot.transform, "PreviewPlacementRing", 32, 0.09f, 1000, true);
            StageOneGameplayBuilder.Set(preview, "character", character, "attackRange", previewRange, "placementRing", previewRing);
            StageOneGameplayBuilder.Set(placement, "session", session, "pointer", pointer, "preview", preview);
            StageOneGameplayBuilder.Set(commands, "pointer", pointer, "placement", placement);
            StageOneGameplayBuilder.Set(screen, "placement", placement);
            foreach (string name in new[]
            {
                "PurchaseWarriorButton",
                "PurchaseMageButton"
            })
            {
                GetOrAdd<ClassDeploymentButton>(GameObject.Find(name));
            }

            var selectionRoot = Child(commands.transform, "SelectionIndicators");
            var selection = GetOrAdd<BattleSelectionPresentation>(selectionRoot);
            var range = Line(selectionRoot.transform, "SelectedAttackRange", 64, 0.07f, 999, true);
            var destination = Line(selectionRoot.transform, "MoveDestinationRing", 32, 0.09f, 1000, true);
            var cross = Line(selectionRoot.transform, "MoveDestinationCross", 5, 0.07f, 1001, false);
            destination.startColor = destination.endColor = cross.startColor = cross.endColor = new Color(0.35f, 0.9f, 1f);
            StageOneGameplayBuilder.Set(
                selection,
                "commands",
                commands,
                "placement",
                placement,
                "attackRange",
                range,
                "destinationRing",
                destination,
                "destinationCross",
                cross);
            GameObject marker = GameObject.Find("AllySpawnMarker");
            if (marker != null)
            {
                marker.SetActive(false);
            }

            GameObject.Find("CommandHint").GetComponent<TMPro.TMP_Text>().text = "클래스를 드래그하거나 클릭한 뒤 지면을 클릭해 배치하세요.";
            ArcaneWorkshopBuilder.Apply();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "배치 입력 2종, 미리보기, 선택 공격 범위, 목적지 표시, 공방 체력바 연결 완료";
        }

        /// <summary>
        /// 부모 아래에 이름으로 구분되는 자식을 준비합니다.
        /// </summary>
        private static GameObject Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created;
        }

        /// <summary>
        /// 필요한 컴포넌트를 재사용하거나 추가합니다.
        /// </summary>
        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        /// <summary>
        /// 선택·배치 표시용 선 렌더러를 생성하고 설정합니다.
        /// </summary>
        private static LineRenderer Line(Transform parent, string name, int count, float width, int order, bool loop)
        {
            var line = GetOrAdd<LineRenderer>(Child(parent, name));
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/02_Res/Materials/BattleLine.mat");
            line.useWorldSpace = true;
            line.positionCount = count;
            line.loop = loop;
            line.widthMultiplier = width;
            line.sortingOrder = order;
            line.startColor = line.endColor = Color.white;
            line.enabled = false;
            return line;
        }

        #endregion // 함수
    }
}
