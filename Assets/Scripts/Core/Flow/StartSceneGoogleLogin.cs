using UnityEngine;
using UnityEngine.SceneManagement;
using DataDrivenDemo.Firebase;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class StartSceneGoogleLogin : MonoBehaviour
    {
        [Header("Auth")]
        [SerializeField] private GoogleSignInFirebaseAuth googleAuth;

        [Header("Scene")]
        [Tooltip("구글 로그인 성공 시 로드할 씬 이름(빌드 세팅에 있어야 함).")]
        [SerializeField] private string demoSceneName = "DemoScene";

        [Tooltip("로그인 성공 시 자동으로 씬 로드")]
        [SerializeField] private bool loadSceneOnSuccess = true;

        private bool busy;

        private void Awake()
        {
            if (googleAuth == null)
                googleAuth = FindFirstObjectByType<GoogleSignInFirebaseAuth>(FindObjectsInactive.Include);
        }

        // UI Button OnClick에 연결
        public void OnClickGoogleLogin()
        {
            Debug.Log("[StartSceneGoogleLogin] Click.");
            if (busy)
                return;

            if (googleAuth == null || !googleAuth.IsAvailable)
            {
                Debug.LogWarning($"[StartSceneGoogleLogin] GoogleSignIn not available. googleAuth={(googleAuth != null ? googleAuth.name : "null")}, webClientId={googleAuth?.DebugWebClientIdStatus}");
                return;
            }

            busy = true;
            googleAuth.SignIn(
                onSignedIn: uid =>
                {
                    Debug.Log($"[StartSceneGoogleLogin] Google sign-in ok. uid={uid}");
                    busy = false;

                    if (!loadSceneOnSuccess)
                        return;

                    if (string.IsNullOrWhiteSpace(demoSceneName))
                    {
                        Debug.LogWarning("[StartSceneGoogleLogin] demoSceneName is empty.");
                        return;
                    }

                    SceneManager.LoadScene(demoSceneName);
                },
                onFailed: err =>
                {
                    busy = false;
                    Debug.LogWarning($"[StartSceneGoogleLogin] Google sign-in failed: {err}");
                });
        }
    }
}

