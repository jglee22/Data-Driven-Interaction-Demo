using System;

namespace DataDrivenDemo.Quest
{
    public static class QuestEvents
    {
        /// <summary>상호작용/획득/제출 등 "행동"을 퀘스트에 전달하는 최소 이벤트.</summary>
        public static event Action<string> ActionPerformed;

        public static void RaiseAction(string actionId)
        {
            ActionPerformed?.Invoke(actionId);
        }
    }
}

