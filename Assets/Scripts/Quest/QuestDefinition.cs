using System;

namespace DataDrivenDemo.Quest
{
    [Serializable]
    public sealed class QuestDefinition
    {
        public string id;
        public string title;
        public QuestReward reward;
        public QuestStep[] steps;
    }

    [Serializable]
    public sealed class QuestStep
    {
        public string actionId;
        public string objective;
        public int requiredCount = 1;
    }

    [Serializable]
    public sealed class QuestReward
    {
        public int coins;
    }
}

