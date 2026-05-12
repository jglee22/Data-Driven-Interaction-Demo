using System.Collections.Generic;
using DataDrivenDemo.Interaction;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 의뢰 NPC 위에는 questGiverSprite(bonus_02 등), 진행 중 목표 오브젝트 위에는 objectiveSprite(bonus_01 등)를 표시합니다.
    /// 의뢰 아이콘: questGiverOnly 배열이 비어 있으면 씬의 모든 QuestGiverInteractable 에 붙습니다(여러 개면 전부).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestObjectiveWorldMarkerManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private QuestSystem questSystem;

        [Header("Quest giver (의뢰 아이콘)")]
        [Tooltip("비어 있으면: 씬에 있는 모든 QuestGiverInteractable 에 bonus_02 가 붙습니다.\n" +
                 "1명만 의뢰 NPC 라면: 그 오브젝트의 QuestGiverInteractable 만 배열에 넣으세요.")]
        [SerializeField] private QuestGiverInteractable[] questGiverOnly;

        [Header("Sprites (예: Clean Vector Icons/Update/bonus_02, bonus_01)")]
        [SerializeField] private Sprite questGiverSprite;
        [SerializeField] private Sprite questObjectiveSprite;

        [Header("Layout")]
        [SerializeField] private float heightOffset = 2.2f;
        [SerializeField] private float iconWorldScale = 1f;
        [SerializeField] private int sortingOrder = 500;

        private readonly List<GameObject> spawned = new();

        private void Awake()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
        }

        public void RefreshFrom(QuestSystem sys)
        {
            ClearSpawned();

            if (!isActiveAndEnabled)
                return;

            if (sys == null)
                sys = questSystem;
            if (sys == null)
                return;

            var objectiveAnchors = new HashSet<Transform>();
            sys.GetWorldMarkerObjectiveAnchors(objectiveAnchors);

            foreach (var t in objectiveAnchors)
            {
                if (t == null || questObjectiveSprite == null)
                    continue;
                SpawnOne(t, questObjectiveSprite);
            }

            if (questGiverOnly != null && questGiverOnly.Length > 0)
            {
                foreach (var giver in questGiverOnly)
                {
                    if (giver == null || questGiverSprite == null)
                        continue;
                    var tr = giver.transform;
                    if (objectiveAnchors.Contains(tr))
                        continue;
                    SpawnOne(tr, questGiverSprite);
                }
            }
            else
            {
                foreach (var giver in UnityEngine.Object.FindObjectsByType<QuestGiverInteractable>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (giver == null || questGiverSprite == null)
                        continue;
                    var tr = giver.transform;
                    if (objectiveAnchors.Contains(tr))
                        continue;
                    SpawnOne(tr, questGiverSprite);
                }
            }
        }

        private void ClearSpawned()
        {
            foreach (var go in spawned)
            {
                if (go != null)
                    Destroy(go);
            }

            spawned.Clear();
        }

        private void OnDestroy() => ClearSpawned();

        private void SpawnOne(Transform anchor, Sprite sprite)
        {
            var go = new GameObject($"QuestMarker_{sprite.name}");
            // 부모가 멀리 있으면 첫 프레임 위치가 꼬일 수 있어 씬 루트에 둡니다.
            go.transform.SetParent(null, true);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder;

            var scale = Mathf.Max(0.25f, iconWorldScale);
            go.transform.localScale = Vector3.one * scale;

            var billboard = go.AddComponent<QuestFloatingMarker>();
            billboard.Initialize(anchor, heightOffset);

            if (anchor != null)
                go.transform.position = anchor.position + Vector3.up * heightOffset;

            spawned.Add(go);
        }
    }
}
