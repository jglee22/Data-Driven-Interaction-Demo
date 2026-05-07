using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using DataDrivenDemo.Core.Save;

namespace DataDrivenDemo.Firebase
{
    public sealed class FirestoreQuestSaveService : ISaveService
    {
        private const string JsonField = "json";
        /// <summary>
        /// 익명 UID가 세션마다 바뀌면 Firestore 경로가 비어 보이므로, 에디터/단일기기에서는 로컬 미러를 둡니다.
        /// </summary>
        private const string MirrorPrefix = "ddidemo.quest.fsbackup.";

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

        public QuestState LoadQuestState(string questId)
        {
            return null;
        }

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
                    Done(null);
                    return;
                }

                bootstrap.SignedIn += Handler;
                return;
            }

            if (!IsReady(out var uid))
            {
                Done(null);
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

        private DocumentReference Doc(string uid, string questId) =>
            db.Collection("users").Document(uid).Collection("quests").Document(questId);

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
