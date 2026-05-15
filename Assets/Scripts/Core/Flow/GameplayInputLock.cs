using UnityEngine;
using DataDrivenDemo.Interaction;
using DataDrivenDemo.Player;
using DataDrivenDemo.UI;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class GameplayInputLock : MonoBehaviour
    {
        [Header("References (optional)")]
        [SerializeField] private UIRoot uiRoot;

        [Header("Behavior")]
        [SerializeField] private bool pauseTimeScale = false;
        [Tooltip("체크하면 메뉴가 아닐 때 커서를 숨기고 Locked(1인칭 FPS용). UGUI 버튼(프롬프트 등) 쓸 땐 끌 것.")]
        [SerializeField] private bool lockCursorDuringGameplay = false;
        [SerializeField] private bool setRigidbodyKinematicInMenu = true;

        private float prevTimeScale = 1f;
        private bool prevKinematic;

        private void Awake()
        {
            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();

            PlayerLocator.Refresh();
        }

        private void OnEnable()
        {
            if (uiRoot != null)
                uiRoot.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (uiRoot != null)
                uiRoot.StateChanged -= HandleStateChanged;
        }

        private void Start()
        {
            if (uiRoot != null)
                HandleStateChanged(uiRoot.State);
        }

        private void HandleStateChanged(UIState state)
        {
            var menuOpen = state == UIState.Menu;

            var playerController = PlayerLocator.Controller;
            var proximityInteractor = PlayerLocator.Interactor;
            var playerRigidbody = PlayerLocator.Rigidbody;

            if (playerController != null)
                playerController.SetMovementLock(QuarterViewMovementLockSource.Menu, menuOpen);

            if (proximityInteractor != null)
                proximityInteractor.enabled = !menuOpen;

            if (setRigidbodyKinematicInMenu && playerRigidbody != null)
            {
                if (menuOpen)
                {
                    prevKinematic = playerRigidbody.isKinematic;
                    playerRigidbody.isKinematic = true;
                    playerRigidbody.linearVelocity = Vector3.zero;
                    playerRigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    playerRigidbody.isKinematic = prevKinematic;
                }
            }

            if (pauseTimeScale)
            {
                if (menuOpen)
                {
                    prevTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
                }
            }

            if (!lockCursorDuringGameplay)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = menuOpen;
                Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }
    }
}
