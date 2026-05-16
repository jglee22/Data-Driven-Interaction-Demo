using System.Collections.Generic;
using DataDrivenDemo.Interaction;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 의뢰 NPC 위에는 questGiverSprite(bonus_02 등), 진행 중 목표 오브젝트 위에는 objectiveSprite(bonus_01 등)를 표시합니다.
    /// 의뢰 아이콘: 수락 가능한 의뢰가 있을 때만 표시합니다. questGiverOnly 가 비어 있으면 씬의 모든 QuestGiverInteractable 을 검사합니다.
    /// 갱신 시 마커를 Destroy 후 재생성합니다(데모 규모용). 대량 오브젝트에서는 풀링·캐시 전환을 고려하세요.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestObjectiveWorldMarkerManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private QuestSystem questSystem;

        [Header("Quest giver (의뢰 아이콘)")]
        [Tooltip("비어 있으면: 씬의 모든 QuestGiverInteractable 을 대상으로, 수락 가능한 의뢰가 있을 때만 ! 마커를 띄웁니다.\n" +
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

        private void OnEnable()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            // QuestSystem.RenderUi 는 수락/리셋 등 이벤트 때만 호출됩니다. 매니저가 나중에 활성화되면 여기서 한 번 맞춥니다.
            RefreshFrom(questSystem);
        }

        public void RefreshFrom(QuestSystem sys)
        {
            if (!isActiveAndEnabled)
                return;

            ClearSpawned();

            if (sys == null)
                sys = questSystem;

            var objectiveAnchors = new HashSet<Transform>();
            if (sys != null)
                sys.GetWorldMarkerObjectiveAnchors(objectiveAnchors);

            // 진행 마커(?) — 수락된 퀘스트의 현재 목표만
            foreach (var t in objectiveAnchors)
            {
                if (t == null || questObjectiveSprite == null)
                    continue;
                SpawnOne(t, questObjectiveSprite, sortingOrder);
            }

            // 의뢰 마크(!) — 아직 수락할 수 있는 의뢰가 있을 때만
            if (questGiverOnly != null && questGiverOnly.Length > 0)
            {
                foreach (var giver in questGiverOnly)
                {
                    if (giver == null || questGiverSprite == null)
                        continue;
                    if (sys != null && !sys.GiverHasAnyAcceptableOffer(giver))
                        continue;
                    SpawnOne(giver.transform, questGiverSprite, sortingOrder + 50);
                }
            }
            else
            {
                foreach (var giver in UnityEngine.Object.FindObjectsByType<QuestGiverInteractable>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (giver == null || questGiverSprite == null)
                        continue;
                    if (sys != null && !sys.GiverHasAnyAcceptableOffer(giver))
                        continue;
                    SpawnOne(giver.transform, questGiverSprite, sortingOrder + 50);
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

        private void SpawnOne(Transform anchor, Sprite sprite, int sortingOrderOverride)
        {
            var go = new GameObject($"QuestMarker_{sprite.name}");
            // 부모가 멀리 있으면 첫 프레임 위치가 꼬일 수 있어 씬 루트에 둡니다.
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

            spawned.Add(go);
        }
    }
}
