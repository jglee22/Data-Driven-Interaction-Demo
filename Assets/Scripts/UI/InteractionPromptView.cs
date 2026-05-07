using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        private Action onClick;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClick);
        }

        private void OnEnable()
        {
            // UI 그룹 토글 시 '기본은 숨김'을 보장.
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void SetOnClick(Action handler)
        {
            onClick = handler;
        }

        public void Show(string displayName)
        {
            if (label != null)
                label.text = $"{displayName} 상호작용";

            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void HandleClick()
        {
            onClick?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            // root가 자기 자신이면 Show->OnEnable->Hide 루프가 생길 수 있어 무시.
            if (root != null && root != gameObject)
            {
                root.SetActive(visible);
                return;
            }

            // gameObject 자체는 끄지 않고(재활성화 루프 회피), 표시 요소만 토글.
            if (label != null) label.gameObject.SetActive(visible);
            if (button != null) button.gameObject.SetActive(visible);
        }
    }
}

