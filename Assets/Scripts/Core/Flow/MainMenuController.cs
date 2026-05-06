using UnityEngine;
using UnityEngine.SceneManagement;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.UI;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Quest Save Key")]
        [SerializeField] private string questId = "quest_001";

        [Header("Scene")]
        [Tooltip("비워두면 현재 씬을 다시 로드합니다.")]
        [SerializeField] private string playSceneName = "";

        [Header("UI")]
        [SerializeField] private MainMenuView menuView;
        [SerializeField] private UIRoot uiRoot;

        private void Awake()
        {
            if (menuView != null)
                menuView.SetHandlers(Continue, NewGame);

            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();
        }

        private void OnEnable()
        {
            // UIRoot로 메뉴 GameObject가 꺼졌다 켜질 때마다 버튼이 숨김 상태로 남지 않도록 보정
            if (menuView == null)
                return;

            menuView.Show();
            menuView.SetButtonsActive(true);
            menuView.SetContinueEnabled(HasSave());
        }

        private void Start()
        {
            if (menuView == null)
                return;

            menuView.Show();
            menuView.SetButtonsActive(true);
            menuView.SetContinueEnabled(HasSave());
        }

        private bool HasSave()
        {
            var saved = SaveServices.QuestSave.LoadQuestState(questId);
            return saved != null;
        }

        public void Continue()
        {
            uiRoot?.ShowGameplay();
            LoadPlayScene();
        }

        public void NewGame()
        {
            SaveServices.QuestSave.ClearQuestState(questId);
            uiRoot?.ShowGameplay();
            LoadPlayScene();
        }

        private void LoadPlayScene()
        {
            var sceneToLoad = string.IsNullOrWhiteSpace(playSceneName)
                ? SceneManager.GetActiveScene().name
                : playSceneName;

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}

