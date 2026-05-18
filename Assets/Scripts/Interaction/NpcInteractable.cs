using UnityEngine;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Interaction
{
    public sealed class NpcInteractable : InteractableBase
    {
        [Header("Quest")]
        [SerializeField] private string actionId = "talk_npc";

        protected override void Awake() => base.Awake();

        public override void Interact(GameObject interactor)
        {
            QuestEvents.RaiseEvent(new QuestEvent(QuestEventType.Talk, Id, 1, actionId));
            Debug.Log($"[NPC] Interacted: {Id} ({DisplayName}), action={actionId}");
        }
    }
}

