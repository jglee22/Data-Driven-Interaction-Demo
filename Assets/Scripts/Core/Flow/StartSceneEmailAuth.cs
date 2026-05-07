using System;
using System.Text;
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
        [Tooltip("아이디/비밀번호를 이 기기에 저장합니다. (데모/개발용: 보안 저장 아님)")]
        [SerializeField] private bool rememberCredentials = true;
        [Tooltip("저장된 아이디/비밀번호가 있으면 시작 시 자동으로 로그인 시도합니다.")]
        [SerializeField] private bool autoSignInOnStart = false;

        private FirebaseAuth auth;
        private bool busy;

        private void Awake()
        {
            auth = FirebaseAuth.DefaultInstance;
            Debug.Log("[StartSceneEmailAuth] Awake.");
            SetStatus("준비됨");

            LoadSavedOptions();
            SyncTogglesFromOptions();
            LoadSavedCredentialsIntoInputs();
            if (autoSignInOnStart)
                TryAutoSignIn();
        }

        public void SetRememberCredentials(bool enabled)
        {
            rememberCredentials = enabled;
            if (!rememberCredentials)
                ClearSavedCredentials();
            SaveOptions();
        }

        public void SetAutoSignInOnStart(bool enabled)
        {
            autoSignInOnStart = enabled;
            if (autoSignInOnStart)
                TryAutoSignIn();
            SaveOptions();
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

                    // 인증 상태는 로컬 캐시일 수 있어 reload 후 최종 판단
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

                            if (rememberCredentials)
                                SaveCredentials(email, pass);

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
            if (!rememberCredentials)
                ClearSavedCredentials();
            SetStatus("로그아웃됨");
        }

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

        private const string KeyEmail = "ddidemo.auth.email";
        private const string KeyPass = "ddidemo.auth.pass";
        private const string KeyRemember = "ddidemo.auth.remember";
        private const string KeyAuto = "ddidemo.auth.auto";

        private void LoadSavedOptions()
        {
            // 인스펙터 기본값을 fallback으로 두고, 저장값이 있으면 덮어씀
            if (PlayerPrefs.HasKey(KeyRemember))
                rememberCredentials = PlayerPrefs.GetInt(KeyRemember, rememberCredentials ? 1 : 0) == 1;
            if (PlayerPrefs.HasKey(KeyAuto))
                autoSignInOnStart = PlayerPrefs.GetInt(KeyAuto, autoSignInOnStart ? 1 : 0) == 1;

            if (!rememberCredentials)
            {
                // 저장 끄면 자동 로그인도 의미 없으니 같이 끔
                autoSignInOnStart = false;
            }
        }

        private void SaveOptions()
        {
            PlayerPrefs.SetInt(KeyRemember, rememberCredentials ? 1 : 0);
            PlayerPrefs.SetInt(KeyAuto, autoSignInOnStart ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SyncTogglesFromOptions()
        {
            if (rememberToggle == null || autoSignInToggle == null)
            {
                // 자동 생성 UI 이름 기반으로 찾아봄(선택)
                foreach (var t in GetComponentsInChildren<Toggle>(true))
                {
                    if (t == null) continue;
                    if (rememberToggle == null && t.gameObject.name.Contains("Remember"))
                        rememberToggle = t;
                    else if (autoSignInToggle == null && t.gameObject.name.Contains("AutoSignIn"))
                        autoSignInToggle = t;
                }
            }

            if (rememberToggle != null)
                rememberToggle.SetIsOnWithoutNotify(rememberCredentials);
            if (autoSignInToggle != null)
                autoSignInToggle.SetIsOnWithoutNotify(autoSignInOnStart);
        }

        private void LoadSavedCredentialsIntoInputs()
        {
            if (emailInput == null || passwordInput == null)
                return;

            var email = PlayerPrefs.GetString(KeyEmail, "");
            var pass = Decode(PlayerPrefs.GetString(KeyPass, ""));

            if (!string.IsNullOrWhiteSpace(email))
                emailInput.text = email;
            if (!string.IsNullOrWhiteSpace(pass))
                passwordInput.text = pass;
        }

        private void SaveCredentials(string email, string pass)
        {
            PlayerPrefs.SetString(KeyEmail, email ?? "");
            PlayerPrefs.SetString(KeyPass, Encode(pass ?? ""));
            PlayerPrefs.Save();
        }

        private void ClearSavedCredentials()
        {
            PlayerPrefs.DeleteKey(KeyEmail);
            PlayerPrefs.DeleteKey(KeyPass);
            PlayerPrefs.Save();
        }

        private void TryAutoSignIn()
        {
            if (busy) return;
            var (email, pass) = ReadCredentials();
            if (!Validate(email, pass)) return;
            OnClickSignIn();
        }

        private static string Encode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var bytes = Encoding.UTF8.GetBytes(raw);
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(bytes[i] ^ 0x5A);
            return Convert.ToBase64String(bytes);
        }

        private static string Decode(string enc)
        {
            if (string.IsNullOrWhiteSpace(enc)) return "";
            try
            {
                var bytes = Convert.FromBase64String(enc);
                for (var i = 0; i < bytes.Length; i++)
                    bytes[i] = (byte)(bytes[i] ^ 0x5A);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        private static string Summarize(AggregateException ex)
        {
            if (ex == null) return "";
            var inner = ex.InnerException ?? ex;
            return inner.Message;
        }
    }
}

