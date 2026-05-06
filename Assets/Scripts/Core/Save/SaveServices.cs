namespace DataDrivenDemo.Core.Save
{
    /// <summary>
    /// 데모 단계에서 DI 컨테이너 없이도 저장 서비스를 교체할 수 있도록 최소한의 접근점을 둡니다.
    /// Firebase로 갈아끼울 때 여기만 바꿔도 됩니다.
    /// </summary>
    public static class SaveServices
    {
        public static ISaveService QuestSave { get; set; } = new PlayerPrefsSaveService();
    }
}

