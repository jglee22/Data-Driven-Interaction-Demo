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
        [SerializeField] private bool hideButtonsOnAwake = true;

        private Action onContinue;
        private Action onNewGame;

        private void Awake()
        {
            ResolveButtonsIfMissing();

            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);
            if (newGameButton != null) newGameButton.onClick.AddListener(HandleNewGame);

            if (hideButtonsOnAwake)
                SetButtonsActive(false);
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinue);
            if (newGameButton != null) newGameButton.onClick.RemoveListener(HandleNewGame);
        }

        public void SetHandlers(Action onContinueClicked, Action onNewGameClicked)
        {
            onContinue = onContinueClicked;
            onNewGame = onNewGameClicked;
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

        private void ResolveButtonsIfMissing()
        {
            if (continueButton != null && newGameButton != null)
                return;

            var searchRoot = root != null ? root.transform : transform;
            var buttons = searchRoot.GetComponentsInChildren<Button>(true);

            foreach (var b in buttons)
            {
                if (b == null) continue;
                if (continueButton != null && newGameButton != null) break;

                var label = b.GetComponentInChildren<TMP_Text>(true);
                var text = label != null ? label.text : b.gameObject.name;

                if (continueButton == null && text != null && text.Contains("Continue", StringComparison.OrdinalIgnoreCase))
                    continueButton = b;
                else if (newGameButton == null && text != null && text.Contains("New", StringComparison.OrdinalIgnoreCase))
                    newGameButton = b;
            }
        }
    }
}

