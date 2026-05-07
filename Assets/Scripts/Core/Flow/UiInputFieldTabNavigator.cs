using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DataDrivenDemo.Core.Flow
{
    [DisallowMultipleComponent]
    public sealed class UiInputFieldTabNavigator : MonoBehaviour
    {
        [Header("Fields (order matters)")]
        [SerializeField] private List<TMP_InputField> fields = new();

        [Header("Submit")]
        [Tooltip("지정되면 마지막 필드(보통 PW)에서 Enter 시 호출합니다.")]
        [SerializeField] private StartSceneEmailAuth submitTarget;

        private void Reset()
        {
            fields.Clear();
            fields.AddRange(GetComponentsInChildren<TMP_InputField>(true));
        }

        private void Update()
        {
            if (fields == null || fields.Count == 0)
                return;

            if (!Input.GetKeyDown(KeyCode.Tab))
                return;

            var currentGo = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (currentGo == null)
                return;

            var currentField = currentGo.GetComponentInParent<TMP_InputField>();
            if (currentField == null)
                return;

            var idx = fields.IndexOf(currentField);
            if (idx < 0)
                return;

            var nextIdx = idx + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
            if (nextIdx < 0) nextIdx = fields.Count - 1;
            if (nextIdx >= fields.Count) nextIdx = 0;

            var next = fields[nextIdx];
            if (next == null)
                return;

            next.Select();
            next.ActivateInputField();
        }

        private void OnEnable()
        {
            if (fields == null) return;
            foreach (var f in fields)
            {
                if (f == null) continue;
                f.onSubmit.AddListener(HandleSubmit);
            }
        }

        private void OnDisable()
        {
            if (fields == null) return;
            foreach (var f in fields)
            {
                if (f == null) continue;
                f.onSubmit.RemoveListener(HandleSubmit);
            }
        }

        private void HandleSubmit(string _)
        {
            if (fields == null || fields.Count == 0)
                return;

            var currentGo = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var currentField = currentGo != null ? currentGo.GetComponentInParent<TMP_InputField>() : null;
            if (currentField == null)
                return;

            var idx = fields.IndexOf(currentField);
            if (idx < 0)
                return;

            // 마지막 필드에서 Enter -> 로그인
            if (idx == fields.Count - 1)
            {
                submitTarget?.OnClickSignIn();
                return;
            }

            // 그 외에는 다음 필드로 이동
            var next = fields[idx + 1];
            if (next == null)
                return;

            next.Select();
            next.ActivateInputField();
        }

        public void SetFieldsAndSubmitTarget(List<TMP_InputField> orderedFields, StartSceneEmailAuth target)
        {
            fields = orderedFields ?? new List<TMP_InputField>();
            submitTarget = target;
        }
    }
}

