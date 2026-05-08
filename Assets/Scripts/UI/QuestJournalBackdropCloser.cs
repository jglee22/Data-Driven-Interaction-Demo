using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    /// <summary>반투명 배경 클릭 시 부모 QuestJournalView 를 닫습니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class QuestJournalBackdropCloser : MonoBehaviour
    {
        private void Awake()
        {
            var img = GetComponent<Image>();
            img.raycastTarget = true;

            var btn = gameObject.GetComponent<Button>();
            if (btn == null) btn = gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                var j = GetComponentInParent<QuestJournalView>();
                j?.Close();
            });
        }
    }
}
