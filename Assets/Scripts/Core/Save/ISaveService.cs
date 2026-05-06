namespace DataDrivenDemo.Core.Save
{
    public interface ISaveService
    {
        void SaveQuestState(QuestState state);
        QuestState LoadQuestState(string questId);
        void ClearQuestState(string questId);
    }
}

