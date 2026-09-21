using System;
using System.IO;
using UnityEditor;
using UnityEngine;

using SW.Util;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 유효한 템플릿을 종류별 폴더에 복제합니다. 입력과 저장 실패는 false이며 원본을 변경하지 않습니다.
    /// </summary>
    public static class ProjectDataAssetService
    {
        #region 생성
        /// <summary>
        /// 검사한 템플릿으로 새 자산을 만듭니다. 연결된 자산은 공유하며 원본 파일을 덮어쓰지 않습니다.
        /// </summary>
        public static bool TryCreate(
            ScriptableObject template,
            string fileName,
            out ScriptableObject created,
            out string reason)
        {
            created = null;
            reason = string.Empty;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "전투를 정지한 뒤 데이터를 생성하세요.";
                return false;
            }

            if (template == null || ProjectDataCatalog.GetKind(template) < 0)
            {
                reason = "지원하는 템플릿을 선택하세요.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileName)
                || fileName != fileName.Trim()
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || fileName.EndsWith(".")
                || fileName.Contains("/")
                || fileName.Contains("\\"))
            {
                reason = "경로·확장자를 제외한 올바른 파일 이름을 입력하세요.";
                return false;
            }

            var issues = ProjectDataValidation.Validate(template);
            if (issues.Count > 0)
            {
                reason = "템플릿 검사 실패: " + issues[0].Message;
                return false;
            }

            string folder = ProjectDataCatalog.GetFolder(template.GetType());
            if (!AssetDatabase.IsValidFolder(folder))
            {
                reason = "종류별 데이터 폴더를 찾지 못했습니다: " + folder;
                return false;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + fileName + ".asset");
            ScriptableObject copy = null;
            try
            {
                copy = UnityEngine.Object.Instantiate(template);
                copy.name = Path.GetFileNameWithoutExtension(path);
                copy.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(copy, path);
                Undo.IncrementCurrentGroup();
                Undo.RegisterCreatedObjectUndo(copy, "Project T 데이터 생성");
                AssetDatabase.SaveAssetIfDirty(copy);
                created = copy;
                reason = "데이터를 생성했습니다. 연결된 외형 등은 원본과 공유합니다.";
                return true;
            }
            catch (Exception exception)
            {
                if (copy != null && AssetDatabase.GetAssetPath(copy) == path)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                else if (copy != null)
                {
                    UnityEngine.Object.DestroyImmediate(copy);
                }

                reason = "생성하지 못했습니다. 이름과 파일 접근 상태를 확인하세요.";
                SWLog.LogError("[ProjectDataAssetService] 생성 실패: " + exception.Message);
                return false;
            }
        }

        #endregion // 생성
    }
}
