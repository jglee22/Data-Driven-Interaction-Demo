using System;

namespace DataDrivenDemo.Core.Save
{
    public interface ISaveService
    {
        void SaveQuestState(QuestState state);
        QuestState LoadQuestState(string questId);
        void LoadQuestStateAsync(string questId, Action<QuestState> onLoaded);
        /// <param name="onCompleted">Firestore 등 비동기 삭제까지 끝난 뒤 호출(메인 스레드).</param>
        void ClearQuestState(string questId, Action onCompleted = null);
    }
}


