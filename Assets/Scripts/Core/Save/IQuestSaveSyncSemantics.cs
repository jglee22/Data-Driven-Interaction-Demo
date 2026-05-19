namespace DataDrivenDemo.Core.Save
{
    /// <summary>
    /// <see cref="ISaveService.LoadQuestState"/>가 어떤 저장소를 읽는지 표시합니다.
    /// Firestore 구현은 동기 경로가 미러일 수 있어, 복원·권위 데이터는 <see cref="ISaveService.LoadQuestStateAsync"/>를 쓰는 것이 안전합니다.
    /// </summary>
    public interface IQuestSaveSyncSemantics
    {
        /// <summary>
        /// true: 동기 Load가 주 저장소(PlayerPrefs 등)를 직접 읽는다.
        /// false: 동기 Load는 캐시/미러 등 보조 소스만 읽을 수 있다(클라우드 원문은 비동기).
        /// </summary>
        bool SyncLoadReadsPrimaryBackingStore { get; }
    }
}
