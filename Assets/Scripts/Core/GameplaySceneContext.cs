using DataDrivenDemo.Interaction;
using DataDrivenDemo.Quest;
using DataDrivenDemo.UI;
using UnityEngine;

namespace DataDrivenDemo.Core
{
    /// <summary>
    /// DemoScene 런타임 참조 허브. UI/퀘스트/월드 마커가 Find 반복 없이 같은 인스턴스를 씁니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplaySceneContext : MonoBehaviour
    {
        public static GameplaySceneContext Instance { get; private set; }

        [SerializeField] private QuestSystem questSystem;
        [SerializeField] private QuestCatalog questCatalog;
        [SerializeField] private QuestHudView questHud;
        [SerializeField] private QuestJournalView questJournal;
        [SerializeField] private QuestObjectiveWorldMarkerManager[] worldMarkerManagers;
        [Header("Optional wired refs (reduces Find fallback)")]
        [SerializeField] private QuestOfferView questOfferView;
        [SerializeField] private ProximityInteractor proximityInteractor;

        public QuestSystem QuestSystem => questSystem;
        public QuestCatalog QuestCatalog => questCatalog;
        public QuestHudView QuestHud => questHud;
        public QuestJournalView QuestJournal => questJournal;
        public QuestOfferView QuestOfferView => questOfferView;
        public ProximityInteractor ProximityInteractor => proximityInteractor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameplaySceneContext] Duplicate instance ignored.");
                return;
            }

            Instance = this;
            ResolveReferencesOnce();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void RefreshWorldMarkers()
        {
            if (questSystem == null || worldMarkerManagers == null)
                return;

            foreach (var m in worldMarkerManagers)
            {
                if (m != null && m.isActiveAndEnabled)
                    m.RefreshFrom(questSystem);
            }
        }

        private void ResolveReferencesOnce()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            if (questCatalog == null && questSystem != null)
                questCatalog = questSystem.GetComponent<QuestCatalog>();
            if (questCatalog == null)
                questCatalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            if (questHud == null)
                questHud = FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);
            if (questJournal == null)
                questJournal = FindFirstObjectByType<QuestJournalView>(FindObjectsInactive.Include);

            if (worldMarkerManagers == null || worldMarkerManagers.Length == 0)
                worldMarkerManagers = FindObjectsByType<QuestObjectiveWorldMarkerManager>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (questOfferView == null)
                questOfferView = FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include);
            if (proximityInteractor == null)
                proximityInteractor = FindFirstObjectByType<ProximityInteractor>(FindObjectsInactive.Include);
        }
    }
}
