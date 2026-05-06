using UnityEngine;
using DataDrivenDemo.UI;

namespace DataDrivenDemo.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ProximityInteractor : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private InteractionPromptView promptView;

        private IInteractable current;

        private void Awake()
        {
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
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable == null) return;

            if (!interactable.CanInteract(gameObject))
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
        }

        private void ResolvePromptView()
        {
            if (promptView == null)
                promptView = FindFirstObjectByType<InteractionPromptView>(FindObjectsInactive.Include);

            if (promptView != null)
                promptView.SetOnClick(Interact);
        }
    }
}

