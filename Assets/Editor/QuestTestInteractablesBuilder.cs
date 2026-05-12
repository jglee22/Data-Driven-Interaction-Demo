using System;
using DataDrivenDemo.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DataDrivenDemo.EditorTools
{
    /// <summary>
    /// 퀘스트 001은 씬에 있는 npc_001 / item_001 / terminal_001를 그대로 사용합니다.
    /// 이 빌더는 002~005용 복제 오브젝트만 추가합니다(navigationId는 npc_002 등).
    /// </summary>
    public static class QuestTestInteractablesBuilder
    {
        [MenuItem("Tools/DataDrivenDemo/Build Test Interactables (002~005)")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestTestInteractablesBuilder] 활성 씬이 없습니다.");
                return;
            }

            var npcTemplate = FindById<NpcInteractable>("npc_001");
            var itemTemplate = FindById<ItemPickupInteractable>("item_001");
            var terminalTemplate = FindById<TerminalSubmitInteractable>("terminal_001");

            if (npcTemplate == null || itemTemplate == null || terminalTemplate == null)
            {
                Debug.LogWarning("[QuestTestInteractablesBuilder] 템플릿을 찾지 못했습니다. 씬에 npc_001/item_001/terminal_001 id가 있는지 확인하세요.");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Test Interactables 002~005");

            var container = GetOrCreateContainer("TestInteractables_002_005");
            var npcFolder = GetOrCreateChild(container.transform, "NPCs");
            var itemFolder = GetOrCreateChild(container.transform, "Items");
            var terminalFolder = GetOrCreateChild(container.transform, "Terminals");

            const int from = 2;
            const int to = 5;
            var created = 0;
            var skipped = 0;
            var moved = 0;

            // 좁은 필드에서도 들어가게: 그리드 배치(여러 줄)
            const int columns = 2;          // 002~005를 2열로 배치
            const float colSpacing = 1.25f; // 열 간격
            const float rowSpacing = 1.15f; // 행 간격
            const float laneSpacing = 0.95f; // NPC/Item/Terminal 라인 간격(Z)

            var baseNpcPos = npcTemplate.transform.position;
            var baseItemPos = itemTemplate.transform.position;
            var baseTerminalPos = terminalTemplate.transform.position;

            var laneNpc = Vector3.zero;
            var laneItem = new Vector3(0f, 0f, -laneSpacing);
            var laneTerminal = new Vector3(0f, 0f, -laneSpacing * 2f);

            for (var i = from; i <= to; i++)
            {
                var suffix = i.ToString("000");
                var npcId = $"npc_{suffix}";
                var itemId = $"item_{suffix}";
                var terminalId = $"terminal_{suffix}";

                // 이미 존재하면 생성 대신 컨테이너로 이동/재배치(사용자가 이전에 생성해둔 경우 포함)
                var npcExisting = FindAnyByRawId(npcId);
                var itemExisting = FindAnyByRawId(itemId);
                var terminalExisting = FindAnyByRawId(terminalId);

                var idx = i - from; // 0..8
                var col = idx % columns;
                var row = idx / columns;

                // X는 열, Z는 행(+ 아래로)으로 배치
                var gridOffset = new Vector3(col * colSpacing, 0f, -row * rowSpacing);

                var npc = npcExisting != null ? npcExisting.gameObject : Duplicate(npcTemplate.gameObject, npcFolder, baseNpcPos + gridOffset + laneNpc);
                if (npcExisting != null)
                {
                    MoveUnder(npc, npcFolder);
                    npc.transform.position = baseNpcPos + gridOffset + laneNpc;
                    moved++;
                }
                else
                {
                    created++;
                }
                SetInteractableIds(npc, npcId, $"NPC {suffix}");

                var item = itemExisting != null ? itemExisting.gameObject : Duplicate(itemTemplate.gameObject, itemFolder, baseItemPos + gridOffset + laneItem);
                if (itemExisting != null)
                {
                    MoveUnder(item, itemFolder);
                    item.transform.position = baseItemPos + gridOffset + laneItem;
                    moved++;
                }
                else
                {
                    created++;
                }
                SetInteractableIds(item, itemId, $"아이템 {suffix}");

                var term = terminalExisting != null ? terminalExisting.gameObject : Duplicate(terminalTemplate.gameObject, terminalFolder, baseTerminalPos + gridOffset + laneTerminal);
                if (terminalExisting != null)
                {
                    MoveUnder(term, terminalFolder);
                    term.transform.position = baseTerminalPos + gridOffset + laneTerminal;
                    moved++;
                }
                else
                {
                    created++;
                }
                SetInteractableIds(term, terminalId, $"터미널 {suffix}");

                if (npcExisting != null || itemExisting != null || terminalExisting != null)
                    skipped++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[QuestTestInteractablesBuilder] 완료. created={created}, moved={moved}, processedSets={skipped} (npc/item/terminal 002~005)");
        }

        private static GameObject GetOrCreateContainer(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Test Interactables Container");
            go.transform.position = Vector3.zero;
            return go;
        }

        private static Transform GetOrCreateChild(Transform parent, string name)
        {
            if (parent == null) return null;
            var existing = parent.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Test Interactables Folder");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static T FindById<T>(string id) where T : MonoBehaviour
        {
            foreach (var x in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (x == null) continue;
                if (x is not InteractableBase b) continue;
                if (string.Equals(GetRawId(b), id, StringComparison.Ordinal))
                    return x;
            }
            return null;
        }

        private static bool ExistsId(string id)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b == null) continue;
                if (string.Equals(GetRawId(b), id, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static InteractableBase FindAnyByRawId(string id)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (b == null) continue;
                if (string.Equals(GetRawId(b), id, StringComparison.Ordinal))
                    return b;
            }
            return null;
        }

        private static void MoveUnder(GameObject go, Transform parent)
        {
            if (go == null || parent == null) return;
            Undo.SetTransformParent(go.transform, parent, "Reparent Test Interactable");
        }

        private static GameObject Duplicate(GameObject template, Transform parent, Vector3 position)
        {
            // 씬 오브젝트를 템플릿으로 쓰므로 InstantiatePrefab(프리팹 에셋 전용) 대신 안전한 Instantiate 사용
            var go = UnityEngine.Object.Instantiate(template);

            Undo.RegisterCreatedObjectUndo(go, "Duplicate Test Interactable");

            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.name = template.name; // 이름은 템플릿과 동일하게 유지(필요하면 Unity가 (1) 붙임)
            return go;
        }

        private static void SetInteractableIds(GameObject go, string id, string displayName)
        {
            if (go == null) return;

            // InteractableBase의 private 필드(id/displayName)를 SerializedObject로 세팅
            var b = go.GetComponent<InteractableBase>();
            if (b == null) return;

            var so = new SerializedObject(b);
            var idProp = so.FindProperty("id");
            var nameProp = so.FindProperty("displayName");
            if (idProp != null) idProp.stringValue = id;
            if (nameProp != null) nameProp.stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(go);
            EditorUtility.SetDirty(b);
        }

        private static string GetRawId(InteractableBase b)
        {
            if (b == null) return "";
            var so = new SerializedObject(b);
            var idProp = so.FindProperty("id");
            return idProp != null ? (idProp.stringValue ?? "") : "";
        }
    }
}

