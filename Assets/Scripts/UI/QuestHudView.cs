using TMPro;
using UnityEngine;

namespace DataDrivenDemo.UI
{
    public sealed class QuestHudView : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text coinsText;

        [Header("Tracker List (optional)")]
        [SerializeField] private QuestTrackerListView trackerList;

        [Header("Quest Journal (optional)")]
        [SerializeField] private QuestJournalView questJournal;
        [Tooltip("테스트: Q 로 저널 열기/닫기")]
        [SerializeField] private bool journalToggleWithQ = true;

        [Header("Toast (optional)")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private float toastSeconds = 1.2f;

        private float toastUntil;

        private void Awake()
        {
            HideToast();
        }

        private void Update()
        {
            if (journalToggleWithQ && questJournal != null && Input.GetKeyDown(KeyCode.Q))
                ToggleQuestJournal();

            if (toastRoot == null) return;
            if (!toastRoot.activeSelf) return;

            if (Time.unscaledTime >= toastUntil)
                HideToast();
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title ?? "";
        }

        public void SetObjective(string objective)
        {
            if (objectiveText != null) objectiveText.text = objective ?? "";
        }

        public void SetProgress(int currentStepIndex, int totalSteps)
        {
            if (progressText != null) progressText.text = $"{Mathf.Clamp(currentStepIndex, 0, totalSteps)}/{Mathf.Max(0, totalSteps)}";
        }

        public void SetCoins(int coins)
        {
            if (coinsText != null) coinsText.text = $"코인: {coins}";
        }

        public void RenderTracker(System.Collections.Generic.IEnumerable<QuestTrackerItem> items)
        {
            trackerList?.Render(items);
        }

        public void ToggleQuestJournal() => questJournal?.Toggle();

        public void OpenQuestJournal() => questJournal?.Open();

        public void RefreshQuestJournalIfOpen()
        {
            if (questJournal != null && questJournal.IsOpen)
                questJournal.Refresh();
        }

        public void ShowToast(string message)
        {
            if (toastRoot == null || toastText == null) return;
            toastText.text = message ?? "";
            toastRoot.SetActive(true);
            toastUntil = Time.unscaledTime + Mathf.Max(0.1f, toastSeconds);
        }

        public void HideToast()
        {
            if (toastRoot != null) toastRoot.SetActive(false);
        }
    }
}

