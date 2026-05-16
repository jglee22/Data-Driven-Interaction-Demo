using System;
using System.Collections.Generic;
using DataDrivenDemo.UI;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 테스트용: F1~F10으로 여러 퀘스트를 "수락"해 트래커/저널 UI를 검증합니다.
    /// (진행/완료는 QuestSystem이 담당)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestDebugAccepter : MonoBehaviour
    {
        [Header("Quest JSON Assets (size <= 5 recommended)")]
        [SerializeField] private TextAsset[] questJsons;

        [Header("Hotkeys")]
        [SerializeField] private bool enable = true;
        [Tooltip("의뢰 NPC UI만 쓸 때 끄면 F1~F5 수락 단축키만 비활성화됩니다. (F12 초기화는 따로 동작합니다.)")]
        [SerializeField] private bool acceptShortcutKeys = true;
        [SerializeField] private bool showToast = true;
        [SerializeField] private KeyCode[] acceptKeys =
        {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5
        };

        [SerializeField] private KeyCode clearAllKey = KeyCode.F12;

        [Header("Optional UI")]
        [SerializeField] private QuestHudView hud;

        [Header("Optional QuestSystem (recommended)")]
        [SerializeField] private QuestSystem questSystem;

        private sealed class Runtime
        {
            public QuestDefinition def;
            public int stepIndex;
            public int stepCount;
            public bool turnedIn;
        }

        private readonly Dictionary<string, Runtime> runtimes = new();

        private void Awake()
        {
            if (hud == null)
                hud = FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            QuestEvents.EventRaised += OnEventRaised;
        }

        private void OnDisable()
        {
            QuestEvents.EventRaised -= OnEventRaised;
        }

        private void Update()
        {
            if (!enable)
                return;

            if (acceptShortcutKeys)
            {
                for (var i = 0; i < acceptKeys.Length; i++)
                {
                    if (!Input.GetKeyDown(acceptKeys[i]))
                        continue;

                    AcceptByIndex(i);
                }
            }

            if (Input.GetKeyDown(clearAllKey))
                ClearAll();
        }

        private void OnEventRaised(QuestEvent evt)
        {
            // 정식 런타임(QuestSystem)이 있으면 진행 처리는 그쪽이 담당
            if (questSystem != null)
                return;

            if (!enable || runtimes.Count == 0)
                return;

            var anyChanged = false;
            foreach (var kv in runtimes)
            {
                var rt = kv.Value;
                if (rt?.def?.steps == null || rt.def.steps.Length == 0)
                    continue;

                var steps = rt.def.steps;
                var completed = rt.stepIndex >= steps.Length;

                // 완료 후 제출 이벤트가 들어오면 "완료(제출됨)" 처리(퀘스트가 제출을 요구하는 경우만)
                if (completed)
                {
                    if (!rt.turnedIn && IsTurnInLastStep(steps) && MatchesObjective(GetLastObjective(steps), evt))
                    {
                        rt.turnedIn = true;
                        anyChanged = true;
                    }
                    continue;
                }

                var step = steps[rt.stepIndex];
                var obj = GetPrimaryObjective(step);
                if (obj == null)
                    continue;

                if (!MatchesObjective(obj, evt))
                    continue;

                var required = Mathf.Max(1, obj.requiredCount);
                rt.stepCount += Mathf.Max(1, evt.amount);

                if (rt.stepCount < required)
                {
                    anyChanged = true;
                    continue;
                }

                rt.stepIndex++;
                rt.stepCount = 0;

                // 마지막 스텝이 제출이면, 스텝 수행과 동시에 제출됨 처리
                if (rt.stepIndex >= steps.Length && IsTurnInLastStep(steps) && MatchesObjective(GetLastObjective(steps), evt))
                    rt.turnedIn = true;

                anyChanged = true;
            }

            if (anyChanged)
                RefreshTrackerFromRuntimes();
        }

        private void AcceptByIndex(int index)
        {
            if (questJsons == null || index < 0 || index >= questJsons.Length)
                return;

            var asset = questJsons[index];
            var def = QuestDefinitionLoader.FromTextAsset(asset);
            if (def == null || string.IsNullOrWhiteSpace(def.id))
                return;

            if (questSystem != null)
            {
                if (!questSystem.Accept(def.id))
                {
                    if (showToast) hud?.ShowToast($"이미 수락됨: {def.title}");
                    return;
                }
                if (showToast) hud?.ShowToast($"수락: {def.title}");
                return;
            }

            if (runtimes.ContainsKey(def.id))
            {
                if (showToast) hud?.ShowToast($"이미 수락됨: {def.title}");
                return;
            }

            runtimes[def.id] = new Runtime
            {
                def = def,
                stepIndex = 0,
                stepCount = 0,
                turnedIn = false
            };

            QuestTrackerService.Upsert(
                questId: def.id,
                title: def.title,
                detail: BuildDetail(def, 0, 0, turnedIn: false),
                status: QuestTrackerStatus.Active,
                sortKey: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );

            hud?.RenderTracker(QuestTrackerService.Items);
            hud?.RefreshQuestJournalIfOpen();

            if (showToast)
                hud?.ShowToast($"수락: {def.title}");
        }

        private void RefreshTrackerFromRuntimes()
        {
            foreach (var kv in runtimes)
            {
                var questId = kv.Key;
                var rt = kv.Value;
                var def = rt?.def;
                if (def == null) continue;

                var status = ComputeStatus(def, rt.stepIndex, rt.turnedIn);
                var detail = BuildDetail(def, rt.stepIndex, rt.stepCount, rt.turnedIn);

                QuestTrackerService.Upsert(
                    questId: questId,
                    title: def.title,
                    detail: detail,
                    status: status,
                    sortKey: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                );
            }

            hud?.RenderTracker(QuestTrackerService.Items);
            hud?.RefreshQuestJournalIfOpen();
        }

        private static QuestTrackerStatus ComputeStatus(QuestDefinition def, int stepIndex, bool turnedIn)
        {
            var hasSteps = def?.steps != null && def.steps.Length > 0;
            if (!hasSteps) return QuestTrackerStatus.Active;
            if (stepIndex < def.steps.Length) return QuestTrackerStatus.Active;
            return turnedIn ? QuestTrackerStatus.Completed : QuestTrackerStatus.TurnInReady;
        }

        private static bool IsTurnInLastStep(QuestStep[] steps)
        {
            if (steps == null || steps.Length == 0) return false;
            var obj = GetLastObjective(steps);
            if (obj == null) return false;
            if (!string.IsNullOrWhiteSpace(obj.actionId))
                return string.Equals(obj.actionId, "submit_terminal", StringComparison.Ordinal);
            if (!string.IsNullOrWhiteSpace(obj.type))
                return string.Equals(obj.type, "Submit", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private static string BuildDetail(QuestDefinition def, int stepIndex, int stepCount, bool turnedIn)
        {
            if (def?.steps == null || def.steps.Length == 0)
                return "수락됨";

            if (stepIndex >= def.steps.Length)
                return turnedIn ? "완료" : "보고 가능";

            var step = def.steps[stepIndex];
            var o = GetPrimaryObjective(step);
            var obj = o?.uiText;
            var required = Mathf.Max(1, o != null ? o.requiredCount : 1);

            if (string.IsNullOrWhiteSpace(obj))
                return required > 1 ? $"진행 중 ({Mathf.Clamp(stepCount, 0, required)}/{required})" : "진행 중";

            if (required <= 1)
                return obj;

            return $"{obj} ({Mathf.Clamp(stepCount, 0, required)}/{required})";
        }

        private static QuestObjective GetPrimaryObjective(QuestStep step)
        {
            if (step == null) return null;
            if (step.objectives != null && step.objectives.Length > 0)
                return step.objectives[0];

            // 레거시 데이터 보호(혹시 loader postprocess 이전에 접근되는 경우)
            if (!string.IsNullOrWhiteSpace(step.actionId) || !string.IsNullOrWhiteSpace(step.objective))
            {
                return new QuestObjective
                {
                    actionId = step.actionId ?? "",
                    uiText = step.objective ?? "",
                    requiredCount = step.requiredCount
                };
            }

            return null;
        }

        private static QuestObjective GetLastObjective(QuestStep[] steps)
        {
            if (steps == null || steps.Length == 0) return null;
            return GetPrimaryObjective(steps[steps.Length - 1]);
        }

        private static bool MatchesObjective(QuestObjective obj, QuestEvent evt)
        {
            if (obj == null) return false;

            // 1) type + targetId 매칭(현업형) 우선
            if (!string.IsNullOrWhiteSpace(obj.type))
            {
                if (!TryParseType(obj.type, out var t) || t == QuestEventType.Unknown)
                    return false;
                if (evt.type != t)
                    return false;

                if (string.IsNullOrWhiteSpace(obj.targetId))
                    return true;
                return string.Equals(obj.targetId, evt.targetId, StringComparison.Ordinal);
            }

            // 2) targetId만 있는 경우
            if (!string.IsNullOrWhiteSpace(obj.targetId))
                return string.Equals(obj.targetId, evt.targetId, StringComparison.Ordinal);

            // 3) actionId 매칭(레거시/간편) 마지막
            if (!string.IsNullOrWhiteSpace(obj.actionId) && !string.IsNullOrWhiteSpace(evt.actionId))
                return string.Equals(obj.actionId, evt.actionId, StringComparison.Ordinal);

            return false;
        }

        private static bool TryParseType(string s, out QuestEventType t)
        {
            t = QuestEventType.Unknown;
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (string.Equals(s, "Talk", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.Talk; return true; }
            if (string.Equals(s, "Pickup", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.Pickup; return true; }
            if (string.Equals(s, "Submit", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.Submit; return true; }
            if (string.Equals(s, "Kill", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.Kill; return true; }
            if (string.Equals(s, "EnterArea", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.EnterArea; return true; }
            if (string.Equals(s, "Use", StringComparison.OrdinalIgnoreCase)) { t = QuestEventType.Use; return true; }
            return false;
        }

        private void ClearAll()
        {
            if (questSystem != null)
            {
                questSystem.ResetAllQuests(clearSavedStates: true);
                if (showToast) hud?.ShowToast("퀘스트 리셋");
                return;
            }

            runtimes.Clear();
            QuestTrackerService.ClearAll();
            hud?.RenderTracker(QuestTrackerService.Items);
            hud?.RefreshQuestJournalIfOpen();
            if (showToast) hud?.ShowToast("퀘스트 목록 초기화");
        }
    }
}

