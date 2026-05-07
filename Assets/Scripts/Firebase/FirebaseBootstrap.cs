using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

namespace DataDrivenDemo.Firebase
{
    [DisallowMultipleComponent]
    public sealed class FirebaseBootstrap : MonoBehaviour
    {
        public event Action<string> SignedIn;
        public event Action<string> InitFailed;

        public bool IsReady { get; private set; }
        public string UserId { get; private set; }

        private FirebaseAuth auth;
        private bool authFlowFinished;
        private bool anonymousSignInStarted;

        [Header("Auth")]
        [Tooltip("로그인된 유저가 없을 때 익명 로그인으로 자동 폴백할지 여부. (구글 로그인 사용 시 false 권장)")]
        [SerializeField] private bool autoAnonymousFallback = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread((Task<DependencyStatus> task) =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Fail("Firebase dependency check failed.", task.Exception);
                        return;
                    }

                    if (task.Result != DependencyStatus.Available)
                    {
                        Fail($"Firebase dependencies not available: {task.Result}");
                        return;
                    }

                    StartAuthFlow();
                });
        }

        private void StartAuthFlow()
        {
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += OnAuthStateChanged;
            OnAuthStateChanged(null, EventArgs.Empty);
        }

        private void OnAuthStateChanged(object sender, EventArgs _)
        {
            if (authFlowFinished || auth == null)
                return;

            var user = auth.CurrentUser;
            if (user != null && !string.IsNullOrWhiteSpace(user.UserId))
            {
                Complete(user);
                return;
            }

            if (!autoAnonymousFallback)
                return;

            if (anonymousSignInStarted)
                return;

            anonymousSignInStarted = true;
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread((Task<AuthResult> task) =>
            {
                if (authFlowFinished)
                    return;

                if (task.IsFaulted)
                {
                    anonymousSignInStarted = false;
                    Fail("Anonymous sign-in failed (exception).", task.Exception);
                    return;
                }

                if (task.IsCanceled)
                {
                    anonymousSignInStarted = false;
                    Fail("Anonymous sign-in canceled.");
                    return;
                }

                var u = task.Result?.User;
                if (u != null && !string.IsNullOrWhiteSpace(u.UserId))
                    Complete(u);
            });
        }

        private void Complete(FirebaseUser user)
        {
            if (authFlowFinished || user == null)
                return;

            authFlowFinished = true;
            if (auth != null)
                auth.StateChanged -= OnAuthStateChanged;

            UserId = user.UserId;
            IsReady = true;
            Debug.Log($"[FirebaseBootstrap] Signed in. uid={UserId}");
            SignedIn?.Invoke(UserId);
        }

        private void Fail(string message, Exception exception = null)
        {
            if (auth != null)
                auth.StateChanged -= OnAuthStateChanged;

            var detail = exception != null ? $" {exception}" : "";
            Debug.LogError($"[FirebaseBootstrap] {message}{detail}");
            IsReady = false;
            InitFailed?.Invoke(message);
        }
    }
}

