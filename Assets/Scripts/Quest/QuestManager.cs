using System.Collections;
using UnityEngine;
using DataDrivenDemo.UI;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.Firebase;
using System;

namespace DataDrivenDemo.Quest
{
    [DisallowMultipleComponent]
    public sealed class QuestManager : MonoBehaviour
    {
        [Header("Quest Data (JSON)")]
        [SerializeField] private TextAsset questJson;

        [Header("UI")]
        [SerializeField] private QuestHudView hud;

        [Header("Save")]
        [SerializeField] private bool autoLoad = true;
        [SerializeField] private bool autoSave = true;

        [Header("Turn-in")]
        [SerializeField] private string turnInActionId = "submit_terminal";

        private QuestDefinition def;
        private int stepIndex;
        private int stepCount;
        private int coins;
        private bool turnedIn;
        /// <summary>비동기 로드 전에 초기 상태를 저장소에 쓰면 클라우드 진행도를 덮어쓸 수 있어 차단합니다.</summary>
        private bool allowSaveWrites;
        private bool disabledByQuestSystem;

        private void Awake()
        {
            // 멀티 퀘스트 런타임(QuestSystem)을 쓰는 씬에서는 단일 퀘스트 매니저가 HUD/트래커에 끼어들면 안 됩니다.
            if (FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include) != null)
            {
                disabledByQuestSystem = true;
                enabled = false;
                return;
            }

            LoadQuest();
            RefreshHud();
        }

