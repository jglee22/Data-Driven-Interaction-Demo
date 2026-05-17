namespace DataDrivenDemo.Quest
{
    /// <summary>퀘스트 진행/수락 목록용 PlayerPrefs 키.</summary>
    public static class QuestSaveKeys
    {
        public const string AcceptedList = "ddidemo.quest.accepted";
        public const string StatePrefix = "ddidemo.quest.";

        public static string StateKey(string questId) => StatePrefix + questId;
    }
}
