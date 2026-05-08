using UnityEngine;
using DataDrivenDemo.UI;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ProximityInteractor : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private InteractionPromptView promptView;

        [Header("Quest Filter (optional)")]
        [SerializeField] private bool onlyShowForAcceptedQuestTargets = true;
        [SerializeField] private QuestSystem questSystem;

        private IInteractable current;

        private void Update()
        {
            // 상호작용 대상이 비활성화/파괴되면 TriggerExit이 안 올 수 있어 강제로 정리
            if (current == null)
                return;

            if (current is Component c)
            {
                if (c == null || !c.gameObject.activeInHierarchy)
                {
                    current = null;
                    promptView?.Hide();
                    return;
                }
            }

            if (!current.CanInteract(gameObject))
            {
                current = null;
                promptView?.Hide();
                return;
            }

            if (onlyShowForAcceptedQuestTargets && !IsRelevant(current))
            {
                current = null;
                promptView?.Hide();
            }
        }

        private void Awake()
        {
            if (questSystem == null)
                questSystem = FindFirstObjectByType<QuestSystem>(FindObjectsInactive.Include);
            ResolvePromptView();
        }

        private void OnEnable()
        {
            // 메뉴 토글 등으로 비활성/활성 반복 시 상태를 초기화
            current = null;
            ResolvePromptView();
            promptView?.Hide();
        }

        private void OnTriggerEnter(Collider other)
        {
            // 상호작용 콜라이더가 자식일 수 있어 Parent까지 탐색.
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null) return;

            if (!interactable.CanInteract(gameObject))
                return;

            if (onlyShowForAcceptedQuestTargets && !IsRelevant(interactable))
                return;

            current = interactable;
            if (promptView != null)
                promptView.Show(interactable.DisplayName);
        }

        private void OnTriggerExit(Collider other)
        {
            if (current == null)
                return;

            // Exit한 콜라이더가 현재 대상인지 확인
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null && ReferenceEquals(interactable, current))
            {
                current = null;
                if (promptView != null)
                    promptView.Hide();
            }
        }

        private void Interact()
        {
            if (current == null)
                return;

            if (!current.CanInteract(gameObject))
                return;

            current.Interact(gameObject);

            // Interact 내부에서 비활성화되는 경우(아이템 줍기 등) 프롬프트가 남지 않게 정리
            if (current is Component c && (c == null || !c.gameObject.activeInHierarchy))
            {
                current = null;
                promptView?.Hide();
                return;
            }

            if (current != null && !current.CanInteract(gameObject))
            {
                current = null;
                promptView?.Hide();
                return;
            }

            if (onlyShowForAcceptedQuestTargets && current != null && !IsRelevant(current))
            {
                current = null;
                promptView?.Hide();
            }
        }

        private void ResolvePromptView()
        {
            // 씬에서 레퍼런스가 끊겨도(프리팹/씬 수정) 최소 동작 보장.
            if (promptView == null)
                promptView = FindFirstObjectByType<InteractionPromptView>(FindObjectsInactive.Include);

            if (promptView != null)
                promptView.SetOnClick(Interact);
        }

        private bool IsRelevant(IInteractable interactable)
        {
            if (questSystem == null || interactable == null)
                return false;

            if (interactable is not Component c || c == null)
                return false;

            // 인터랙터블 타입으로 QuestEventType 추론 (현 데모 범위)
            QuestEventType type;
            if (c.GetComponentInParent<NpcInteractable>() != null) type = QuestEventType.Talk;
            else if (c.GetComponentInParent<ItemPickupInteractable>() != null) type = QuestEventType.Pickup;
            else if (c.GetComponentInParent<TerminalSubmitInteractable>() != null) type = QuestEventType.Submit;
            else type = QuestEventType.Unknown;

            if (type == QuestEventType.Unknown)
                return false;

            var targetId = interactable.Id;
            return questSystem.IsRelevantForPrompt(type, targetId);
        }
    }
}