        private IEnumerator Start()
        {
            if (!autoLoad)
            {
                allowSaveWrites = true;
                yield break;
            }

            var boot = FindAnyObjectByType<FirebaseBootstrap>(FindObjectsInactive.Include);
            if (boot != null)
            {
                const float authTimeout = 30f;
                var waitedAuth = 0f;
                while (!boot.IsReady && waitedAuth < authTimeout)
                {
                    waitedAuth += Time.unscaledDeltaTime;
                    yield return null;
                }

                // SignedIn 처리와 다른 스크립트 Awake 순서로 QuestSave 교체가 한 틱 늦을 수 있음.
                const float installerTimeout = 5f;
                var waitedInstall = 0f;
                while (SaveServices.QuestSave is PlayerPrefsSaveService && waitedInstall < installerTimeout)
                {
                    waitedInstall += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            TryLoadState();
            StartCoroutine(CoEnsureSaveUnlockedAfterTimeout(15f));
            RefreshHud();
        }

        private IEnumerator CoEnsureSaveUnlockedAfterTimeout(float secondsRealtime)
        {
            yield return new WaitForSecondsRealtime(secondsRealtime);
            if (allowSaveWrites)
                yield break;
            Debug.LogWarning("[QuestManager] Save load did not finish in time; allowing writes to avoid locking progress.");
            allowSaveWrites = true;
        }

        private void OnEnable()
        {
            if (disabledByQuestSystem) return;
            QuestEvents.ActionPerformed += OnActionPerformed;
        }

        private void OnDisable()
        {
            if (disabledByQuestSystem) return;
            QuestEvents.ActionPerformed -= OnActionPerformed;
        }

        private void LoadQuest()
        {
            def = QuestDefinitionLoader.FromTextAsset(questJson);
            stepIndex = 0;
            stepCount = 0;
            turnedIn = false;

            if (def == null || def.steps == null)
                Debug.LogWarning("[QuestManager] Quest JSON is missing/invalid.");
        }

        private void OnActionPerformed(string actionId)
        {
            if (def?.steps == null || def.steps.Length == 0)
                return;

            if (IsCompleted())
            {
                // 목표 완료 후 제출(보고) 액션이 들어오면 "완료(제출됨)" 상태로 전환
                if (!turnedIn && !string.IsNullOrWhiteSpace(turnInActionId) &&
                    string.Equals(actionId, turnInActionId, StringComparison.Ordinal))
                {
                    turnedIn = true;
                    RefreshHud();
                    hud?.ShowToast("제출 완료!");
                    if (autoSave)
                        SaveState();
                }
                return;
            }

            var expected = def.steps[stepIndex]?.actionId;
            if (string.IsNullOrWhiteSpace(expected))
                return;

            if (!string.Equals(expected, actionId))
                return;

            var required = Mathf.Max(1, def.steps[stepIndex].requiredCount);
            stepCount++;

            if (stepCount < required)
            {
                RefreshHud();
                hud?.ShowToast($"{stepCount}/{required}");
                if (autoSave)
                    SaveState();
                return;
            }

            stepIndex++;
            stepCount = 0;

                // 마지막 스텝이 제출 액션이면, 완료와 동시에 "제출됨" 처리
            if (!turnedIn &&
                IsCompleted() &&
                !string.IsNullOrWhiteSpace(turnInActionId) &&
                string.Equals(expected, turnInActionId, StringComparison.Ordinal))
            {
                turnedIn = true;
            }

            RefreshHud();

            if (IsCompleted())
            {
                var rewardCoins = def.reward != null ? def.reward.coins : 0;
                if (rewardCoins != 0)
                {
                    coins += rewardCoins;
                    hud?.SetCoins(coins);
                }

                hud?.ShowToast($"퀘스트 완료! 보상: 코인 +{rewardCoins}");
                Debug.Log($"[Quest] Completed: {def.id}, coins +{rewardCoins} (total={coins})");

                if (autoSave)
                    SaveState();
            }
            else
            {
                hud?.ShowToast("진행!");
                if (autoSave)
                    SaveState();
            }
        }

        private bool IsCompleted()
        {
            return def?.steps != null && stepIndex >= def.steps.Length;
        }

        private void RefreshHud()
        {
            if (hud == null)
                return;

            try
            {
                hud.SetTitle(def != null ? def.title : "퀘스트");
                hud.SetCoins(coins);

                var total = def?.steps != null ? def.steps.Length : 0;
                hud.SetProgress(stepIndex, total);

                var hasSteps = def?.steps != null && def.steps.Length > 0;
                var completed = hasSteps && IsCompleted();

                string objectiveText;
                string trackerDetail;
                var trackerStatus = completed ? (turnedIn ? QuestTrackerStatus.Completed : QuestTrackerStatus.TurnInReady) : QuestTrackerStatus.Active;

                if (!hasSteps)
                {
                    objectiveText = "퀘스트 데이터가 없습니다.";
                    trackerDetail = "데이터 없음";
                    trackerStatus = QuestTrackerStatus.Active;
                }
                else if (completed)
                {
                    objectiveText = "완료!";
                    trackerDetail = turnedIn ? "완료" : "보고 가능";
                }
                else
                {
                    var step = def.steps[stepIndex];
                    var objective = step?.objective;
                    var required = Mathf.Max(1, step.requiredCount);
                    var suffix = required > 1 ? $" ({Mathf.Clamp(stepCount, 0, required)}/{required})" : "";
                    objectiveText = (string.IsNullOrWhiteSpace(objective) ? "목표 진행" : objective) + suffix;
                    trackerDetail = string.IsNullOrWhiteSpace(objectiveText) ? "진행 중" : objectiveText;
                }

                hud.SetObjective(objectiveText);

                // Tracker list (optional) — 완료 상태도 반드시 갱신해야 UI가 바뀜
                if (def != null && !string.IsNullOrWhiteSpace(def.id))
                {
                    QuestTrackerService.Upsert(def.id, def.title, trackerDetail, trackerStatus, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    hud.RenderTracker(QuestTrackerService.Items);
                }
            }
            finally
            {
                hud.RefreshQuestJournalIfOpen();
            }
        }

        private void TryLoadState()
        {
            allowSaveWrites = false;
            if (def == null || string.IsNullOrWhiteSpace(def.id))
            {
                allowSaveWrites = true;
                return;
            }

            var svc = SaveServices.QuestSave;
            if (svc is FirestoreQuestSaveService)
            {
                svc.LoadQuestStateAsync(def.id, FinalizeHydrationLoad);
                return;
            }

            var saved = svc.LoadQuestState(def.id);
            if (saved != null)
                FinalizeHydrationLoad(saved);
            else
                svc.LoadQuestStateAsync(def.id, FinalizeHydrationLoad);
        }

        private void FinalizeHydrationLoad(QuestState saved)
        {
            ApplyLoadedState(saved);
            allowSaveWrites = true;
        }

        private void ApplyLoadedState(QuestState saved)
        {
            if (saved == null)
                return;

            stepIndex = Mathf.Max(0, saved.stepIndex);
            stepCount = Mathf.Max(0, saved.stepCount);
            coins = Mathf.Max(0, saved.coins);
            turnedIn = saved.turnedIn;

            // 이전 저장(turnedIn 필드 없던 시절) 호환: 완료 + 마지막 액션이 제출이면 제출된 것으로 간주
            if (!turnedIn && saved.completed && def?.steps != null && def.steps.Length > 0 &&
                !string.IsNullOrWhiteSpace(turnInActionId))
            {
                var lastAction = def.steps[def.steps.Length - 1]?.actionId;
                if (!string.IsNullOrWhiteSpace(lastAction) &&
                    string.Equals(lastAction, turnInActionId, StringComparison.Ordinal))
                {
                    turnedIn = true;
                }
            }

            // 저장 데이터가 퀘스트 정의보다 더 진행된 경우 안전하게 클램프
            if (def.steps != null)
                stepIndex = Mathf.Min(stepIndex, def.steps.Length);

            if (saved.completed && def.steps != null)
                stepIndex = def.steps.Length;

            RefreshHud();
        }

        private void SaveState()
        {
            if (!allowSaveWrites)
                return;

            if (def == null || string.IsNullOrWhiteSpace(def.id))
                return;

            var state = new QuestState
            {
                questId = def.id,
                stepIndex = stepIndex,
                stepCount = stepCount,
                coins = coins,
                completed = IsCompleted(),
                turnedIn = turnedIn
            };
            SaveServices.QuestSave.SaveQuestState(state);
        }

        [ContextMenu("Clear Saved Quest State")]
        private void ClearSaved()
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                return;
            SaveServices.QuestSave.ClearQuestState(def.id);
        }

        [ContextMenu("Dump Saved Quest State")]
        private void DumpSaved()
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id))
            {
                Debug.Log("[QuestManager] No quest loaded.");
                return;
            }

            SaveServices.QuestSave.LoadQuestStateAsync(def.id, saved =>
            {
                if (saved == null)
                {
                    Debug.Log($"[QuestManager] No saved state for quest '{def.id}'.");
                    return;
                }

                Debug.Log($"[QuestManager] Saved state: questId={saved.questId}, stepIndex={saved.stepIndex}, stepCount={saved.stepCount}, coins={saved.coins}, completed={saved.completed}");
            });
        }
    }
}

