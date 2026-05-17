using System;

using UnityEngine;

using UnityEngine.UI;

using TMPro;



namespace DataDrivenDemo.UI

{

    public sealed class MainMenuView : MonoBehaviour

    {

        [SerializeField] private GameObject root;

        [SerializeField] private Button continueButton;

        [SerializeField] private Button newGameButton;

        [SerializeField] private Button googleSignInButton;

        [SerializeField] private bool hideButtonsOnAwake = true;



        private bool showGoogleSignIn;

        private Action onContinue;

        private Action onNewGame;

        private Action onGoogleSignIn;



        private void Awake()

        {

            ResolveButtonsIfMissing();



            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);

            if (newGameButton != null) newGameButton.onClick.AddListener(HandleNewGame);

            if (googleSignInButton != null) googleSignInButton.onClick.AddListener(HandleGoogleSignIn);



            ApplyGoogleSignInVisibility();



            if (hideButtonsOnAwake)

                SetButtonsActive(false);

        }



        private void OnDestroy()

        {

            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinue);

            if (newGameButton != null) newGameButton.onClick.RemoveListener(HandleNewGame);

            if (googleSignInButton != null) googleSignInButton.onClick.RemoveListener(HandleGoogleSignIn);

        }



        public void ConfigureGoogleSignIn(bool visible)

        {

            showGoogleSignIn = visible;

            ApplyGoogleSignInVisibility();

        }



        public void SetHandlers(Action onContinueClicked, Action onNewGameClicked, Action onGoogleSignInClicked = null)

        {

            onContinue = onContinueClicked;

            onNewGame = onNewGameClicked;

            onGoogleSignIn = onGoogleSignInClicked;

        }



        public void SetContinueEnabled(bool enabled)

        {

            if (continueButton != null) continueButton.interactable = enabled;

        }



        public void SetButtonsActive(bool active)

        {

            ResolveButtonsIfMissing();



            if (continueButton != null) continueButton.gameObject.SetActive(active);

            if (newGameButton != null) newGameButton.gameObject.SetActive(active);

            if (googleSignInButton != null)

                googleSignInButton.gameObject.SetActive(active && showGoogleSignIn);

        }



        public void Show()

        {

            if (root != null) root.SetActive(true);

            else gameObject.SetActive(true);

        }



        public void Hide()

        {

            if (root != null) root.SetActive(false);

            else gameObject.SetActive(false);

        }



        private void HandleContinue() => onContinue?.Invoke();

        private void HandleNewGame() => onNewGame?.Invoke();

        private void HandleGoogleSignIn() => onGoogleSignIn?.Invoke();



        private void ApplyGoogleSignInVisibility()

        {

            if (googleSignInButton != null)

                googleSignInButton.gameObject.SetActive(showGoogleSignIn);

        }



        private void ResolveButtonsIfMissing()

        {

            if (continueButton != null && newGameButton != null && googleSignInButton != null)

                return;



            var searchRoot = root != null ? root.transform : transform;

            var buttons = searchRoot.GetComponentsInChildren<Button>(true);



            foreach (var b in buttons)

            {

                if (b == null) continue;

                if (continueButton != null && newGameButton != null && googleSignInButton != null) break;



                var label = b.GetComponentInChildren<TMP_Text>(true);

                var text = label != null ? label.text : b.gameObject.name;



                if (continueButton == null && text != null && text.Contains("Continue", StringComparison.OrdinalIgnoreCase))

                    continueButton = b;

                else if (newGameButton == null && text != null && text.Contains("New", StringComparison.OrdinalIgnoreCase))

                    newGameButton = b;

                else if (googleSignInButton == null && text != null &&

                         (text.Contains("Google", StringComparison.OrdinalIgnoreCase) ||

                          text.Contains("Login", StringComparison.OrdinalIgnoreCase) ||

                          text.Contains("Sign", StringComparison.OrdinalIgnoreCase)))

                    googleSignInButton = b;

            }



            ApplyGoogleSignInVisibility();

        }

    }

}


