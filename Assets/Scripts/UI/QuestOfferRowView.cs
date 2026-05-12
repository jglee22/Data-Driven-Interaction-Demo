using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    [DisallowMultipleComponent]
    public sealed class QuestOfferRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button button;
        [SerializeField] private Image background;

        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color selectedColor = new Color(0.28f, 0.45f, 0.85f, 0.35f);

        private QuestOfferView owner;
        private string questId = "";

        public string QuestId => questId;

        public void Bind(QuestOfferView view, string id, string title, string status)
        {
            owner = view;
            questId = id ?? "";
            if (titleText != null) titleText.text = title ?? "";
            if (statusText != null) statusText.text = status ?? "";
            SetRowSelected(false);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        private void OnClicked()
        {
            owner?.NotifyRowClicked(questId);
        }

        public void SetRowSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
        }
    }
}
