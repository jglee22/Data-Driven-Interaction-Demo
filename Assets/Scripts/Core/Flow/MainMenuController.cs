using UnityEngine;
using UnityEngine.SceneManagement;
using DataDrivenDemo.UI;
using DataDrivenDemo.Firebase;
using DataDrivenDemo.Quest;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Quest Save")]
        [SerializeField] private QuestCatalog questCatalog;

        [Header("Scene")]
        [Tooltip("비워두면 현재 씬을 다시 로드합니다.")]
        [SerializeField] private string playSceneName = "";

        [Header("UI")]
        [SerializeField] private MainMenuView menuView;
        [SerializeField] private UIRoot uiRoot;

        [Header("Auth (Optional)")]
        [SerializeField] private bool showGoogleSignIn;
        [SerializeField] private GoogleSignInFirebaseAuth googleAuth;
        [SerializeField] private bool loadPlaySceneAfterGoogleSignIn = true;

        private void Awake()
        {
            if (questCatalog == null)
                questCatalog = FindFirstObjectByType<QuestCatalog>(FindObjectsInactive.Include);

            if (menuView != null)
            {
                menuView.ConfigureGoogleSignIn(showGoogleSignIn);
                menuView.SetHandlers(Continue, NewGame, showGoogleSignIn ? GoogleSignIn : null);
            }

            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();

            if (googleAuth == null)
                googleAuth = FindFirstObjectByType<GoogleSignInFirebaseAuth>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            if (menuView == null)
                return;

            if (ShouldShowMenuButtons())
            {
                RefreshContinueState();
                menuView.SetButtonsActive(true);
            }
            else
            {
                menuView.SetButtonsActive(false);
            }
        }

        private bool ShouldShowMenuButtons()
        {
            if (uiRoot == null)
                return false;

            return uiRoot.State == UIState.Menu;
        }

        private void RefreshContinueState()
        {
            menuView.SetContinueEnabled(false);
            QuestDemoSaveHelper.HasAnySavedProgressAsync(questCatalog, has =>
            {
                if (menuView != null)
                    menuView.SetContinueEnabled(has);
            });
        }

        public void Continue()
        {
            uiRoot?.ShowGameplay();
            LoadPlayScene();
        }

        public void NewGame()
        {
            QuestDemoSaveHelper.ClearAllProgress(questCatalog, () =>
            {
                uiRoot?.ShowGameplay();
                LoadPlayScene();
            });
        }

        public void GoogleSignIn()
        {
            if (!showGoogleSignIn)
                return;

            if (googleAuth == null || !googleAuth.IsAvailable)
            {
                Debug.LogWarning("[MainMenuController] GoogleSignIn not available. Install Google Sign-In plugin and set Web Client Id.");
                return;
            }

            googleAuth.SignIn(
                onSignedIn: uid =>
                {
                    Debug.Log($"[MainMenuController] Google sign-in ok. uid={uid}");
                    RefreshContinueState();

                    if (loadPlaySceneAfterGoogleSignIn)
                    {
                        uiRoot?.ShowGameplay();
                        LoadPlayScene();
                    }
                },
                onFailed: err => Debug.LogWarning($"[MainMenuController] Google sign-in failed: {err}"));
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
