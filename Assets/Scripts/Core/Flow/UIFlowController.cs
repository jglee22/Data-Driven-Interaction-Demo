using UnityEngine;
using DataDrivenDemo.UI;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class UIFlowController : MonoBehaviour
    {
        [SerializeField] private UIRoot uiRoot;
        [SerializeField] private bool startInGameplay = true;

        [Header("Optional Menu (fallback)")]
        [SerializeField] private MainMenuView menuView;
        [SerializeField] private QuestCatalog questCatalog;

        private void Awake()
        {
            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();

            if (menuView == null)
                menuView = FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);

            if (questCatalog == null)
                questCatalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            if (uiRoot != null)
            {
                uiRoot.StateChanged += HandleStateChanged;
                uiRoot.SetState(startInGameplay ? UIState.Gameplay : UIState.Menu);
            }
            else
            {
                HandleStateChanged(startInGameplay ? UIState.Gameplay : UIState.Menu);
            }
        }

        private void OnDestroy()
        {
            if (uiRoot != null)
                uiRoot.StateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (uiRoot != null)
                    uiRoot.ToggleMenu();
                else
                    HandleStateChanged(UIState.Menu);
            }
        }

        private void HandleStateChanged(UIState state)
        {
            if (menuView == null)
                return;

            if (state == UIState.Menu)
            {
                menuView.SetButtonsActive(true);
                menuView.SetContinueEnabled(QuestDemoSaveHelper.HasAnySavedProgress(questCatalog));
            }
        }
    }
}
