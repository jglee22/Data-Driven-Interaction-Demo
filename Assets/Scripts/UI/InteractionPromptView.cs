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
            // UIRoot 등으로 PromptUI가 재활성화될 때, 접촉 전 프롬프트가 보이지 않도록 강제로 숨김
            // (root 미지정 시 gameObject를 끄면 Show->OnEnable->Hide 루프가 생길 수 있어, 내부 요소만 숨김)
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
            // root가 자기 자신을 가리키면(SetActive가 OnEnable을 다시 유발),
            // Show -> OnEnable -> Hide 루프가 생길 수 있어 안전하게 무시한다.
            if (root != null && root != gameObject)
            {
                root.SetActive(visible);
                return;
            }

            // root를 안 쓰는 구성(컴포넌트가 PromptUI 그룹에 붙어있는 경우)을 위해
            // gameObject 자체는 끄지 않고, 표시 요소만 토글한다.
            if (label != null) label.gameObject.SetActive(visible);
            if (button != null) button.gameObject.SetActive(visible);
        }
    }
}

