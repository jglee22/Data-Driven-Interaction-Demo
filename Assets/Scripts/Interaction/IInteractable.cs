using UnityEngine;

namespace DataDrivenDemo.Interaction
{
    public interface IInteractable
    {
        string Id { get; }
        string DisplayName { get; }

        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}

