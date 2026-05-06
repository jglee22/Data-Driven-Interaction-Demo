using UnityEngine;

namespace DataDrivenDemo.Core.Save
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        private const string Prefix = "ddidemo.quest.";

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

        public void ClearQuestState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            PlayerPrefs.DeleteKey(Key(questId));
            PlayerPrefs.Save();
        }

        private static string Key(string questId) => Prefix + questId;
    }
}

