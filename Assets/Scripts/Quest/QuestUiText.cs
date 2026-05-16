namespace DataDrivenDemo.Quest
{
    internal static class QuestUiText
    {
        public static string FormatRewardLine(int coins) =>
            coins > 0 ? $"보상: 코인 +{coins}" : "보상: 없음";

        public static string FormatRewardBlock(int coins) =>
            coins > 0 ? $"코인 +{coins}" : "없음";
    }
}
