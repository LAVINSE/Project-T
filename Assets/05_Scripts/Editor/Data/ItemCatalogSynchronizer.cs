using System;
using UnityEditor;

using SW.Util;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 아이템 생성·복제·이동·삭제 후 실행용 목록을 동기화합니다. 플레이어 저장 파일과 보유 수량은 변경하지 않습니다.
    /// </summary>
    [InitializeOnLoad]
    public sealed class ItemCatalogSynchronizer : AssetPostprocessor
    {
        #region 초기화
        /// <summary>
        /// 편집기 준비와 플레이 진입 전에 아이템 목록을 갱신합니다.
        /// </summary>
        static ItemCatalogSynchronizer()
        {
            Schedule();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 플레이 진입 직전 현재 아이템 참조를 저장합니다.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Synchronize();
            }
        }

        #endregion // 초기화

        #region 동기화
        /// <summary>
        /// 자산 변경 처리가 끝난 뒤 목록 갱신을 예약합니다. 목록 자체 저장은 재예약하지 않습니다.
        /// </summary>
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (path != ProjectDefine.Inventory.CatalogPath && path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    Schedule();
                    return;
                }
            }

            if (deleted.Length > 0 || moved.Length > 0)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 같은 편집기 갱신에서 여러 자산 변경을 한 번 처리합니다.
        /// </summary>
        private static void Schedule()
        {
            EditorApplication.delayCall -= Synchronize;
            EditorApplication.delayCall += Synchronize;
        }

        /// <summary>
        /// 아이템 자산 식별자와 참조를 정렬해 저장합니다. 목록 자산이 아직 없으면 생성하지 않고 종료합니다.
        /// </summary>
        public static void Synchronize()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalogData>(ProjectDefine.Inventory.CatalogPath);
            if (catalog == null)
            {
                return;
            }

            string[] identifiers = AssetDatabase.FindAssets("t:ItemData", new[] { DataCatalog.DataFolder });
            Array.Sort(identifiers, StringComparer.Ordinal);
            var serialized = new SerializedObject(catalog);
            SerializedProperty items = serialized.FindProperty("items");
            if (items == null)
            {
                SWLog.LogWarning("[ItemCatalogSynchronizer] 목록 연결 실패: items 속성이 없습니다.");
                return;
            }

            bool changed = items.arraySize != identifiers.Length;
            items.arraySize = identifiers.Length;
            for (int index = 0; index < identifiers.Length; index++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(identifiers[index]));
                SerializedProperty entry = items.GetArrayElementAtIndex(index);
                SerializedProperty identifier = entry.FindPropertyRelative("identifier");
                SerializedProperty reference = entry.FindPropertyRelative("definition");
                if (identifier == null || reference == null || definition == null)
                {
                    SWLog.LogWarning("[ItemCatalogSynchronizer] 목록 갱신 실패: 아이템 식별자 또는 정의를 확인하세요.");
                    return;
                }

                changed |= identifier.stringValue != identifiers[index] || reference.objectReferenceValue != definition;
                identifier.stringValue = identifiers[index];
                reference.objectReferenceValue = definition;
            }

            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        #endregion // 동기화
    }
}
