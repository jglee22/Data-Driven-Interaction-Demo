using System.Collections.Generic;
using DataDrivenDemo.Interaction;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 의뢰 NPC(!) / 진행 목표(?) 월드 스프라이트 마커. 오브젝트 풀링으로 재사용합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestObjectiveWorldMarkerManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private QuestSystem questSystem;

        [Header("Quest giver (의뢰 아이콘)")]
        [Tooltip("비어 있으면 씬의 QuestGiverInteractable을 Awake에서 한 번만 수집합니다.")]
        [SerializeField] private QuestGiverInteractable[] questGiverOnly;

        [Header("Sprites")]
        [SerializeField] private Sprite questGiverSprite;
        [SerializeField] private Sprite questObjectiveSprite;

        [Header("Layout")]
        [SerializeField] private float heightOffset = 2.2f;
        [SerializeField] private float iconWorldScale = 1f;
        [SerializeField] private int sortingOrder = 500;

        private readonly Dictionary<int, GameObject> objectiveByAnchorId = new();
        private readonly Dictionary<int, GameObject> giverByAnchorId = new();
        private QuestGiverInteractable[] cachedGivers;

        private void Awake()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);

            CacheGivers();
        }

        private void OnEnable()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            RefreshFrom(questSystem);
        }

        public void RefreshFrom(QuestSystem sys)
        {
            if (!isActiveAndEnabled)
                return;

            if (sys == null)
                sys = questSystem;

            var activeObjectiveIds = new HashSet<int>();
            var activeGiverIds = new HashSet<int>();

            var objectiveAnchors = new HashSet<Transform>();
            if (sys != null)
                sys.GetWorldMarkerObjectiveAnchors(objectiveAnchors);

            foreach (var t in objectiveAnchors)
            {
                if (t == null || questObjectiveSprite == null)
                    continue;

                var id = t.GetInstanceID();
                activeObjectiveIds.Add(id);
                SyncMarker(objectiveByAnchorId, id, t, questObjectiveSprite, sortingOrder);
            }

            DeactivateStale(objectiveByAnchorId, activeObjectiveIds);

            if (questGiverSprite == null)
                return;

            var givers = cachedGivers;
            if (givers == null || givers.Length == 0)
                CacheGivers();

            givers = cachedGivers;
            if (givers == null)
                return;

            foreach (var giver in givers)
            {
                if (giver == null)
                    continue;
                if (sys != null && !sys.GiverHasAnyAcceptableOffer(giver))
                    continue;

                var id = giver.transform.GetInstanceID();
                activeGiverIds.Add(id);
                SyncMarker(giverByAnchorId, id, giver.transform, questGiverSprite, sortingOrder + 50);
            }

            DeactivateStale(giverByAnchorId, activeGiverIds);
        }

        private void CacheGivers()
        {
            if (questGiverOnly != null && questGiverOnly.Length > 0)
            {
                cachedGivers = questGiverOnly;
                return;
            }

            cachedGivers = FindObjectsByType<QuestGiverInteractable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static void DeactivateStale(Dictionary<int, GameObject> pool, HashSet<int> activeIds)
        {
            var toDeactivate = new List<int>();
            foreach (var kv in pool)
            {
                if (!activeIds.Contains(kv.Key) && kv.Value != null)
                    toDeactivate.Add(kv.Key);
            }

            foreach (var id in toDeactivate)
            {
                if (pool.TryGetValue(id, out var go) && go != null)
                    go.SetActive(false);
            }
        }

        private void SyncMarker(
            Dictionary<int, GameObject> pool,
            int anchorId,
            Transform anchor,
            Sprite sprite,
            int sortingOrderOverride)
        {
            if (!pool.TryGetValue(anchorId, out var go) || go == null)
            {
                go = CreateMarker(anchor, sprite, sortingOrderOverride);
                pool[anchorId] = go;
            }

            go.SetActive(true);
            var billboard = go.GetComponent<QuestFloatingMarker>();
            if (billboard != null)
                billboard.Initialize(anchor, heightOffset);

            if (anchor != null)
                go.transform.position = anchor.position + Vector3.up * heightOffset;
        }

        private GameObject CreateMarker(Transform anchor, Sprite sprite, int sortingOrderOverride)
        {
            var go = new GameObject($"QuestMarker_{sprite.name}");
            go.transform.SetParent(null, true);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = sortingOrderOverride;

            var scale = Mathf.Max(0.25f, iconWorldScale);
            go.transform.localScale = Vector3.one * scale;

            var billboard = go.AddComponent<QuestFloatingMarker>();
            billboard.Initialize(anchor, heightOffset);

            if (anchor != null)
                go.transform.position = anchor.position + Vector3.up * heightOffset;

            return go;
        }

        private void OnDestroy()
        {
            DestroyPool(objectiveByAnchorId);
            DestroyPool(giverByAnchorId);
        }

        private static void DestroyPool(Dictionary<int, GameObject> pool)
        {
            foreach (var kv in pool)
            {
                if (kv.Value != null)
                    Destroy(kv.Value);
            }

            pool.Clear();
        }
    }
}
