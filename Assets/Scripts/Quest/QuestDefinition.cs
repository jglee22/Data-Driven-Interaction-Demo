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
        // New format (현업형)
        public QuestObjective[] objectives;

        // Legacy format (호환)
        public string actionId;
        public string objective;
        public int requiredCount = 1;
    }

    [Serializable]
    public sealed class QuestObjective
    {
        public string type;        // Talk / Pickup / Submit / Kill ...
        public string targetId;    // npc_01 / item_apple / terminal_01 ...
        public string actionId;    // 레거시 매칭용(선택): talk_npc 같은 전역 액션
        public int requiredCount = 1;
        public string uiText;      // UI 표기용(선택)
    }

    [Serializable]
    public sealed class QuestReward
    {
        public int coins;
    }
}

