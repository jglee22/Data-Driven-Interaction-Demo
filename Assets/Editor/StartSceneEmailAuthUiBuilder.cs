using System.Linq;
using DataDrivenDemo.Core.Flow;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DataDrivenDemo.EditorTools
{
    public static class StartSceneEmailAuthUiBuilder
    {
        [MenuItem("Tools/DataDrivenDemo/Build StartScene Email Auth UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[StartSceneEmailAuthUiBuilder] No active scene loaded.");
                return;
            }

            EnsureEventSystem();

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                canvas = CreateCanvas();

            var root = new GameObject("EmailAuthUI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, "Create EmailAuthUI");
            root.transform.SetParent(canvas.transform, false);

            var panel = CreatePanel(root.transform);

            var title = CreateText(panel.transform, "Title", "Email Auth", 36, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(800, 60));

            var email = CreateInput(panel.transform, "EmailInput", "Email", false);
            SetAnchored(email.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(800, 60));

            var pass = CreateInput(panel.transform, "PasswordInput", "Password (>= 6)", true);
            SetAnchored(pass.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -210), new Vector2(800, 60));

            var togglesRow = new GameObject("TogglesRow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(togglesRow, "Create toggles row");
            togglesRow.transform.SetParent(panel.transform, false);
            var togglesRt = togglesRow.GetComponent<RectTransform>();
            SetAnchored(togglesRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -285), new Vector2(900, 40));
            var togglesLayout = togglesRow.AddComponent<HorizontalLayoutGroup>();
            togglesLayout.childAlignment = TextAnchor.MiddleCenter;
            togglesLayout.spacing = 40;
            togglesLayout.childForceExpandWidth = false;
            togglesLayout.childControlWidth = false;

            var rememberToggle = CreateToggle(togglesRow.transform, "RememberToggle", "저장");
            var autoToggle = CreateToggle(togglesRow.transform, "AutoSignInToggle", "자동 로그인");

            var status = CreateText(panel.transform, "StatusText", "", 22, TextAlignmentOptions.Center);
            SetAnchored(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(900, 80));

            var row1 = new GameObject("ButtonsRow1", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row1, "Create row1");
            row1.transform.SetParent(panel.transform, false);
            var row1Rt = row1.GetComponent<RectTransform>();
            SetAnchored(row1Rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -350), new Vector2(900, 60));
            var row1Layout = row1.AddComponent<HorizontalLayoutGroup>();
            row1Layout.childAlignment = TextAnchor.MiddleCenter;
            row1Layout.spacing = 20;
            row1Layout.childForceExpandHeight = true;
            row1Layout.childForceExpandWidth = false;
            row1Layout.childControlWidth = false;

            var row2 = new GameObject("ButtonsRow2", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row2, "Create row2");
            row2.transform.SetParent(panel.transform, false);
            var row2Rt = row2.GetComponent<RectTransform>();
            SetAnchored(row2Rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -430), new Vector2(900, 60));
            var row2Layout = row2.AddComponent<HorizontalLayoutGroup>();
            row2Layout.childAlignment = TextAnchor.MiddleCenter;
            row2Layout.spacing = 20;
            row2Layout.childForceExpandHeight = true;
            row2Layout.childForceExpandWidth = false;
            row2Layout.childControlWidth = false;

            var signUpBtn = CreateButton(row1.transform, "SignUpButton", "Sign Up");
            var signInBtn = CreateButton(row1.transform, "SignInButton", "Sign In");
            var sendMailBtn = CreateButton(row2.transform, "SendVerificationButton", "Send Verification");
            var signOutBtn = CreateButton(panel.transform, "SignOutButton", "Sign Out");
            SetAnchored(signOutBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(260, 50));

            var controllerGo = new GameObject("StartSceneEmailAuthController");
            Undo.RegisterCreatedObjectUndo(controllerGo, "Create StartSceneEmailAuthController");
            controllerGo.transform.SetParent(root.transform, false);
            var controller = controllerGo.AddComponent<StartSceneEmailAuth>();

            // Wire references
            var emailInput = email.GetComponent<TMP_InputField>();
            var passInput = pass.GetComponent<TMP_InputField>();
            SetPrivateField(controller, "emailInput", emailInput);
            SetPrivateField(controller, "passwordInput", passInput);
            SetPrivateField(controller, "statusText", status);

            // Tab/Enter navigation
            var nav = root.AddComponent<UiInputFieldTabNavigator>();
            nav.SetFieldsAndSubmitTarget(new System.Collections.Generic.List<TMP_InputField> { emailInput, passInput }, controller);
            EditorUtility.SetDirty(nav);

            // Toggles -> controller
            rememberToggle.isOn = true;
            autoToggle.isOn = false;
            UnityEventTools.AddPersistentListener(rememberToggle.onValueChanged, controller.SetRememberCredentials);
            UnityEventTools.AddPersistentListener(autoToggle.onValueChanged, controller.SetAutoSignInOnStart);
            EditorUtility.SetDirty(rememberToggle);
            EditorUtility.SetDirty(autoToggle);

            // DemoScene default is "DemoScene" in script. Leave as-is.

            // Wire button events (persistent so it survives Play/reload)
            WirePersistent(signUpBtn, controller, controller.OnClickSignUp);
            WirePersistent(signInBtn, controller, controller.OnClickSignIn);
            WirePersistent(sendMailBtn, controller, controller.OnClickSendVerificationEmail);
            WirePersistent(signOutBtn, controller, controller.OnClickSignOut);

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[StartSceneEmailAuthUiBuilder] Email auth UI created and wired.");
        }

        private static void WirePersistent(Button button, StartSceneEmailAuth target, UnityEngine.Events.UnityAction call)
        {
            if (button == null || target == null)
                return;

            var evt = button.onClick;
            // Unity 버전에 따라 RemovePersistentListeners API가 없어서 인덱스 제거로 처리합니다.
            for (var i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(evt, i);
            UnityEventTools.AddPersistentListener(evt, call);
            EditorUtility.SetDirty(button);
        }

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es != null)
                return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static Canvas CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static Image CreatePanel(Transform parent)
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panelGo, "Create Panel");
            panelGo.transform.SetParent(parent, false);
            var img = panelGo.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.55f);

            var rt = panelGo.GetComponent<RectTransform>();
            SetAnchored(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 600));
            return img;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, "Create Text");
            go.transform.SetParent(parent, false);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        private static GameObject CreateInput(Transform parent, string name, string placeholder, bool password)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(root, "Create Input");
            root.transform.SetParent(parent, false);
            var img = root.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);

            var textArea = new GameObject("TextArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textArea, "Create TextArea");
            textArea.transform.SetParent(root.transform, false);
            var textAreaRt = textArea.GetComponent<RectTransform>();
            SetAnchored(textAreaRt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20, -20));
            textAreaRt.offsetMin = new Vector2(14, 10);
            textAreaRt.offsetMax = new Vector2(-14, -10);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(placeholderGo, "Create Placeholder");
            placeholderGo.transform.SetParent(textArea.transform, false);
            var ph = placeholderGo.GetComponent<TextMeshProUGUI>();
            ph.text = placeholder;
            ph.fontSize = 26;
            ph.color = new Color(1f, 1f, 1f, 0.4f);
            ph.alignment = TextAlignmentOptions.Left;
            SetAnchored(ph.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(textGo, "Create InputText");
            textGo.transform.SetParent(textArea.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            SetAnchored(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var input = root.AddComponent<TMP_InputField>();
            input.textViewport = textAreaRt;
            input.textComponent = text;
            input.placeholder = ph;
            if (password)
                input.contentType = TMP_InputField.ContentType.Password;
            else
                input.contentType = TMP_InputField.ContentType.EmailAddress;

            return root;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Button");
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.14f);

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var text = CreateText(go.transform, "Text", label, 24, TextAlignmentOptions.Center);
            SetAnchored(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 60);

            // 고정 폭(레이아웃 그룹 내에서도 적용되도록 LayoutElement 사용)
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 200;
            le.minWidth = 200;
            le.flexibleWidth = 0;
            return btn;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            Undo.RegisterCreatedObjectUndo(root, "Create Toggle");
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 40);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(bgGo, "Create Toggle BG");
            bgGo.transform.SetParent(root.transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.12f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = new Vector2(16, 0);
            bgRt.sizeDelta = new Vector2(26, 26);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(checkGo, "Create Toggle Check");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImg = checkGo.GetComponent<Image>();
            checkImg.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            SetAnchored(checkGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var text = CreateText(root.transform, "Label", label, 22, TextAlignmentOptions.Left);
            text.color = Color.white;
            var textRt = text.rectTransform;
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = new Vector2(52, 0);
            textRt.offsetMax = new Vector2(0, 0);

            var t = root.GetComponent<Toggle>();
            t.targetGraphic = bgImg;
            t.graphic = checkImg;

            return t;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var t = target.GetType();
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning($"[StartSceneEmailAuthUiBuilder] Field not found: {t.Name}.{fieldName}");
                return;
            }
            f.SetValue(target, value);
            EditorUtility.SetDirty((UnityEngine.Object)target);
        }
    }
}

