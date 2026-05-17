namespace DataDrivenDemo.Core.Save
{
    /// <summary>
    /// 데모용 전역 저장 구현 교체 지점. 씬 오브젝트 참조는 GameplaySceneContext를 사용합니다.
    /// </summary>
    public static class SaveServices
    {
        public static ISaveService QuestSave { get; set; } = new PlayerPrefsSaveService();
    }
}
