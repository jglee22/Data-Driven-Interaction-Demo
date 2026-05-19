using System;

namespace DataDrivenDemo.Core.Save
{
    /// <summary>
    /// 데모용 전역 저장 구현 교체 지점. 런타임은 <see cref="QuestSystem"/>이 <see cref="QuestSaveChanged"/>로 최신 구현을 받습니다.
    /// 메인 메뉴·에디터 테스트 등 씬 밖 코드는 여전히 이 정적 프로퍼티를 사용할 수 있습니다.
    /// </summary>
    public static class SaveServices
    {
        private static ISaveService questSave = new PlayerPrefsSaveService();

        /// <summary>Firestore 등으로 구현이 바뀔 때 한 번 호출됩니다(메인 스레드).</summary>
        public static event Action<ISaveService> QuestSaveChanged;

        public static ISaveService QuestSave
        {
            get => questSave;
            set
            {
                var next = value ?? new PlayerPrefsSaveService();
                if (ReferenceEquals(questSave, next))
                    return;

                questSave = next;
                QuestSaveChanged?.Invoke(questSave);
            }
        }
    }
}
