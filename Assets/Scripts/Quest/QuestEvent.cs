using System;

namespace DataDrivenDemo.Quest
{
    public enum QuestEventType
    {
        Unknown = 0,
        Talk = 1,
        Pickup = 2,
        Submit = 3,
        Kill = 4,
        EnterArea = 5,
        Use = 6,
    }

    /// <summary>
    /// 현업형 이벤트: "무슨 행동을, 어떤 대상으로, 얼마나" 했는지 전달.
    /// actionId는 레거시 호환용(기존 JSON/스크립트)으로 유지합니다.
    /// </summary>
    [Serializable]
    public readonly struct QuestEvent
    {
        public readonly QuestEventType type;
        public readonly string targetId;
        public readonly int amount;
        public readonly string actionId;

        public QuestEvent(QuestEventType type, string targetId, int amount = 1, string actionId = null)
        {
            this.type = type;
            this.targetId = targetId ?? "";
            this.amount = Math.Max(1, amount);
            this.actionId = actionId ?? "";
        }
    }
}

