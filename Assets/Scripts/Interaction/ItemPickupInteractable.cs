using UnityEngine;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Interaction
{
    public sealed class ItemPickupInteractable : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string actionId = "pickup_item";

        [Header("Behavior")]
        [SerializeField] private bool disableAfterPickup = true;

        private bool pickedUp;

        protected override void Awake() => base.Awake();

        public override bool CanInteract(GameObject interactor) => !pickedUp;

        public override void Interact(GameObject interactor)
        {
            if (pickedUp) return;

            pickedUp = true;
            QuestEvents.RaiseEvent(new QuestEvent(QuestEventType.Pickup, Id, 1, actionId));
            Debug.Log($"[Item] Picked up: {Id} ({DisplayName}), action={actionId}");

            if (disableAfterPickup)
                gameObject.SetActive(false);
        }
    }
}

