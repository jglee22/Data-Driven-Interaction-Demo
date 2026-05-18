using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class StartSceneEmailAuth : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Toggle rememberToggle;
        [SerializeField] private Toggle autoSignInToggle;

        [Header("Scene")]
        [Tooltip("이메일 인증 완료 후 로드할 씬 이름(빌드 세팅에 있어야 함).")]
        [SerializeField] private string demoSceneName = "DemoScene";

        [Header("Behavior")]
        [Tooltip("회원가입 직후 자동으로 인증메일을 발송합니다.")]
        [SerializeField] private bool sendVerificationAfterSignUp = true;
        [Tooltip("로그인 성공 시 이메일 인증이 완료돼 있으면 바로 씬을 로드합니다.")]
        [SerializeField] private bool autoContinueOnVerifiedSignIn = true;

        private FirebaseAuth auth;
        private bool busy;

        private void Awake()
        {
            auth = FirebaseAuth.DefaultInstance;
            ClearLegacyCredentialPrefs();
            HideRememberUi();
            Debug.Log("[StartSceneEmailAuth] Awake.");
            SetStatus("준비됨");
        }

        public void OnClickSignUp()
        {
            Debug.Log("[StartSceneEmailAuth] SignUp click.");
            if (busy) return;
            var (email, pass) = ReadCredentials();
            if (!Validate(email, pass)) return;

            busy = true;
            SetStatus("회원가입 중...");

            auth.CreateUserWithEmailAndPasswordAsync(email, pass)
                .ContinueWithOnMainThread((Task<AuthResult> task) =>
                {
                    busy = false;

                    if (task.IsCanceled)
                    {
                        SetStatus("회원가입 취소됨");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        SetStatus("회원가입 실패: " + Summarize(task.Exception));
                        return;
                    }

                    SetStatus("회원가입 완료");

                    if (sendVerificationAfterSignUp)
                        OnClickSendVerificationEmail();
                });
        }

        public void OnClickSignIn()
        {
            Debug.Log("[StartSceneEmailAuth] SignIn click.");
            if (busy) return;
            var (email, pass) = ReadCredentials();
            if (!Validate(email, pass)) return;

            busy = true;
            SetStatus("로그인 중...");

            auth.SignInWithEmailAndPasswordAsync(email, pass)
                .ContinueWithOnMainThread((Task<AuthResult> task) =>
                {
                    if (task.IsCanceled)
                    {
                        busy = false;
                        SetStatus("로그인 취소됨");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        busy = false;
                        SetStatus("로그인 실패: " + Summarize(task.Exception));
                        return;
                    }

                    var user = auth.CurrentUser;
                    if (user == null)
                    {
                        busy = false;
                        SetStatus("로그인 실패: user=null");
                        return;
                    }

                    if (!autoContinueOnVerifiedSignIn)
                    {
                        busy = false;
                        SetStatus(user.IsEmailVerified
                            ? "로그인 완료 (인증됨)"
                            : "로그인 완료 (미인증) - 인증메일을 확인하세요");
                        return;
                    }

                    user.ReloadAsync()
                        .ContinueWithOnMainThread(reloadTask =>
                        {
                            busy = false;

                            if (reloadTask.IsCanceled)
                            {
                                SetStatus("인증 상태 확인 취소됨");
                                return;
                            }

                            if (reloadTask.IsFaulted)
                            {
                                SetStatus("인증 상태 확인 실패: " + Summarize(reloadTask.Exception));
                                return;
                            }

                            var refreshed = auth.CurrentUser;
                            if (refreshed == null)
                            {
                                SetStatus("인증 확인 실패: user=null");
                                return;
                            }

                            if (!refreshed.IsEmailVerified)
                            {
                                SetStatus("로그인 완료 (미인증) - 메일 인증 후 다시 로그인하세요");
                                return;
                            }

                            SetStatus("로그인 완료! 데모 씬으로 이동합니다.");
                            if (!string.IsNullOrWhiteSpace(demoSceneName))
                                SceneManager.LoadScene(demoSceneName);
                        });
                });
        }

        public void OnClickSendVerificationEmail()
        {
            Debug.Log("[StartSceneEmailAuth] SendVerification click.");
            if (busy) return;
            var user = auth.CurrentUser;
            if (user == null)
            {
                SetStatus("인증메일 발송 실패: 로그인 필요");
                return;
            }

            busy = true;
            SetStatus("인증메일 발송 중...");
            user.SendEmailVerificationAsync()
                .ContinueWithOnMainThread(task =>
                {
                    busy = false;
                    if (task.IsCanceled)
                    {
                        SetStatus("인증메일 발송 취소됨");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        SetStatus("인증메일 발송 실패: " + Summarize(task.Exception));
                        return;
                    }

                    SetStatus("인증메일을 보냈습니다. 메일함에서 인증 후 '인증 확인'을 눌러주세요.");
                });
        }

        public void OnClickCheckVerifiedAndContinue()
        {
            Debug.Log("[StartSceneEmailAuth] CheckVerified click.");
            if (busy) return;
            var user = auth.CurrentUser;
            if (user == null)
            {
                SetStatus("인증 확인 실패: 로그인 필요");
                return;
            }

            busy = true;
            SetStatus("인증 상태 확인 중...");

            user.ReloadAsync()
                .ContinueWithOnMainThread(task =>
                {
                    busy = false;
                    if (task.IsCanceled)
                    {
                        SetStatus("인증 확인 취소됨");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        SetStatus("인증 확인 실패: " + Summarize(task.Exception));
                        return;
                    }

                    var refreshed = auth.CurrentUser;
                    if (refreshed == null)
                    {
                        SetStatus("인증 확인 실패: user=null");
                        return;
                    }

                    if (!refreshed.IsEmailVerified)
                    {
                        SetStatus("아직 미인증입니다. 메일에서 인증 후 다시 시도하세요.");
                        return;
                    }

                    SetStatus("인증 완료! 데모 씬으로 이동합니다.");
                    if (!string.IsNullOrWhiteSpace(demoSceneName))
                        SceneManager.LoadScene(demoSceneName);
                });
        }

        public void OnClickSignOut()
        {
            Debug.Log("[StartSceneEmailAuth] SignOut click.");
            if (busy) return;
            auth.SignOut();
            SetStatus("로그아웃됨");
        }

        /// <summary>UI 토글 바인딩 호환용. 비밀번호는 저장하지 않습니다.</summary>
        public void SetRememberCredentials(bool enabled) { }

        /// <summary>UI 토글 바인딩 호환용. 자동 로그인은 지원하지 않습니다.</summary>
        public void SetAutoSignInOnStart(bool enabled) { }

        private (string email, string pass) ReadCredentials()
        {
            var email = emailInput != null ? (emailInput.text ?? "").Trim() : "";
            var pass = passwordInput != null ? (passwordInput.text ?? "") : "";
            return (email, pass);
        }

        private bool Validate(string email, string pass)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                SetStatus("이메일을 입력하세요");
                return false;
            }

            if (string.IsNullOrWhiteSpace(pass) || pass.Length < 6)
            {
                SetStatus("비밀번호는 6자 이상이어야 합니다");
                return false;
            }

            return true;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg ?? "";
            else if (!string.IsNullOrWhiteSpace(msg))
                Debug.Log("[StartSceneEmailAuth] " + msg);
        }

        private const string LegacyKeyEmail = "ddidemo.auth.email";
        private const string LegacyKeyPass = "ddidemo.auth.pass";
        private const string LegacyKeyRemember = "ddidemo.auth.remember";
        private const string LegacyKeyAuto = "ddidemo.auth.auto";

        private static void ClearLegacyCredentialPrefs()
        {
            PlayerPrefs.DeleteKey(LegacyKeyEmail);
            PlayerPrefs.DeleteKey(LegacyKeyPass);
            PlayerPrefs.DeleteKey(LegacyKeyRemember);
            PlayerPrefs.DeleteKey(LegacyKeyAuto);
            PlayerPrefs.Save();
        }

        private void HideRememberUi()
        {
            if (rememberToggle != null)
                rememberToggle.gameObject.SetActive(false);
            if (autoSignInToggle != null)
                autoSignInToggle.gameObject.SetActive(false);
        }

        private static string Summarize(AggregateException ex)
        {
            if (ex == null) return "";
            var inner = ex.InnerException ?? ex;
            return inner.Message;
        }
    }
}
