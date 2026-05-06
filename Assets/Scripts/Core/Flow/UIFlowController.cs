using UnityEngine;
using DataDrivenDemo.UI;
using DataDrivenDemo.Core.Save;

namespace DataDrivenDemo.Core.Flow
{
    /// <summary>
    /// 전역 입력(예: Esc)으로 UI 상태를 토글하는 컨트롤러.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIFlowController : MonoBehaviour
    {
        [SerializeField] private UIRoot uiRoot;
        [SerializeField] private bool startInGameplay = true;

        [Header("Optional Menu (fallback)")]
        [SerializeField] private MainMenuView menuView;
        [SerializeField] private string questId = "quest_001";

        private void Awake()
        {
            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();

            if (menuView == null)
                menuView = FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);
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
                // UIRoot가 없을 때도 최소한 메뉴 버튼 토글이 동작하도록
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
                    HandleStateChanged(UIState.Menu); // best-effort
            }
        }

        private void HandleStateChanged(UIState state)
        {
            if (menuView == null)
                return;

            if (state == UIState.Menu)
            {
                menuView.SetButtonsActive(true);
                menuView.SetContinueEnabled(SaveServices.QuestSave.LoadQuestState(questId) != null);
            }
        }
    }
}

