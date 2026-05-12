using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("Selection (저널 리스트 등)")]
        [SerializeField] private Color selectedRowColor = new Color(0.32f, 0.55f, 0.95f, 0.28f);

        private string questId = "";
        private string lastTitle = "";
        private string lastDetail = "";

        private Color _normalRowColor;
        private bool _normalRowColorCached;
        private bool _selected;

        public string QuestId => questId;
        public string LastTitle => lastTitle;
        public string LastDetail => lastDetail;

        private void Awake() => CacheNormalRowColorIfNeeded();

        public void Set(string title, string detail, QuestTrackerStatus status, string questKey = null)
        {
            questId = questKey ?? "";
            lastTitle = title ?? "";
            lastDetail = detail ?? "";
            if (titleText != null) titleText.text = lastTitle;
            if (detailText != null) detailText.text = lastDetail;

            CacheNormalRowColorIfNeeded();
        }

        /// <summary>저널 등에서 현재 선택된 퀘스트 행을 강조할 때 사용합니다.</summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            CacheNormalRowColorIfNeeded();
            ApplySelectionVisual();
        }

        private void CacheNormalRowColorIfNeeded()
        {
            if (_normalRowColorCached)
                return;

            var img = GetComponent<Image>();
            if (img == null)
                return;

            _normalRowColor = img.color;
            _normalRowColorCached = true;
        }

        private void ApplySelectionVisual()
        {
            var img = GetComponent<Image>();
            if (img == null || !_normalRowColorCached)
                return;

            img.color = _selected ? selectedRowColor : _normalRowColor;
        }
    }
}
