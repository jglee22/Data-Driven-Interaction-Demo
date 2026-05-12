using DataDrivenDemo.UI;
using UnityEngine;

namespace DataDrivenDemo.Interaction
{
    /// <summary>NPC 근처에서 상호작용 시 의뢰 목록 패널을 엽니다. (기존 Talk 퀘스트 진행 이벤트는 발생하지 않습니다.)</summary>
    public sealed class QuestGiverInteractable : InteractableBase
    {
        [Header("Offer UI")]
        [SerializeField] private QuestOfferView offerView;

        [Tooltip("비어 있으면 카탈로그의 모든 퀘스트를 목록에 표시합니다.")]
        [SerializeField] private string[] offeredQuestIds;

        public string[] OfferedQuestIds => offeredQuestIds ?? System.Array.Empty<string>();

        public override void Interact(GameObject interactor)
        {
            if (offerView == null)
                offerView = FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include);

            offerView?.OpenFromGiver(this, interactor);
        }
    }
}
