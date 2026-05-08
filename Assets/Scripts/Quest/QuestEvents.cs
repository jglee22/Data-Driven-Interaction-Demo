using System;

namespace DataDrivenDemo.Quest
{
    public static class QuestEvents
    {
        /// <summary>레거시(문자열) 액션 이벤트.</summary>
        public static event Action<string> ActionPerformed;

        /// <summary>현업형 이벤트.</summary>
        public static event Action<QuestEvent> EventRaised;

        public static void RaiseAction(string actionId)
        {
            ActionPerformed?.Invoke(actionId);
            EventRaised?.Invoke(ParseLegacy(actionId));
        }

        public static void RaiseEvent(QuestEvent evt)
        {
            EventRaised?.Invoke(evt);
            if (!string.IsNullOrWhiteSpace(evt.actionId))
                ActionPerformed?.Invoke(evt.actionId);
        }

        private static QuestEvent ParseLegacy(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                return new QuestEvent(QuestEventType.Unknown, "", 1, "");

            // 규칙: "Type:targetId" 형태도 지원
            var idx = actionId.IndexOf(':');
            if (idx > 0 && idx < actionId.Length - 1)
            {
                var typeStr = actionId.Substring(0, idx);
                var target = actionId.Substring(idx + 1);
                return new QuestEvent(ParseType(typeStr), target, 1, actionId);
            }

            // 기존 데모: talk_*/pickup_*/submit_* -> 타입 추론, targetId는 actionId로 둠(레거시 매칭)
            if (actionId.StartsWith("talk_", StringComparison.Ordinal))
                return new QuestEvent(QuestEventType.Talk, actionId, 1, actionId);
            if (actionId.StartsWith("pickup_", StringComparison.Ordinal))
                return new QuestEvent(QuestEventType.Pickup, actionId, 1, actionId);
            if (actionId.StartsWith("submit_", StringComparison.Ordinal))
                return new QuestEvent(QuestEventType.Submit, actionId, 1, actionId);
            if (actionId.StartsWith("kill_", StringComparison.Ordinal))
                return new QuestEvent(QuestEventType.Kill, actionId, 1, actionId);

            return new QuestEvent(QuestEventType.Unknown, actionId, 1, actionId);
        }

        private static QuestEventType ParseType(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return QuestEventType.Unknown;
            if (string.Equals(s, "Talk", StringComparison.OrdinalIgnoreCase)) return QuestEventType.Talk;
            if (string.Equals(s, "Pickup", StringComparison.OrdinalIgnoreCase)) return QuestEventType.Pickup;
            if (string.Equals(s, "Submit", StringComparison.OrdinalIgnoreCase)) return QuestEventType.Submit;
            if (string.Equals(s, "Kill", StringComparison.OrdinalIgnoreCase)) return QuestEventType.Kill;
            if (string.Equals(s, "EnterArea", StringComparison.OrdinalIgnoreCase)) return QuestEventType.EnterArea;
            if (string.Equals(s, "Use", StringComparison.OrdinalIgnoreCase)) return QuestEventType.Use;
            return QuestEventType.Unknown;
        }
    }
}

