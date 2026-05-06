using UnityEngine;

namespace DataDrivenDemo.Interaction
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "오브젝트";

        public string Id => string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        public virtual bool CanInteract(GameObject interactor) => true;

        public abstract void Interact(GameObject interactor);
    }
}

