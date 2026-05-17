using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.Quest;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

namespace DataDrivenDemo.Firebase
{
    public sealed class FirestoreQuestSaveService : ISaveService
    {
        private const string JsonField = "json";
        private const string AcceptedField = "accepted";
        /// <summary>
        /// 익명 UID가 세션마다 바뀌면 Firestore 경로가 비어 보이므로, 에디터/단일기기에서는 로컬 미러를 둡니다.
        /// </summary>
        private const string MirrorPrefix = "ddidemo.quest.fsbackup.";
        private const string MirrorAcceptedKey = MirrorPrefix + "__accepted__";

        private readonly FirebaseBootstrap bootstrap;
        private readonly FirebaseFirestore db;

        public FirestoreQuestSaveService(FirebaseBootstrap bootstrap)
        {
            this.bootstrap = bootstrap;
            db = FirebaseFirestore.DefaultInstance;
        }

        public void SaveQuestState(QuestState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.questId))
                return;

            if (!IsReady(out _))
            {
                void Handler(string _)
                {
                    bootstrap.SignedIn -= Handler;
                    SaveQuestState(state);
                }

                if (bootstrap == null)
                {
                    Debug.LogWarning("[FirestoreQuestSaveService] Save skipped: no FirebaseBootstrap.");
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
                return;

            var json = JsonUtility.ToJson(state);
            var payload = new Dictionary<string, object> { { JsonField, json } };

            Doc(uid, state.questId)
                .SetAsync(payload)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        var ex = task.Exception != null ? task.Exception.InnerException ?? task.Exception : null;
                        Debug.LogWarning($"[FirestoreQuestSaveService] Save failed.{(ex != null ? " " + ex.Message : "")} Rules? Auth?");
                        return;
                    }

                    WriteMirror(state);
                });
        }

        public QuestState LoadQuestState(string questId) => ReadMirror(questId);

        public void LoadQuestStateAsync(string questId, Action<QuestState> onLoaded)
        {
            void Done(QuestState s) => onLoaded?.Invoke(s);

            if (string.IsNullOrWhiteSpace(questId))
            {
                Done(null);
                return;
            }

            if (!IsReady(out _))
            {
                void Handler(string _)
                {
                    bootstrap.SignedIn -= Handler;
                    LoadQuestStateAsync(questId, onLoaded);
                }

                if (bootstrap == null)
                {
                    Done(ReadMirror(questId) ?? new PlayerPrefsSaveService().LoadQuestState(questId));
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
            {
                Done(ReadMirror(questId));
                return;
            }

            Doc(uid, questId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        var ex = task.Exception != null ? task.Exception.InnerException ?? task.Exception : null;
                        Debug.LogWarning($"[FirestoreQuestSaveService] Load failed.{(ex != null ? " " + ex.Message : "")}");
                        Done(ReadMirror(questId));
                        return;
                    }

                    var snap = task.Result;
                    if (snap == null || !snap.Exists || !snap.ContainsField(JsonField))
                    {
                        Done(ReadMirror(questId));
                        return;
                    }

                    try
                    {
                        var json = snap.GetValue<string>(JsonField);
                        var parsed = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<QuestState>(json);
                        if (parsed != null)
                            WriteMirror(parsed);
                        Done(parsed ?? ReadMirror(questId));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FirestoreQuestSaveService] Parse failed: {ex.Message}");
                        Done(ReadMirror(questId));
                    }
                });
        }

        public void ClearQuestState(string questId, Action onCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                onCompleted?.Invoke();
                return;
            }

            if (!IsReady(out _))
            {
                void Handler(string _)
                {
                    bootstrap.SignedIn -= Handler;
                    ClearQuestState(questId, onCompleted);
                }

                if (bootstrap == null)
                {
                    ClearMirror(questId);
                    onCompleted?.Invoke();
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
            {
                ClearMirror(questId);
                onCompleted?.Invoke();
                return;
            }

            Doc(uid, questId)
                .DeleteAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogWarning("[FirestoreQuestSaveService] Clear failed.");
                    ClearMirror(questId);
                    onCompleted?.Invoke();
                });
        }

        public void LoadAcceptedQuestIdsAsync(Action<string[]> onLoaded)
        {
            void Done(string[] ids) => onLoaded?.Invoke(ids ?? Array.Empty<string>());

            var local = MergeAcceptedSources(
                PlayerPrefsSaveService.ReadAcceptedFromPlayerPrefs(),
                ReadAcceptedMirror());

            if (!IsReady(out _))
            {
                void Handler(string _)
                {
                    bootstrap.SignedIn -= Handler;
                    LoadAcceptedQuestIdsAsync(onLoaded);
                }

                if (bootstrap == null)
                {
                    Done(local);
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
            {
                Done(local);
                return;
            }

            MetaDoc(uid)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
                {
                    if (task.IsFaulted || task.IsCanceled || task.Result == null || !task.Result.Exists ||
                        !task.Result.ContainsField(AcceptedField))
                    {
                        Done(local);
                        return;
                    }

                    try
                    {
                        var raw = task.Result.GetValue<string>(AcceptedField);
                        var cloud = ParseAcceptedRaw(raw);
                        var merged = MergeAcceptedSources(local, cloud);
                        WriteAcceptedMirrors(merged);
                        Done(merged);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FirestoreQuestSaveService] Accepted load failed: {ex.Message}");
                        Done(local);
                    }
                });
        }

        public void SaveAcceptedQuestIdsAsync(string[] questIds, Action onCompleted = null)
        {
            var ids = questIds ?? Array.Empty<string>();
            WriteAcceptedMirrors(ids);

            if (!IsReady(out _))
            {
                void Handler(string _)
                {
                    bootstrap.SignedIn -= Handler;
                    SaveAcceptedQuestIdsAsync(questIds, onCompleted);
                }

                if (bootstrap == null)
                {
                    onCompleted?.Invoke();
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
            {
                onCompleted?.Invoke();
                return;
            }

            var raw = ids.Length == 0 ? "" : string.Join("|", ids);
            MetaDoc(uid)
                .SetAsync(new Dictionary<string, object> { { AcceptedField, raw } })
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogWarning("[FirestoreQuestSaveService] Accepted save failed.");
                    onCompleted?.Invoke();
                });
        }

        public void ProbeAnySavedProgressAsync(IReadOnlyList<string> questIds, Action<bool> onResult)
        {
            if (PlayerPrefsSaveService.ProbeLocal(questIds))
            {
                onResult?.Invoke(true);
                return;
            }

            LoadAcceptedQuestIdsAsync(accepted =>
            {
                if (accepted != null && accepted.Length > 0)
                {
                    onResult?.Invoke(true);
                    return;
                }

                ProbeQuestDocuments(questIds, 0, onResult);
            });
        }

        private void ProbeQuestDocuments(IReadOnlyList<string> questIds, int index, Action<bool> onResult)
        {
            if (questIds == null || index >= questIds.Count)
            {
                onResult?.Invoke(false);
                return;
            }

            var id = questIds[index];
            if (string.IsNullOrWhiteSpace(id))
            {
                ProbeQuestDocuments(questIds, index + 1, onResult);
                return;
            }

            if (ReadMirror(id) != null)
            {
                onResult?.Invoke(true);
                return;
            }

            LoadQuestStateAsync(id, state =>
            {
                if (state != null)
                {
                    onResult?.Invoke(true);
                    return;
                }

                ProbeQuestDocuments(questIds, index + 1, onResult);
            });
        }

        private static string MirrorKey(string questId) => MirrorPrefix + questId;

        private static void WriteMirror(QuestState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.questId))
                return;

            PlayerPrefs.SetString(MirrorKey(state.questId), JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        private static QuestState ReadMirror(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;

            var key = MirrorKey(questId);
            if (!PlayerPrefs.HasKey(key))
                return null;

            var json = PlayerPrefs.GetString(key, "");
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<QuestState>(json);
        }

        private static void ClearMirror(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            PlayerPrefs.DeleteKey(MirrorKey(questId));
            PlayerPrefs.Save();
        }

        private static string[] ReadAcceptedMirror()
        {
            if (!PlayerPrefs.HasKey(MirrorAcceptedKey))
                return Array.Empty<string>();

            return ParseAcceptedRaw(PlayerPrefs.GetString(MirrorAcceptedKey, ""));
        }

        private static void WriteAcceptedMirrors(string[] ids)
        {
            var raw = ids == null || ids.Length == 0 ? "" : string.Join("|", ids);
            PlayerPrefs.SetString(MirrorAcceptedKey, raw);
            PlayerPrefs.Save();
            PlayerPrefsSaveService.WriteAcceptedToPlayerPrefs(ids);
        }

        private static string[] ParseAcceptedRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw.Split('|');
        }

        private static string[] MergeAcceptedSources(params string[][] sources)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (sources == null)
                return Array.Empty<string>();

            foreach (var arr in sources)
            {
                if (arr == null)
                    continue;
                foreach (var id in arr)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        set.Add(id.Trim());
                }
            }

            return set.Count == 0 ? Array.Empty<string>() : set.ToArray();
        }

        private DocumentReference Doc(string uid, string questId) =>
            db.Collection("users").Document(uid).Collection("quests").Document(questId);

        private DocumentReference MetaDoc(string uid) =>
            db.Collection("users").Document(uid).Collection("saves").Document("quest_meta");

        private bool IsReady(out string uid)
        {
            uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (!string.IsNullOrWhiteSpace(uid))
                return true;

            uid = bootstrap != null ? bootstrap.UserId : null;
            return bootstrap != null && bootstrap.IsReady && !string.IsNullOrWhiteSpace(uid);
        }
    }
}
