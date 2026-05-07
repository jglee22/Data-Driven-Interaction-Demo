using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

#if GOOGLE_SIGNIN
using Google;
#endif

namespace DataDrivenDemo.Firebase
{
    /// <summary>
    /// Google Sign-In(Unity 플러그인) -> FirebaseAuth SignInWithCredential 래퍼.
    /// GOOGLE_SIGNIN 스크립팅 심볼과 GoogleSignIn 플러그인이 있을 때만 동작합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoogleSignInFirebaseAuth : MonoBehaviour
    {
        [Header("OAuth")]
        [Tooltip("Google OAuth 2.0 Web client ID (RequestIdToken=true에 필요)")]
        [SerializeField] private string webClientId = "";

        public string DebugWebClientIdStatus
        {
            get
            {
#if GOOGLE_SIGNIN
                if (string.IsNullOrWhiteSpace(webClientId))
                    return "empty";
                var len = webClientId.Length;
                var tail = len <= 6 ? webClientId : webClientId.Substring(len - 6);
                return $"set(len={len}, tail=...{tail})";
#else
                return "GOOGLE_SIGNIN define missing";
#endif
            }
        }

        public bool IsAvailable
        {
            get
            {
#if GOOGLE_SIGNIN
                return !string.IsNullOrWhiteSpace(webClientId);
#else
                return false;
#endif
            }
        }

        public void SignIn(Action<string> onSignedIn, Action<string> onFailed)
        {
#if GOOGLE_SIGNIN
#if UNITY_EDITOR
            onFailed?.Invoke("Google Sign-In is not supported in Unity Editor. Build & run on an Android device.");
            return;
#endif
#if !(UNITY_ANDROID || UNITY_IOS)
            onFailed?.Invoke("Google Sign-In is supported on Android/iOS only.");
            return;
#endif
            if (string.IsNullOrWhiteSpace(webClientId))
            {
                onFailed?.Invoke("Web client id is empty.");
                return;
            }

            Debug.Log("[GoogleSignInFirebaseAuth] SignIn start.");
            var config = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestEmail = true,
            };

            GoogleSignIn.Configuration = config;
            GoogleSignIn.DefaultInstance.SignIn()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        onFailed?.Invoke("Google sign-in canceled.");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        var ex = task.Exception != null ? task.Exception.InnerException ?? task.Exception : null;
                        onFailed?.Invoke("Google sign-in failed." + (ex != null ? " " + ex.Message : ""));
                        return;
                    }

                    var googleUser = task.Result;
                    if (googleUser == null || string.IsNullOrWhiteSpace(googleUser.IdToken))
                    {
                        onFailed?.Invoke("Google sign-in returned empty idToken.");
                        return;
                    }

                    var cred = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                    var auth = FirebaseAuth.DefaultInstance;
                    var current = auth.CurrentUser;
                    if (current != null && current.IsAnonymous)
                    {
                        // 익명 -> 구글로 업그레이드(진행도 유지)
                        current.LinkWithCredentialAsync(cred)
                            .ContinueWithOnMainThread((Task<AuthResult> t) =>
                            {
                                if (t.IsCanceled)
                                {
                                    onFailed?.Invoke("Firebase credential link canceled.");
                                    return;
                                }

                                if (t.IsFaulted)
                                {
                                    var ex = t.Exception != null ? t.Exception.InnerException ?? t.Exception : null;
                                    onFailed?.Invoke("Firebase credential link failed." + (ex != null ? " " + ex.Message : ""));
                                    return;
                                }

                                var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                                if (string.IsNullOrWhiteSpace(uid))
                                {
                                    onFailed?.Invoke("Firebase returned empty uid.");
                                    return;
                                }

                                onSignedIn?.Invoke(uid);
                            });
                    }
                    else
                    {
                        auth.SignInWithCredentialAsync(cred)
                            .ContinueWithOnMainThread((Task<FirebaseUser> t) =>
                            {
                                if (t.IsCanceled)
                                {
                                    onFailed?.Invoke("Firebase credential sign-in canceled.");
                                    return;
                                }

                                if (t.IsFaulted)
                                {
                                    var ex = t.Exception != null ? t.Exception.InnerException ?? t.Exception : null;
                                    onFailed?.Invoke("Firebase credential sign-in failed." + (ex != null ? " " + ex.Message : ""));
                                    return;
                                }

                                var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
                                if (string.IsNullOrWhiteSpace(uid))
                                {
                                    onFailed?.Invoke("Firebase returned empty uid.");
                                    return;
                                }

                                onSignedIn?.Invoke(uid);
                            });
                    }
                });
#else
            onFailed?.Invoke("Google Sign-In plugin not installed (missing GOOGLE_SIGNIN define).");
#endif
        }
    }
}

