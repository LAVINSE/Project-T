using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 스테이지와 관련 전투 데이터를 독립 복제해 시험 장면을 만듭니다. 원본 장면과 원본 참조는 저장하지 않습니다.
    /// </summary>
    public static class DataTrialService
    {
        #region 필드
        public const string TrialRoot = "Assets/Temp/ProjectDataTrials";

        #endregion // 필드

        #region 시험 생성
        /// <summary>
        /// 선택 데이터를 연결한 시험 복사본을 만듭니다. 외형 시험은 적용할 클래스나 적이 필요하며 실패하면 생성분만 정리합니다.
        /// </summary>
        public static bool TryCreate(
            StageData baseStage,
            ProjectData selected,
            string templateScenePath,
            out string scenePath,
            out string reason)
        {
            scenePath = string.Empty;
            reason = string.Empty;
            StageData stage = selected as StageData ?? baseStage;
            if (EditorApplication.isPlayingOrWillChangePlaymode || stage == null || !DataCatalog.IsSupported(selected))
            {
                reason = "전투를 정지하고 시험할 데이터와 기준 스테이지를 지정하세요.";
                return false;
            }

            if (selected is RewardData)
            {
                reason = "재화·아이템은 적 보상에 연결한 뒤 적 편집기에서 시험하세요.";
                return false;
            }

            if (string.IsNullOrEmpty(templateScenePath)
                || !templateScenePath.StartsWith("Assets/", StringComparison.Ordinal)
                || AssetDatabase.LoadAssetAtPath<SceneAsset>(templateScenePath) == null)
            {
                reason = "기준 전투 장면을 지정하세요.";
                return false;
            }

            if (DataCatalog.CollectIssues(stage).Count > 0 || DataCatalog.CollectIssues(selected).Count > 0)
            {
                reason = "기준 스테이지와 선택 데이터의 검사 오류를 먼저 수정하세요.";
                return false;
            }

            var copies = new Dictionary<ProjectData, ProjectData>();
            string folder = string.Empty;
            Scene trialScene = default;
            Scene previousActive = SceneManager.GetActiveScene();
            try
            {
                var stageCopy = (StageData)CloneGraph(stage, copies);
                var selectedCopy = CloneGraph(selected, copies);
                Connect(stageCopy, selectedCopy);
                var issues = DataCatalog.CollectIssues(stageCopy);
                if (issues.Count > 0)
                {
                    reason = "시험 연결 검사 실패: " + issues[0].Message;
                    return false;
                }

                if (!EnsureFolder(TrialRoot))
                {
                    reason = "시험 복사본 폴더를 만들지 못했습니다.";
                    return false;
                }

                string identifier = "Trial_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                string folderIdentifier = AssetDatabase.CreateFolder(TrialRoot, identifier);
                if (string.IsNullOrEmpty(folderIdentifier))
                {
                    reason = "시험 작업 폴더를 만들지 못했습니다.";
                    return false;
                }

                folder = AssetDatabase.GUIDToAssetPath(folderIdentifier);
                foreach (var copy in copies.Values)
                {
                    string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + copy.name + ".asset");
                    copy.hideFlags = HideFlags.None;
                    AssetDatabase.CreateAsset(copy, path);
                }

                foreach (var copy in copies.Values)
                {
                    EditorUtility.SetDirty(copy);
                    AssetDatabase.SaveAssetIfDirty(copy);
                }

                string destination = folder + "/DataTrial.unity";
                if (!AssetDatabase.CopyAsset(templateScenePath, destination))
                {
                    reason = "기준 장면을 복제하지 못했습니다.";
                    return false;
                }

                trialScene = EditorSceneManager.OpenScene(destination, OpenSceneMode.Additive);
                var battles = trialScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleManager>(true)).ToArray();
                if (battles.Length != 1)
                {
                    reason = "기준 장면에는 전투 진행자가 정확히 한 개 있어야 합니다.";
                    return false;
                }

                using (var serialized = new SerializedObject(battles[0]))
                {
                    var stageProperty = serialized.FindProperty("stage");
                    if (stageProperty == null)
                    {
                        reason = "전투 진행자의 스테이지 참조를 찾지 못했습니다.";
                        return false;
                    }

                    stageProperty.objectReferenceValue = stageCopy;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(trialScene);
                if (!EditorSceneManager.SaveScene(trialScene))
                {
                    reason = "시험 장면을 저장하지 못했습니다.";
                    return false;
                }

                scenePath = destination;
                reason = "시험 복사본을 만들었습니다. 시험 장면 열기 후 Unity 재생 버튼으로 확인하세요. 복사본은 보관됩니다.";
                return true;
            }
            catch (Exception exception)
            {
                reason = "시험 복사본 생성에 실패했습니다. 원본은 유지됩니다.";
                SWLog.LogError("[DataTrialService] 시험 준비 실패: " + exception.Message);
                return false;
            }
            finally
            {
                if (trialScene.IsValid() && trialScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(trialScene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }

                if (string.IsNullOrEmpty(scenePath) && folder.StartsWith(TrialRoot + "/Trial_", StringComparison.Ordinal))
                {
                    AssetDatabase.DeleteAsset(folder);
                }

                foreach (var copy in copies.Values)
                {
                    if (copy != null && !AssetDatabase.Contains(copy))
                    {
                        UnityEngine.Object.DestroyImmediate(copy);
                    }
                }
            }
        }

        /// <summary>
        /// 지원 데이터의 참조 그래프를 한 번씩 복제합니다. 그림 자산 등은 읽기 전용으로 공유합니다.
        /// </summary>
        private static ProjectData CloneGraph(
            ProjectData source,
            Dictionary<ProjectData, ProjectData> copies)
        {
            if (copies.TryGetValue(source, out ProjectData existing))
            {
                return existing;
            }

            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name;
            copy.hideFlags = HideFlags.HideAndDontSave;
            copies.Add(source, copy);
            using (var serialized = new SerializedObject(copy))
            {
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference
                        && iterator.objectReferenceValue is ProjectData reference
                        && DataCatalog.IsSupported(reference))
                    {
                        iterator.objectReferenceValue = CloneGraph(reference, copies);
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return copy;
        }

        /// <summary>
        /// 선택한 복사본을 시험 스테이지에 연결합니다. 새 클래스는 기존 구매 목록에 추가합니다.
        /// </summary>
        private static void Connect(StageData stage, ProjectData selected)
        {
            using (var serialized = new SerializedObject(stage))
            {
                if (selected is UnitEnemyData)
                {
                    serialized.FindProperty("enemy").objectReferenceValue = selected;
                }
                else if (selected is EnemyRouteData)
                {
                    serialized.FindProperty("enemyRoute").objectReferenceValue = selected;
                }
                else if (selected is UnitClassData)
                {
                    var classes = serialized.FindProperty("classes");
                    bool found = false;
                    for (int index = 0; index < classes.arraySize; index++)
                    {
                        found |= classes.GetArrayElementAtIndex(index).objectReferenceValue == selected;
                    }

                    if (!found)
                    {
                        int index = classes.arraySize;
                        classes.InsertArrayElementAtIndex(index);
                        classes.GetArrayElementAtIndex(index).objectReferenceValue = selected;
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 시험용 프로젝트 폴더만 생성합니다. 올바르지 않은 부모 경로이면 false를 반환합니다.
        /// </summary>
        private static bool EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return true;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)
                || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || !EnsureFolder(parent))
            {
                return false;
            }

            return !string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)));
        }

        #endregion // 시험 생성
    }
}
