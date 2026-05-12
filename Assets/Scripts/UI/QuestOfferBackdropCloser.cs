using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class QuestOfferBackdropCloser : MonoBehaviour, IPointerClickHandler
    {
        private void Awake()
        {
            var img = GetComponent<Image>();
            img.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            GetComponentInParent<QuestOfferView>()?.Close();
        }
    }
}
