using UnityEngine;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Interaction
{
    public sealed class NpcInteractable : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string actionId = "talk_npc";

        public override void Interact(GameObject interactor)
        {
            QuestEvents.RaiseAction(actionId);
            Debug.Log($"[NPC] Interacted: {Id} ({DisplayName}), action={actionId}");
        }
    }
}

