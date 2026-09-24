using System;
using UnityEditor;
using UnityEngine;

using SW.Util;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 편집 중 값을 원본과 분리하고 데이터 유효성 검사 없이 저장합니다. 생성 실패는 null, 저장 실패는 false입니다.
    /// </summary>
    public sealed class DataEditSession : IDisposable
    {
        #region 필드
        private readonly ScriptableObject source;
        private ScriptableObject draft;
        private SerializedObject serialized;
        private string baseline;
        private string sourceSnapshot;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 저장된 원본 데이터입니다.
        /// </summary>
        public ScriptableObject Source => source;

        /// <summary>
        /// 수정 중인 메모리 복사본입니다. 원본 참조는 적용 전까지 바뀌지 않습니다.
        /// </summary>
        public ScriptableObject Draft => draft;

        /// <summary>
        /// 입력 필드를 연결할 복사본의 직렬화 객체입니다.
        /// </summary>
        public SerializedObject Serialized => serialized;

        /// <summary>
        /// 마지막 적용 이후 수정 사항이 있는지 반환합니다.
        /// </summary>
        public bool HasChanges => draft != null && EditorJsonUtility.ToJson(draft) != baseline;

        /// <summary>
        /// 다른 창이나 실행 취소에서 원본이 변경되었는지 반환합니다.
        /// </summary>
        public bool HasExternalChanges => source == null || EditorJsonUtility.ToJson(source) != sourceSnapshot;

        /// <summary>
        /// 창 재열기에서 충돌 검사를 유지할 원본 기준값입니다.
        /// </summary>
        public string SourceSnapshot => sourceSnapshot;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검증된 원본으로 편집 복사본을 준비합니다.
        /// </summary>
        private DataEditSession(ScriptableObject asset)
        {
            source = asset;
            Reload();
        }

        /// <summary>
        /// 편집 가능한 독립 데이터에만 세션을 만듭니다. 잘못된 대상이면 경고 후 null입니다.
        /// </summary>
        public static DataEditSession Create(ScriptableObject asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!DataCatalog.IsSupported(asset)
                || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || !AssetDatabase.IsMainAsset(asset))
            {
                SWLog.LogWarning("[DataEditSession] 준비 실패: 저장된 프로젝트 데이터가 필요합니다.");
                return null;
            }

            return new DataEditSession(asset);
        }

        /// <summary>
        /// 원본에서 다시 읽습니다. 미적용 변경은 취소되며 기존 직렬화 연결은 다시 만들어야 합니다.
        /// </summary>
        public void Reload()
        {
            Dispose();
            if (source == null)
            {
                return;
            }

            draft = UnityEngine.Object.Instantiate(source);
            draft.name = source.name;
            draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            serialized = new SerializedObject(draft);
            baseline = EditorJsonUtility.ToJson(draft);
            sourceSnapshot = EditorJsonUtility.ToJson(source);
        }

        /// <summary>
        /// 동일 편집기 세션에서 보관한 초안을 복원합니다. 원본에는 적용하지 않습니다.
        /// </summary>
        public void RestoreDraft(string contents, string originalSnapshot)
        {
            if (draft == null || string.IsNullOrEmpty(contents))
            {
                return;
            }

            EditorJsonUtility.FromJsonOverwrite(contents, draft);
            draft.name = source.name;
            draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            if (!string.IsNullOrEmpty(originalSnapshot))
            {
                sourceSnapshot = originalSnapshot;
            }

            serialized.Update();
        }

        #endregion // 초기화

        #region 적용
        /// <summary>
        /// 편집 가능 여부와 외부 변경을 확인한 뒤 저장합니다. 빈 값과 미완성 데이터도 허용하며 저장 불가 시 false입니다.
        /// </summary>
        public bool TrySave(out string reason)
        {
            reason = string.Empty;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "전투를 정지한 뒤 데이터를 적용하세요.";
                return false;
            }

            if (source == null || draft == null || HasExternalChanges)
            {
                reason = "원본이 외부에서 변경되었습니다. 원본 다시 읽기로 상태를 확인하세요.";
                return false;
            }

            if (!AssetDatabase.IsOpenForEdit(source))
            {
                reason = "원본 파일을 수정할 수 없습니다. 파일의 읽기 전용 또는 잠금 상태를 확인하세요.";
                return false;
            }

            if (!HasChanges)
            {
                reason = "적용할 변경이 없습니다.";
                return true;
            }

            string original = EditorJsonUtility.ToJson(source);
            try
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Project T 데이터 적용");
                Undo.RegisterCompleteObjectUndo(source, "Project T 데이터 적용");
                CopyValues(draft, source);
                EditorUtility.SetDirty(source);
                AssetDatabase.SaveAssetIfDirty(source);
                Undo.FlushUndoRecordObjects();
                baseline = EditorJsonUtility.ToJson(draft);
                sourceSnapshot = EditorJsonUtility.ToJson(source);
                reason = "저장했습니다.";
                return true;
            }
            catch (Exception exception)
            {
                EditorJsonUtility.FromJsonOverwrite(original, source);
                EditorUtility.SetDirty(source);
                reason = "저장 실패로 메모리 원본을 복구했습니다. 파일 접근 상태를 확인하세요.";
                SWLog.LogError("[DataEditSession] 적용 실패: " + exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 직렬화 값을 복사하며 대상의 이름과 수명 설정을 보존합니다.
        /// </summary>
        private static void CopyValues(ScriptableObject from, ScriptableObject to)
        {
            if (from == null || to == null || from.GetType() != to.GetType())
            {
                SWLog.LogWarning("[DataEditSession] 복사 실패: 같은 종류의 원본과 대상이 필요합니다.");
                return;
            }

            string name = to.name;
            HideFlags flags = to.hideFlags;
            EditorUtility.CopySerialized(from, to);
            to.name = name;
            to.hideFlags = flags;
        }

        /// <summary>
        /// 편집용 메모리와 직렬화 연결만 해제합니다. 원본은 제거하지 않습니다.
        /// </summary>
        public void Dispose()
        {
            serialized?.Dispose();
            serialized = null;
            if (draft != null)
            {
                Undo.ClearUndo(draft);
                UnityEngine.Object.DestroyImmediate(draft);
                draft = null;
            }
        }

        #endregion // 적용
    }
}
