using UnityEngine;
using UnityEngine.SceneManagement;
using DataDrivenDemo.Core.Save;
using DataDrivenDemo.UI;
using DataDrivenDemo.Firebase;

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

        [Header("Auth (Optional)")]
        [SerializeField] private GoogleSignInFirebaseAuth googleAuth;
        [SerializeField] private bool loadPlaySceneAfterGoogleSignIn = true;

        private void Awake()
        {
            if (menuView != null)
                menuView.SetHandlers(Continue, NewGame, GoogleSignIn);

            if (uiRoot == null)
                uiRoot = FindFirstObjectByType<UIRoot>();

            if (googleAuth == null)
                googleAuth = FindFirstObjectByType<GoogleSignInFirebaseAuth>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            if (menuView == null)
                return;

            // ESC로 메뉴가 열릴 때 버튼 표시/컨티뉴 활성은 UIFlowController(UIRoot StateChanged)가 담당.
            // 여기서 항상 켜면 첫 진입부터 New/Continue가 같이 보인다.
            if (ShouldShowMenuButtons())
            {
                SaveServices.QuestSave.LoadQuestStateAsync(questId,
                    saved => menuView.SetContinueEnabled(saved != null));
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

        public void Continue()
        {
            uiRoot?.ShowGameplay();
            LoadPlayScene();
        }

        public void NewGame()
        {
            SaveServices.QuestSave.ClearQuestState(questId, () =>
            {
                uiRoot?.ShowGameplay();
                LoadPlayScene();
            });
        }

        public void GoogleSignIn()
        {
            if (googleAuth == null || !googleAuth.IsAvailable)
            {
                Debug.LogWarning("[MainMenuController] GoogleSignIn not available. Install Google Sign-In plugin and set Web Client Id.");
                return;
            }

            googleAuth.SignIn(
                onSignedIn: uid =>
                {
                    Debug.Log($"[MainMenuController] Google sign-in ok. uid={uid}");
                    SaveServices.QuestSave.LoadQuestStateAsync(questId, saved => menuView?.SetContinueEnabled(saved != null));

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

