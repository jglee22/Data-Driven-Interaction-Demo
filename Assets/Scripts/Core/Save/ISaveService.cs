using System;
using System.Collections.Generic;

namespace DataDrivenDemo.Core.Save
{
    /// <summary>
    /// 퀘스트 진행·수락 목록 저장소. 구현마다 동기/비동기 의미가 다르므로
    /// <see cref="LoadQuestState"/> / <see cref="LoadQuestStateAsync"/> 주석과 <see cref="IQuestSaveSyncSemantics"/>를 함께 확인합니다.
    /// </summary>
    public interface ISaveService
    {
        void SaveQuestState(QuestState state);

        /// <summary>
        /// 호출 스레드에서 즉시 반환. PlayerPrefs 등 로컬 구현은 본 저장소를 읽습니다.
        /// Firestore 등은 미러/캐시만 읽을 수 있으므로, 복원·최신 스냅샷에는 <see cref="LoadQuestStateAsync"/>를 사용합니다.
        /// </summary>
        QuestState LoadQuestState(string questId);

        /// <summary>
        /// 네트워크·비동기 I/O를 포함해 한 번 호출되면 콜백은 메인 스레드에서 정확히 한 번 실행됩니다.
        /// </summary>
        void LoadQuestStateAsync(string questId, Action<QuestState> onLoaded);
        /// <param name="onCompleted">Firestore 등 비동기 삭제까지 끝난 뒤 호출(메인 스레드).</param>
        void ClearQuestState(string questId, Action onCompleted = null);

        void LoadAcceptedQuestIdsAsync(Action<string[]> onLoaded);
        void SaveAcceptedQuestIdsAsync(string[] questIds, Action onCompleted = null);
        void ProbeAnySavedProgressAsync(IReadOnlyList<string> questIds, Action<bool> onResult);
    }
}
