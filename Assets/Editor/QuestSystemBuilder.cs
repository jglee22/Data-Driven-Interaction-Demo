using DataDrivenDemo.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

namespace DataDrivenDemo.EditorTools
{
    public static class QuestSystemBuilder
    {
        [MenuItem("Tools/DataDrivenDemo/Build Quest System (Multi)")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestSystemBuilder] 활성 씬이 없습니다.");
                return;
            }

            var existing = Object.FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                Debug.LogWarning("[QuestSystemBuilder] 이미 QuestSystem 이 있습니다.");
                return;
            }

            var go = new GameObject("QuestSystem", typeof(QuestCatalog), typeof(QuestSystem));
            Undo.RegisterCreatedObjectUndo(go, "Create QuestSystem");

            var catalog = go.GetComponent<QuestCatalog>();
            var sys = go.GetComponent<QuestSystem>();

            // Data/Json 아래의 quest_*.json을 전부 catalog에 연결
            var jsonDir = "Assets/Data/Json";
            var guids = AssetDatabase.FindAssets("t:TextAsset quest_", new[] { jsonDir });
            var assets = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => p.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                .Select(p => AssetDatabase.LoadAssetAtPath<TextAsset>(p))
                .Where(a => a != null)
                .ToArray();

            SetPrivateField(catalog, "questJsons", assets);
            catalog.Rebuild();

            // QuestHudView가 있으면 자동 연결
            var hud = Object.FindFirstObjectByType<DataDrivenDemo.UI.QuestHudView>(FindObjectsInactive.Include);
            SetPrivateField(sys, "catalog", catalog);
            if (hud != null)
                SetPrivateField(sys, "hud", hud);

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[QuestSystemBuilder] 생성 완료. catalog quests={assets.Length}");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var t = target.GetType();
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning($"[QuestSystemBuilder] Field not found: {t.Name}.{fieldName}");
                return;
            }
            f.SetValue(target, value);
            EditorUtility.SetDirty((Object)target);
        }
    }
}

