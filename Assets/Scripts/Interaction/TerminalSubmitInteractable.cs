using UnityEngine;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Interaction
{
    public sealed class TerminalSubmitInteractable : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string actionId = "submit_terminal";

        public override void Interact(GameObject interactor)
        {
            QuestEvents.RaiseAction(actionId);
            Debug.Log($"[Terminal] Used: {Id} ({DisplayName}), action={actionId}");
        }
    }
}

