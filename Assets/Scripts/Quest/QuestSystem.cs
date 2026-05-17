using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataDrivenDemo.Core;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.Firebase;
using DataDrivenDemo.Interaction;
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
        [SerializeField] private QuestObjectiveWorldMarkerManager[] worldMarkerManagers;

        [Header("Behavior")]
        [SerializeField] private bool autoAcceptSavedQuests = true;
        [SerializeField] private bool autoSaveOnChange = true;

        private sealed class Runtime
        {
            public QuestDefinition def;
            public QuestState state;
        }

        private readonly Dictionary<string, Runtime> runtimes = new();
        private readonly PlayerPrefsSaveService localFallbackSave = new();

        /// <summary>Firestore/로컬 저장 복원이 끝난 뒤 true.</summary>
        public bool IsHydrated { get; private set; }

        private void Awake()
        {
            if (catalog == null)
                catalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            if (hud == null)
                hud = FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);

            CacheWorldMarkerManagers();
            catalog?.Rebuild();
        }

        private void CacheWorldMarkerManagers()
        {
            if (worldMarkerManagers != null && worldMarkerManagers.Length > 0)
                return;

            worldMarkerManagers = FindObjectsByType<QuestObjectiveWorldMarkerManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            QuestEvents.EventRaised += OnEventRaised;
        }

        private void OnDisable()
        {
            QuestEvents.EventRaised -= OnEventRaised;
        }

        private void Start()
        {
            StartCoroutine(CoHydrateFromSave());
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

        /// <summary>의뢰 NPC 목록 패널용 상태 문구입니다.</summary>
        public string GetOfferStatusLabel(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return "";
            if (!IsAccepted(questId))
                return "미수락";
            if (!runtimes.TryGetValue(questId, out var rt) || rt?.def == null || rt.state == null)
                return "미수락";

            return ComputeStatus(rt.def, rt.state) switch
            {
                QuestTrackerStatus.Active => "진행 중",
                QuestTrackerStatus.TurnInReady => "보고 가능",
                QuestTrackerStatus.Completed => "완료",
                _ => "진행 중"
            };
        }

        /// <summary>의뢰 NPC에서 아직 수락하지 않은 퀘스트만 수락 가능합니다.</summary>
        public bool CanAcceptOffer(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return false;
            if (catalog == null || catalog.Get(questId) == null)
                return false;
            return !IsAccepted(questId);
        }

        /// <summary>월드 마커(!): 이 기버가 걸어 둔 의뢰 중 아직 수락할 수 있는 것이 있는지.</summary>
        public bool GiverHasAnyAcceptableOffer(QuestGiverInteractable giver)
        {
            if (giver == null || catalog == null)
                return false;

            var ids = giver.OfferedQuestIds;
            if (ids.Length == 0)
            {
                foreach (var def in catalog.All())
                {
                    if (def == null || string.IsNullOrWhiteSpace(def.id))
                        continue;
                    if (CanAcceptOffer(def.id))
                        return true;
                }

                return false;
            }

            foreach (var id in ids)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (CanAcceptOffer(id))
                    return true;
            }

            return false;
        }

        /// <summary>미수락이면 카탈로그 기준, 수락 후에는 저널과 동일한 요약을 만듭니다.</summary>
        public bool TryGetOfferDetailText(string questId, out string body)
        {
            body = "";
            if (string.IsNullOrWhiteSpace(questId) || catalog == null)
                return false;

            var def = catalog.Get(questId);
            if (def == null)
                return false;

            if (!IsAccepted(questId))
            {
                var lines = new System.Text.StringBuilder();
                if (def.steps != null && def.steps.Length > 0)
                {
                    var o = GetPrimaryObjective(def.steps[0]);
                    if (!string.IsNullOrWhiteSpace(o?.uiText))
                        lines.AppendLine(o.uiText);
                }
                var coins = def.reward != null ? def.reward.coins : 0;
                lines.AppendLine(QuestUiText.FormatRewardLine(coins));
                body = lines.ToString().TrimEnd();
                return true;
            }

            return TryGetJournalDetail(questId, out _, out body);
        }

        public bool TryGetJournalDetail(string questId, out string title, out string body)
        {
            title = "";
            body = "";

            if (string.IsNullOrWhiteSpace(questId))
                return false;

            if (!runtimes.TryGetValue(questId, out var rt) || rt?.def == null || rt.state == null)
                return false;

            var def = rt.def;
            var st = rt.state;

            title = def.title ?? questId;

            var status = ComputeStatus(def, st) switch
            {
                QuestTrackerStatus.Active => "진행 중",
                QuestTrackerStatus.TurnInReady => "보고 가능",
                QuestTrackerStatus.Completed => "완료",
                _ => "진행 중"
            };

            string objective;
            string progress;

            if (def.steps == null || def.steps.Length == 0)
            {
                objective = "퀘스트 데이터가 없습니다.";
                progress = "-";
            }
            else if (st.stepIndex >= def.steps.Length)
            {
                objective = st.turnedIn ? "완료" : "보고";
                progress = $"{def.steps.Length} / {def.steps.Length}";
            }
            else
            {
                var obj = GetPrimaryObjective(def.steps[st.stepIndex]);
                var text = obj?.uiText;
                if (string.IsNullOrWhiteSpace(text))
                    text = "진행";

                var required = Mathf.Max(1, obj != null ? obj.requiredCount : 1);
                var cur = Mathf.Clamp(st.stepCount, 0, required);
                objective = text;
                progress = required <= 1 ? $"{st.stepIndex + 1} / {def.steps.Length}" : $"{cur} / {required}";
            }

            var rewardCoins = def.reward != null ? def.reward.coins : 0;

            body =
                $"목표\n{objective}\n\n" +
                $"진행도\n{progress}\n\n" +
                $"상태\n{status}\n\n" +
                $"보상\n{QuestUiText.FormatRewardBlock(rewardCoins)}";

            return true;
        }

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
            // 리셋 직후 runtimes.Keys 가 비면 저장소 삭제가 한 건도 안 돌아가고,
            // 다음 플레이 Awake 에서 TryAutoAcceptSaved 가 남아 있는 진행 저장만 보고 자동 수락되는 버그가 난다.
            catalog ??= FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
            catalog?.Rebuild();

            var clearIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in runtimes.Keys)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    clearIds.Add(id);
            }

            if (catalog != null)
            {
                foreach (var def in catalog.All())
                {
                    if (def != null && !string.IsNullOrWhiteSpace(def.id))
                        clearIds.Add(def.id);
                }
            }

            runtimes.Clear();
            QuestTrackerService.ClearAll();
            RenderUi();

            if (!clearSavedStates)
            {
                SaveAcceptedList();
                return;
            }

            var remaining = clearIds.Count;
            if (remaining == 0)
            {
                SaveServices.QuestSave.SaveAcceptedQuestIdsAsync(Array.Empty<string>());
                return;
            }

            foreach (var id in clearIds)
            {
                SaveServices.QuestSave.ClearQuestState(id, () =>
                {
                    localFallbackSave.ClearQuestState(id);
                    remaining--;
                    if (remaining > 0)
                        return;

                    SaveServices.QuestSave.SaveAcceptedQuestIdsAsync(Array.Empty<string>());
                });
            }
        }

        private IEnumerator CoHydrateFromSave()
        {
            yield return CoWaitForSaveServiceReady();

            string[] accepted = null;
            yield return CoLoadAcceptedIds(ids => accepted = ids);

            if (accepted != null)
            {
                foreach (var id in accepted)
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    yield return CoHydrateQuestId(id.Trim());
                }
            }

            if (autoAcceptSavedQuests && runtimes.Count == 0 && catalog != null)
            {
                foreach (var def in catalog.All())
                {
                    if (def == null || string.IsNullOrWhiteSpace(def.id))
                        continue;
                    if (runtimes.ContainsKey(def.id))
                        continue;

                    QuestState saved = null;
                    yield return CoLoadQuestState(def.id, s => saved = s);
                    if (saved == null)
                        continue;

                    HydrateRuntime(def.id, saved);
                }
            }

            SaveAcceptedList();
            IsHydrated = true;
            RefreshTrackerAll();
        }

        private IEnumerator CoWaitForSaveServiceReady()
        {
            var installer = FindFirstObjectByType<FirebaseSaveInstaller>(FindObjectsInactive.Include);
            if (installer == null)
                yield break;

            var waitedInstall = 0f;
            const float installerTimeout = 5f;
            while (SaveServices.QuestSave is PlayerPrefsSaveService && waitedInstall < installerTimeout)
            {
                waitedInstall += Time.unscaledDeltaTime;
                yield return null;
            }

            var bootstrap = FindFirstObjectByType<FirebaseBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
                yield break;

            var waitedAuth = 0f;
            const float authTimeout = 30f;
            while (!bootstrap.IsReady && waitedAuth < authTimeout)
            {
                waitedAuth += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator CoLoadAcceptedIds(Action<string[]> onLoaded)
        {
            var done = false;
            string[] result = Array.Empty<string>();
            SaveServices.QuestSave.LoadAcceptedQuestIdsAsync(ids =>
            {
                result = ids ?? Array.Empty<string>();
                done = true;
            });

            while (!done)
                yield return null;

            onLoaded?.Invoke(result);
        }

        private IEnumerator CoLoadQuestState(string questId, Action<QuestState> onLoaded)
        {
            var done = false;
            QuestState state = null;
            SaveServices.QuestSave.LoadQuestStateAsync(questId, s =>
            {
                state = s;
                done = true;
            });

            while (!done)
                yield return null;

            if (state == null)
                state = localFallbackSave.LoadQuestState(questId);

            onLoaded?.Invoke(state);
        }

        private IEnumerator CoHydrateQuestId(string questId)
        {
            var def = catalog != null ? catalog.Get(questId) : null;
            if (def == null)
                yield break;

            if (runtimes.ContainsKey(def.id))
                yield break;

            QuestState saved = null;
            yield return CoLoadQuestState(def.id, s => saved = s);
            HydrateRuntime(def.id, saved);
        }

        private void HydrateRuntime(string questId, QuestState saved)
        {
            var def = catalog != null ? catalog.Get(questId) : null;
            if (def == null)
                return;

            var state = saved ?? new QuestState { questId = def.id };
            runtimes[def.id] = new Runtime { def = def, state = state };
        }

        private void SaveAcceptedList()
        {
            var ids = runtimes.Keys.ToArray();
            SaveServices.QuestSave.SaveAcceptedQuestIdsAsync(ids);
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

        private void OnEventRaised(QuestEvent evt)
        {
            if (!IsHydrated || runtimes.Count == 0)
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

            var ctx = GameplaySceneContext.Instance;
            if (ctx != null)
            {
                ctx.RefreshWorldMarkers();
                return;
            }

            if (worldMarkerManagers == null || worldMarkerManagers.Length == 0)
                CacheWorldMarkerManagers();

            if (worldMarkerManagers == null)
                return;

            foreach (var m in worldMarkerManagers)
            {
                if (m != null && m.isActiveAndEnabled)
                    m.RefreshFrom(this);
            }
        }

        /// <summary>월드 마커용: 수락된 퀘스트의 현재 목표(또는 보고 대기) 오브젝트 Transform.</summary>
        public void GetWorldMarkerObjectiveAnchors(HashSet<Transform> anchors)
        {
            if (anchors == null)
                return;
            anchors.Clear();

            foreach (var kv in runtimes)
            {
                var rt = kv.Value;
                var def = rt?.def;
                var st = rt?.state;
                if (def?.steps == null || st == null)
                    continue;

                QuestObjective obj = null;
                if (st.stepIndex < def.steps.Length)
                    obj = GetPrimaryObjective(def.steps[st.stepIndex]);
                else if (!st.turnedIn && IsTurnInQuest(def))
                    obj = GetLastObjective(def);

                if (obj == null || string.IsNullOrWhiteSpace(obj.targetId))
                    continue;

                if (TryFindInteractableByTargetId(obj.targetId.Trim(), out var it))
                    anchors.Add(it.transform);
            }
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

        /// <summary>
        /// 월드 마커용: targetId와 Id가 같은 상호작용 오브젝트를 찾습니다.
        /// 의뢰 NPC(<see cref="QuestGiverInteractable"/>)는 Id가 목표와 같아도 진행 목표 앵커로는 쓰지 않고,
        /// Npc/Item/Terminal 등이 없을 때만 폴백합니다(같은 Id가 여러 컴포넌트에 있을 때 진행 마커가 사라지는 문제 방지).
        /// </summary>
        private static bool TryFindInteractableByTargetId(string targetId, out InteractableBase found)
        {
            found = null;
            if (string.IsNullOrWhiteSpace(targetId))
                return false;

            var key = targetId.Trim();
            InteractableBase questGiverFallback = null;

            foreach (var b in UnityEngine.Object.FindObjectsByType<InteractableBase>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (b == null)
                    continue;
                if (!string.Equals(b.Id, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (b is QuestGiverInteractable)
                {
                    if (questGiverFallback == null)
                        questGiverFallback = b;
                    continue;
                }

                found = b;
                return true;
            }

            if (questGiverFallback != null)
            {
                found = questGiverFallback;
                return true;
            }

            return false;
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

