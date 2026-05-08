using TMPro;
using UnityEngine;

namespace DataDrivenDemo.UI
{
    public enum QuestTrackerStatus
    {
        Active = 0,
        TurnInReady = 1,
        Completed = 2,
    }

    [DisallowMultipleComponent]
    public sealed class QuestTrackerRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;

        private string questId = "";
        private string lastTitle = "";
        private string lastDetail = "";

        public string QuestId => questId;
        public string LastTitle => lastTitle;
        public string LastDetail => lastDetail;

        public void Set(string title, string detail, QuestTrackerStatus status, string questKey = null)
        {
            questId = questKey ?? "";
            lastTitle = title ?? "";
            lastDetail = detail ?? "";
            if (titleText != null) titleText.text = lastTitle;
            if (detailText != null) detailText.text = lastDetail;
        }
    }
}

