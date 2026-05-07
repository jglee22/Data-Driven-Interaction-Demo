using System;
using UnityEngine;

namespace DataDrivenDemo.UI
{
    public enum UIState
    {
        Gameplay = 0,
        Menu = 1,
    }

    public sealed class UIRoot : MonoBehaviour
    {
        [Header("Groups")]
        [SerializeField] private GameObject promptUI;
        [SerializeField] private GameObject questHud;
        [SerializeField] private GameObject mainMenu;

        public UIState State { get; private set; } = UIState.Gameplay;
        public event Action<UIState> StateChanged;

        public void SetState(UIState state)
        {
            State = state;

            // Menu가 열리면 게임 중 UI는 숨김
            var gameplayVisible = state == UIState.Gameplay;
            if (promptUI != null) promptUI.SetActive(gameplayVisible);
            if (questHud != null) questHud.SetActive(gameplayVisible);
            if (mainMenu != null) mainMenu.SetActive(!gameplayVisible);

            StateChanged?.Invoke(State);
        }

        public void ToggleMenu()
        {
            SetState(State == UIState.Menu ? UIState.Gameplay : UIState.Menu);
        }

        public void ShowMenu() => SetState(UIState.Menu);
        public void ShowGameplay() => SetState(UIState.Gameplay);
    }
}

