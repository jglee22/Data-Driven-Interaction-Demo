using DataDrivenDemo.Core.Save;
using DataDrivenDemo.Interaction;
using DataDrivenDemo.Quest;
using NUnit.Framework;
using UnityEngine;

namespace DataDrivenDemo.Tests.Editor
{
    public sealed class QuestSystemTests
    {
        private GameObject _root;
        private QuestCatalog _catalog;
        private QuestSystem _system;
        private PlayerPrefsSaveService _save;

        [SetUp]
        public void SetUp()
        {
            InteractableRegistry.ClearForTests();
            _save = new PlayerPrefsSaveService();
            SaveServices.QuestSave = _save;
            ClearAllQuestPrefs();

            _root = new GameObject("QuestSystemTests");
            _catalog = _root.AddComponent<QuestCatalog>();
            _system = _root.AddComponent<QuestSystem>();
            _system.SetSkipStartHydrateForTests(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);

            InteractableRegistry.ClearForTests();
            ClearAllQuestPrefs();
        }

        [Test]
        public void Accept_AddsRuntime()
        {
            _catalog.RegisterDefinitionsForTests(MakePickupQuest("test_accept", "item_a"));
            _system.ConfigureForTests(_catalog);

            Assert.IsTrue(_system.Accept("test_accept"));
            Assert.IsTrue(_system.IsAccepted("test_accept"));
        }

        [Test]
        public void PickupEvent_AdvancesStepCount()
        {
            _catalog.RegisterDefinitionsForTests(MakePickupQuest("test_pickup", "item_x"));
            _system.ConfigureForTests(_catalog);
            Assert.IsTrue(_system.Accept("test_pickup"));

            QuestEvents.RaiseEvent(new QuestEvent(QuestEventType.Pickup, "item_x", 1, ""));

            Assert.IsTrue(_system.TryGetQuestState("test_pickup", out var st));
            Assert.AreEqual(1, st.stepIndex);
            Assert.IsTrue(st.completed);
        }

        [Test]
        public void TurnIn_SubmitAfterCompleted_SetsTurnedIn()
        {
            const string questId = "test_turnin";
            _catalog.RegisterDefinitionsForTests(MakeTurnInQuest(questId, "term_1"));
            _save.SaveQuestState(new QuestState
            {
                questId = questId,
                stepIndex = 1,
                stepCount = 0,
                completed = true,
                turnedIn = false
            });

            _system.ConfigureForTests(_catalog);
            Assert.IsTrue(_system.Accept(questId));

            QuestEvents.RaiseEvent(new QuestEvent(QuestEventType.Submit, "term_1", 1, ""));

            Assert.IsTrue(_system.TryGetQuestState(questId, out var st));
            Assert.IsTrue(st.turnedIn);
        }

        [Test]
        public void Hydrate_RestoresAcceptedQuestFromPlayerPrefs()
        {
            const string questId = "test_hydrate";
            _save.SaveAcceptedQuestIdsAsync(new[] { questId });
            _save.SaveQuestState(new QuestState { questId = questId, stepIndex = 0, stepCount = 0 });

            Object.DestroyImmediate(_root);
            _root = null;

            // TearDown 전에 다른 테스트/초기화가 SaveServices를 바꿨을 수 있으므로 명시적으로 맞춤.
            SaveServices.QuestSave = _save;

            var hydrateRoot = new GameObject("HydrateTest");
            var hydrateCatalog = hydrateRoot.AddComponent<QuestCatalog>();
            hydrateCatalog.RegisterDefinitionsForTests(MakePickupQuest(questId, "item_h"));
            var hydrateSystem = hydrateRoot.AddComponent<QuestSystem>();
            hydrateSystem.SetSkipStartHydrateForTests(true);
            hydrateSystem.SetCatalogForTests(hydrateCatalog);
            hydrateSystem.RunHydrateBlockingForTests();

            Assert.IsTrue(hydrateSystem.IsHydrated);
            Assert.IsTrue(hydrateSystem.IsAccepted(questId));

            Object.DestroyImmediate(hydrateRoot);
        }

        [Test]
        public void InteractableRegistry_PrefersNonGiver()
        {
            var npcRoot = new GameObject("npc_shared");
            var npc = npcRoot.AddComponent<NpcInteractable>();

            var giverRoot = new GameObject("giver");
            giverRoot.SetActive(false);
            var giver = giverRoot.AddComponent<QuestGiverInteractable>();
            typeof(InteractableBase).GetField("id",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(giver, "npc_shared");
            giverRoot.SetActive(true);

            InteractableRegistry.Register(npc);
            InteractableRegistry.Register(giver);

            Assert.IsTrue(InteractableRegistry.TryGet("npc_shared", out var found));
            Assert.IsInstanceOf<NpcInteractable>(found);

            Object.DestroyImmediate(npcRoot);
            Object.DestroyImmediate(giverRoot);
        }

        private static QuestDefinition MakePickupQuest(string questId, string targetId) =>
            new()
            {
                id = questId,
                title = "Test",
                steps = new[]
                {
                    new QuestStep
                    {
                        objectives = new[]
                        {
                            new QuestObjective
                            {
                                type = "Pickup",
                                targetId = targetId,
                                requiredCount = 1
                            }
                        }
                    }
                }
            };

        private static QuestDefinition MakeTurnInQuest(string questId, string terminalId) =>
            new()
            {
                id = questId,
                title = "Test",
                steps = new[]
                {
                    new QuestStep
                    {
                        objectives = new[]
                        {
                            new QuestObjective
                            {
                                type = "Submit",
                                targetId = terminalId,
                                requiredCount = 1
                            }
                        }
                    }
                }
            };

        private static void ClearAllQuestPrefs()
        {
            PlayerPrefs.DeleteKey(QuestSaveKeys.AcceptedList);
            foreach (var id in new[] { "test_accept", "test_pickup", "test_turnin", "test_hydrate" })
                PlayerPrefs.DeleteKey(QuestSaveKeys.StateKey(id));
            PlayerPrefs.Save();
        }
    }
}
