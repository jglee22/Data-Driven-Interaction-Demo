using System.IO;
using DataDrivenDemo.Quest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataDrivenDemo.EditorTools
{
    public static class QuestDebugAccepterBuilder
    {
        private const string JsonDir = "Assets/Data/Json";

        [MenuItem("Tools/DataDrivenDemo/Build Quest Debug Accepter (F1~F5)")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestDebugAccepterBuilder] 활성 씬이 없습니다.");
                return;
            }

            var existing = Object.FindFirstObjectByType<QuestDebugAccepter>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                Debug.LogWarning("[QuestDebugAccepterBuilder] 이미 QuestDebugAccepter 가 있습니다.");
                return;
            }

            EnsureSampleJsonsMissingOnly();

            var go = new GameObject("QuestDebugAccepter", typeof(QuestDebugAccepter));
            Undo.RegisterCreatedObjectUndo(go, "Create QuestDebugAccepter");

            var accepter = go.GetComponent<QuestDebugAccepter>();

            var assets = new TextAsset[5];
            for (var i = 0; i < 5; i++)
            {
                var path = $"{JsonDir}/quest_{(i + 1).ToString("000")}.json";
                assets[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            }

            SetPrivateField(accepter, "questJsons", assets);

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[QuestDebugAccepterBuilder] 생성 완료. Play 중 F1~F5로 수락, F12로 초기화.");
        }

        /// <summary>
        /// quest_002~005 JSON이 없을 때만 샘플을 만듭니다. 데모용으로 수정한 파일은 덮어쓰지 않습니다.
        /// </summary>
        private static void EnsureSampleJsonsMissingOnly()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Json"))
                AssetDatabase.CreateFolder("Assets/Data", "Json");

            var created = 0;
            var skipped = 0;

            for (var i = 0; i < 4; i++)
            {
                var id = (i + 2).ToString("000"); // quest_002~005만 생성/갱신
                var assetPath = $"{JsonDir}/quest_{id}.json";
                var onDisk = Path.Combine(Application.dataPath, "Data", "Json", $"quest_{id}.json");
                if (File.Exists(onDisk))
                {
                    skipped++;
                    continue;
                }

                var questId = $"quest_{id}";
                var title = $"테스트 퀘스트 {id}";
                // 현업형 테스트: type + targetId 로 매칭해서 "같이 올라가는" 문제를 피함.
                // 씬 오브젝트의 InteractableBase.id 를 npc_001 / item_001 / terminal_001 같은 형태로 맞추면 됨.
                var npcTarget = $"npc_{id}";
                var itemTarget = $"item_{id}";
                var terminalTarget = $"terminal_{id}";

                var json =
                    "{\n" +
                    $"  \"id\": \"{questId}\",\n" +
                    $"  \"title\": \"{title}\",\n" +
                    "  \"reward\": { \"coins\": 1 },\n" +
                    "  \"steps\": [\n" +
                    "    {\n" +
                    "      \"objectives\": [\n" +
                    $"        {{ \"type\": \"Talk\", \"targetId\": \"{npcTarget}\", \"requiredCount\": 2, \"uiText\": \"NPC에게 2번 말 걸기\" }}\n" +
                    "      ]\n" +
                    "    },\n" +
                    "    {\n" +
                    "      \"objectives\": [\n" +
                    $"        {{ \"type\": \"Pickup\", \"targetId\": \"{itemTarget}\", \"uiText\": \"아이템 줍기\" }}\n" +
                    "      ]\n" +
                    "    },\n" +
                    "    {\n" +
                    "      \"objectives\": [\n" +
                    $"        {{ \"type\": \"Submit\", \"targetId\": \"{terminalTarget}\", \"uiText\": \"터미널에 제출하기\" }}\n" +
                    "      ]\n" +
                    "    }\n" +
                    "  ]\n" +
                    "}\n";

                File.WriteAllText(onDisk, json);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                created++;
            }

            AssetDatabase.Refresh();
            if (created > 0 || skipped > 0)
                Debug.Log($"[QuestDebugAccepterBuilder] 샘플 JSON: 새로 생성 {created}개, 기존 유지 {skipped}개(덮어쓰지 않음).");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var t = target.GetType();
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning($"[QuestDebugAccepterBuilder] Field not found: {t.Name}.{fieldName}");
                return;
            }

            f.SetValue(target, value);
            EditorUtility.SetDirty((Object)target);
        }
    }
}

