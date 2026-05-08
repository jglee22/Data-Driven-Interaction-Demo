using System;
using System.Collections.Generic;
using System.Linq;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.UI;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 현업형 멀티 퀘스트 런타임: 수락/진행/완료(보고가능)/제출완료 상태를 관리하고,
    /// QuestTrackerService로 HUD/저널에 표시합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestSystem : MonoBehaviour
    {
        [SerializeField] private QuestCatalog catalog;
        [SerializeField] private QuestHudView hud;

        [Header("Behavior")]
        [SerializeField] private bool autoAcceptSavedQuests = true;
        [SerializeField] private bool autoSaveOnChange = true;

        private sealed class Runtime
        {
            public QuestDefinition def;
            public QuestState state;
        }

        private readonly Dictionary<string, Runtime> runtimes = new();
        private const string AcceptedKey = "ddidemo.quest.accepted";
        private readonly PlayerPrefsSaveService localFallbackSave = new();

        private void Awake()
        {
            if (catalog == null)
                catalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            if (hud == null)
                hud = FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);

            catalog?.Rebuild();

            LoadAcceptedList();

            // 수락 목록이 비어있는 "첫 실행/마이그레이션"에서만, 저장된 퀘스트를 자동 수락(옵션)
            if (autoAcceptSavedQuests && runtimes.Count == 0)
            {
                TryAutoAcceptSaved();
                SaveAcceptedList();
            }

            RefreshTrackerAll();
        }

        private void OnEnable()
        {
            QuestEvents.EventRaised += OnEventRaised;
        }

        private void OnDisable()
        {
            QuestEvents.EventRaised -= OnEventRaised;
        }

        public bool Accept(string questId)
        {
            var def = catalog != null ? catalog.Get(questId) : null;
            if (def == null) return false;

            if (runtimes.ContainsKey(def.id))
                return false;

            var saved = LoadStateWithFallback(def.id);
            var state = saved ?? new QuestState { questId = def.id };

            // 저장이 레거시인 경우에도 loader/postprocess가 steps/objectives를 맞춰줌
            runtimes[def.id] = new Runtime { def = def, state = state };

            UpsertTracker(def, state);
            RenderUi();
            SaveAcceptedList();
            return true;
        }

        public bool IsAccepted(string questId) => !string.IsNullOrWhiteSpace(questId) && runtimes.ContainsKey(questId);

        public bool Abandon(string questId, bool clearSavedState = true)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            if (!runtimes.Remove(questId))
                return false;

            QuestTrackerService.Remove(questId);

            if (clearSavedState)
            {
                SaveServices.QuestSave.ClearQuestState(questId);
                localFallbackSave.ClearQuestState(questId);
            }

            SaveAcceptedList();
            RenderUi();
            return true;
        }

        public void ResetAllQuests(bool clearSavedStates = true)
        {
            var ids = runtimes.Keys.ToList();
            runtimes.Clear();
            QuestTrackerService.ClearAll();
            RenderUi();

            if (!clearSavedStates)
                return;

            foreach (var id in ids)
                SaveServices.QuestSave.ClearQuestState(id);

            PlayerPrefs.DeleteKey(AcceptedKey);
            PlayerPrefs.Save();
        }

        private void LoadAcceptedList()
        {
            if (!PlayerPrefs.HasKey(AcceptedKey))
                return;

            var raw = PlayerPrefs.GetString(AcceptedKey, "");
            if (string.IsNullOrWhiteSpace(raw))
                return;

            var ids = raw.Split('|');
            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var def = catalog != null ? catalog.Get(id) : null;
                if (def == null)
                    continue;

                if (runtimes.ContainsKey(def.id))
                    continue;

                var saved = LoadStateWithFallback(def.id);
                var state = saved ?? new QuestState { questId = def.id };
                runtimes[def.id] = new Runtime { def = def, state = state };
            }
        }

        private void SaveAcceptedList()
        {
            var ids = runtimes.Keys.ToArray();
            var raw = string.Join("|", ids);
            PlayerPrefs.SetString(AcceptedKey, raw);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 프롬프트 필터링용: 이 (type,targetId) 상호작용이 현재 수락된 퀘스트들의 "다음 목표" 중 하나인가?
        /// </summary>
        public bool IsRelevantForPrompt(QuestEventType type, string targetId)
        {
            if (type == QuestEventType.Unknown || string.IsNullOrWhiteSpace(targetId))
                return false;

            foreach (var kv in runtimes.Values)
            {
                var def = kv?.def;
                var st = kv?.state;
                if (def?.steps == null || def.steps.Length == 0 || st == null)
                    continue;

                // 이미 완료(제출 대기 포함)면 프롬프트는 원칙적으로 숨김
                if (st.stepIndex >= def.steps.Length)
                    continue;

                var obj = GetPrimaryObjective(def.steps[st.stepIndex]);
                if (obj == null)
                    continue;

                // type/targetId 기반 우선
                if (!string.IsNullOrWhiteSpace(obj.type) && !string.IsNullOrWhiteSpace(obj.targetId))
                {
                    if (TryParseType(obj.type, out var t) && t == type &&
                        string.Equals(obj.targetId, targetId, StringComparison.Ordinal))
                        return true;
                }
                else
                {
                    // targetId만 있거나, 레거시 actionId만 있는 경우엔 event 매칭으로 폴백
                    if (MatchesObjective(obj, new QuestEvent(type, targetId, 1, actionId: "")))
                        return true;
                }
            }

            return false;
        }

        private void TryAutoAcceptSaved()
        {
            if (catalog == null) return;

            foreach (var def in catalog.All())
            {
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;
                var saved = LoadStateWithFallback(def.id);
                if (saved == null)
                    continue;
                if (runtimes.ContainsKey(def.id))
                    continue;
                runtimes[def.id] = new Runtime { def = def, state = saved };
            }
        }

        private void OnEventRaised(QuestEvent evt)
        {
            if (runtimes.Count == 0)
                return;

            var anyChanged = false;

            foreach (var kv in runtimes)
            {
                var rt = kv.Value;
                var def = rt?.def;
                var st = rt?.state;
                if (def?.steps == null || def.steps.Length == 0 || st == null)
                    continue;

                var completed = st.stepIndex >= def.steps.Length;

                // 완료 후 제출 이벤트로 "완료(제출됨)" 처리
                if (completed)
                {
                    if (!st.turnedIn && IsTurnInQuest(def) && MatchesObjective(GetLastObjective(def), evt))
                    {
                        st.turnedIn = true;
                        st.completed = true;
                        anyChanged = true;
                    }
                    continue;
                }

                var obj = GetPrimaryObjective(def.steps[st.stepIndex]);
                if (obj == null)
                    continue;

                if (!MatchesObjective(obj, evt))
                    continue;

                var required = Mathf.Max(1, obj.requiredCount);
                st.stepCount += Mathf.Max(1, evt.amount);

                if (st.stepCount < required)
                {
                    anyChanged = true;
                    continue;
                }

                st.stepIndex++;
                st.stepCount = 0;

                // 목표 완료 체크
                if (st.stepIndex >= def.steps.Length)
                {
                    st.completed = true;
                    if (IsTurnInQuest(def) && MatchesObjective(GetLastObjective(def), evt))
                        st.turnedIn = true;
                }

                anyChanged = true;
            }

            if (!anyChanged)
                return;

            if (autoSaveOnChange)
                SaveAllChanged();

            RefreshTrackerAll();
        }

        private void SaveAllChanged()
        {
            foreach (var kv in runtimes)
            {
                var st = kv.Value?.state;
                if (st == null || string.IsNullOrWhiteSpace(st.questId))
                    continue;
                SaveServices.QuestSave.SaveQuestState(st);
                if (SaveServices.QuestSave is not PlayerPrefsSaveService)
                    localFallbackSave.SaveQuestState(st);
            }
        }

        private QuestState LoadStateWithFallback(string questId)
        {
            var s = SaveServices.QuestSave.LoadQuestState(questId);
            if (s != null)
                return s;
            return localFallbackSave.LoadQuestState(questId);
        }

        private void RefreshTrackerAll()
        {
            QuestTrackerService.ClearAll();

            foreach (var kv in runtimes.Values)
            {
                if (kv?.def == null || kv.state == null)
                    continue;
                UpsertTracker(kv.def, kv.state);
            }

            RenderUi();
        }

        private void UpsertTracker(QuestDefinition def, QuestState st)
        {
            var status = ComputeStatus(def, st);
            var detail = BuildDetail(def, st);

            QuestTrackerService.Upsert(
                questId: def.id,
                title: def.title,
                detail: detail,
                status: status,
                sortKey: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
        }

        private void RenderUi()
        {
            hud?.RenderTracker(QuestTrackerService.Items);
            hud?.RefreshQuestJournalIfOpen();
        }

        private static QuestTrackerStatus ComputeStatus(QuestDefinition def, QuestState st)
        {
            if (def?.steps == null || def.steps.Length == 0 || st == null)
                return QuestTrackerStatus.Active;

            var done = st.stepIndex >= def.steps.Length;
            if (!done) return QuestTrackerStatus.Active;
            return st.turnedIn ? QuestTrackerStatus.Completed : QuestTrackerStatus.TurnInReady;
        }

        private static string BuildDetail(QuestDefinition def, QuestState st)
        {
            if (def?.steps == null || def.steps.Length == 0 || st == null)
                return "수락됨";

            if (st.stepIndex >= def.steps.Length)
                return st.turnedIn ? "완료" : "보고 가능";

            var obj = GetPrimaryObjective(def.steps[st.stepIndex]);
            var text = obj?.uiText;
            var required = Mathf.Max(1, obj != null ? obj.requiredCount : 1);

            if (string.IsNullOrWhiteSpace(text))
                text = "진행 중";

            if (required <= 1)
                return text;

            return $"{text} ({Mathf.Clamp(st.stepCount, 0, required)}/{required})";
        }

        private static bool IsTurnInQuest(QuestDefinition def)
        {
            var last = GetLastObjective(def);
            if (last == null) return false;

            if (!string.IsNullOrWhiteSpace(last.type))
                return string.Equals(last.type, "Submit", StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(last.actionId))
                return string.Equals(last.actionId, "submit_terminal", StringComparison.Ordinal);

            return false;
        }

        private static QuestObjective GetPrimaryObjective(QuestStep step)
        {
            if (step == null) return null;
            if (step.objectives != null && step.objectives.Length > 0)
                return step.objectives[0];

            // 레거시 보호
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

        private static QuestObjective GetLastObjective(QuestDefinition def)
        {
            if (def?.steps == null || def.steps.Length == 0) return null;
            return GetPrimaryObjective(def.steps[def.steps.Length - 1]);
        }

        private static bool MatchesObjective(QuestObjective obj, QuestEvent evt)
        {
            if (obj == null) return false;

            // 현업형: type + targetId
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

            if (!string.IsNullOrWhiteSpace(obj.targetId))
                return string.Equals(obj.targetId, evt.targetId, StringComparison.Ordinal);

            // 레거시: actionId
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
    }
}

