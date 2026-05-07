namespace DataDrivenDemo.Core.Save
{
    public static class SaveServices
    {
        public static ISaveService QuestSave { get; set; } = new PlayerPrefsSaveService();
    }
}

