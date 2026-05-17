using System;
using System.Collections.Generic;
using DataDrivenDemo.Quest;
using UnityEngine;

namespace DataDrivenDemo.Core.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public void SaveQuestState(QuestState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.questId))
                return;

            var json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(QuestSaveKeys.StateKey(state.questId), json);
            PlayerPrefs.Save();
        }

        public QuestState LoadQuestState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;

            var key = QuestSaveKeys.StateKey(questId);
            if (!PlayerPrefs.HasKey(key))
                return null;

            var json = PlayerPrefs.GetString(key, "");
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<QuestState>(json);
        }

        public void LoadQuestStateAsync(string questId, Action<QuestState> onLoaded)
        {
            onLoaded?.Invoke(LoadQuestState(questId));
        }

        public void ClearQuestState(string questId, Action onCompleted = null)
        {
            if (!string.IsNullOrWhiteSpace(questId))
            {
                PlayerPrefs.DeleteKey(QuestSaveKeys.StateKey(questId));
                PlayerPrefs.Save();
            }

            onCompleted?.Invoke();
        }

        public void LoadAcceptedQuestIdsAsync(Action<string[]> onLoaded)
        {
            onLoaded?.Invoke(ReadAcceptedFromPlayerPrefs());
        }

        public void SaveAcceptedQuestIdsAsync(string[] questIds, Action onCompleted = null)
        {
            WriteAcceptedToPlayerPrefs(questIds);
            onCompleted?.Invoke();
        }

        public void ProbeAnySavedProgressAsync(IReadOnlyList<string> questIds, Action<bool> onResult)
        {
            onResult?.Invoke(ProbeLocal(questIds));
        }

        internal static bool ProbeLocal(IReadOnlyList<string> questIds)
        {
            var accepted = ReadAcceptedFromPlayerPrefs();
            if (accepted.Length > 0)
                return true;

            if (questIds == null)
                return false;

            foreach (var id in questIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (PlayerPrefs.HasKey(QuestSaveKeys.StateKey(id)))
                {
                    var json = PlayerPrefs.GetString(QuestSaveKeys.StateKey(id), "");
                    if (!string.IsNullOrWhiteSpace(json))
                        return true;
                }
            }

            return false;
        }

        internal static string[] ReadAcceptedFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(QuestSaveKeys.AcceptedList))
                return Array.Empty<string>();

            var raw = PlayerPrefs.GetString(QuestSaveKeys.AcceptedList, "");
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw.Split('|');
        }

        internal static void WriteAcceptedToPlayerPrefs(string[] questIds)
        {
            var raw = questIds == null || questIds.Length == 0
                ? ""
                : string.Join("|", questIds);
            PlayerPrefs.SetString(QuestSaveKeys.AcceptedList, raw);
            PlayerPrefs.Save();
        }
    }
}
