using System;

namespace DataDrivenDemo.Core.Save
{
    [Serializable]
    public sealed class QuestState
    {
        public string questId;
        public int stepIndex;
        public int stepCount;
        public int coins;
        public bool completed;
        public bool turnedIn;
    }
}

