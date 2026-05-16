using System;
using UnityEngine;

using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Core.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        private const string Prefix = QuestSaveKeys.StatePrefix;

        public void SaveQuestState(QuestState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.questId))
                return;

            var key = Key(state.questId);
            var json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public QuestState LoadQuestState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;

            var key = Key(questId);
            if (!PlayerPrefs.HasKey(key))
                return null;

            var json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<QuestState>(json);
        }

        public void LoadQuestStateAsync(string questId, Action<QuestState> onLoaded)
        {
            var s = LoadQuestState(questId);
            onLoaded?.Invoke(s);
        }

        public void ClearQuestState(string questId, Action onCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                onCompleted?.Invoke();
                return;
            }

            PlayerPrefs.DeleteKey(Key(questId));
            PlayerPrefs.Save();
            onCompleted?.Invoke();
        }

        private static string Key(string questId) => QuestSaveKeys.StateKey(questId);
    }
}

