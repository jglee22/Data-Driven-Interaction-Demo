using DataDrivenDemo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.EditorTools
{
    /// <summary>메이플식 전체 퀘스트 창(좌 스크롤 / 우 상세) 골격. HUD 트래커는 QuestTrackerListView maxVisible 로 제한.</summary>
    public static class QuestJournalUiBuilder
    {
        private const string RootName = "QuestJournal";

        [MenuItem("Tools/DataDrivenDemo/Build Quest Journal UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestJournalUiBuilder] 활성 씬이 없습니다.");
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[QuestJournalUiBuilder] Canvas 를 찾을 수 없습니다.");
                return;
            }

            if (Object.FindFirstObjectByType<QuestJournalView>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[QuestJournalUiBuilder] 이미 QuestJournal 가 있습니다.");
                return;
            }

            var hud = Object.FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);

            var rootGo = new GameObject(RootName, typeof(RectTransform), typeof(QuestJournalView));
            Undo.RegisterCreatedObjectUndo(rootGo, "Create QuestJournal");
            rootGo.transform.SetParent(canvas.transform, false);
            rootGo.SetActive(false);

            var rootRt = rootGo.GetComponent<RectTransform>();
            StretchFull(rootRt);

            var journal = rootGo.GetComponent<QuestJournalView>();

            // 배경(닫기)
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(QuestJournalBackdropCloser));
            Undo.RegisterCreatedObjectUndo(backdropGo, "Create Journal Backdrop");
            backdropGo.transform.SetParent(rootGo.transform, false);
            var backdropRt = backdropGo.GetComponent<RectTransform>();
            StretchFull(backdropRt);
            var backdropImg = backdropGo.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.55f);

            // 중앙 패널
            var modalGo = new GameObject("Modal", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(modalGo, "Create Journal Modal");
            modalGo.transform.SetParent(rootGo.transform, false);
            var modalRt = modalGo.GetComponent<RectTransform>();
            modalRt.anchorMin = modalRt.anchorMax = modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.sizeDelta = new Vector2(920f, 560f);
            modalRt.anchoredPosition = Vector2.zero;
            var modalImg = modalGo.GetComponent<Image>();
            modalImg.color = new Color(0.06f, 0.06f, 0.08f, 0.96f);
            modalImg.raycastTarget = true;

            var modalVlg = modalGo.GetComponent<VerticalLayoutGroup>();
            modalVlg.padding = new RectOffset(24, 24, 20, 20);
            modalVlg.spacing = 16;
            modalVlg.childAlignment = TextAnchor.UpperLeft;
            modalVlg.childControlHeight = true;
            modalVlg.childControlWidth = true;
            modalVlg.childForceExpandHeight = true;
            modalVlg.childForceExpandWidth = true;

            var modalLe = modalGo.GetComponent<LayoutElement>();
            modalLe.minHeight = 520f;
            modalLe.preferredWidth = 920f;

            var headerGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(headerGo, "Create Journal Title");
            headerGo.transform.SetParent(modalGo.transform, false);
            StretchTopFullWidth(headerGo.GetComponent<RectTransform>());
            var headerTmp = headerGo.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "QUEST";
            headerTmp.fontSize = 34;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.color = Color.white;
            headerTmp.alignment = TextAlignmentOptions.Left;
            var headerLe = headerGo.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 44f;
            headerLe.minHeight = 44f;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(bodyGo, "Create Journal Body");
            bodyGo.transform.SetParent(modalGo.transform, false);
            StretchTopFullWidth(bodyGo.GetComponent<RectTransform>());
            bodyGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 0f);
            var bodyHlg = bodyGo.GetComponent<HorizontalLayoutGroup>();
            bodyHlg.childAlignment = TextAnchor.UpperLeft;
            bodyHlg.spacing = 20;
            bodyHlg.childControlHeight = true;
            bodyHlg.childControlWidth = true;
            bodyHlg.childForceExpandHeight = true;
            bodyHlg.childForceExpandWidth = true;
            var bodyLe = bodyGo.GetComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;
            bodyLe.flexibleWidth = 1f;
            bodyLe.preferredHeight = 420f;

            // ── 좌: 스크롤 (Content 에 CSF — 저널용)
            var scrollArea = new GameObject("ScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(scrollArea, "Create Journal Scroll");
            scrollArea.transform.SetParent(bodyGo.transform, false);
            var scrollAreaRt = scrollArea.GetComponent<RectTransform>();
            scrollAreaRt.sizeDelta = Vector2.zero;
            var scrollAreaImg = scrollArea.GetComponent<Image>();
            scrollAreaImg.color = new Color(1f, 1f, 1f, 0.04f);
            scrollAreaImg.raycastTarget = true;
            var scrollLe = scrollArea.GetComponent<LayoutElement>();
            scrollLe.flexibleWidth = 1.1f;
            scrollLe.preferredWidth = 420f;
            scrollLe.flexibleHeight = 1f;
            scrollLe.preferredHeight = 400f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            Undo.RegisterCreatedObjectUndo(viewportGo, "Create Journal Viewport");
            viewportGo.transform.SetParent(scrollArea.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(contentGo, "Create Journal Scroll Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var contentVlg = contentGo.GetComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(8, 8, 8, 8);
            contentVlg.spacing = 10;
            contentVlg.childAlignment = TextAnchor.UpperLeft;
            contentVlg.childControlHeight = true;
            contentVlg.childControlWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childForceExpandWidth = true;
            var contentCsf = contentGo.GetComponent<ContentSizeFitter>();
            contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var journalRowPrefab = BuildJournalRowTemplate(contentGo.transform);

            // ── 우: 상세
            var detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(detailPanel, "Create Journal Detail");
            detailPanel.transform.SetParent(bodyGo.transform, false);
            var detailPanelRt = detailPanel.GetComponent<RectTransform>();
            detailPanelRt.sizeDelta = Vector2.zero;
            var detailVlg = detailPanel.GetComponent<VerticalLayoutGroup>();
            detailVlg.padding = new RectOffset(14, 14, 8, 8);
            detailVlg.spacing = 12;
            detailVlg.childAlignment = TextAnchor.UpperLeft;
            detailVlg.childControlHeight = true;
            detailVlg.childControlWidth = true;
            detailVlg.childForceExpandHeight = false;
            detailVlg.childForceExpandWidth = true;

            var detailPanelLe = detailPanel.GetComponent<LayoutElement>();
            detailPanelLe.flexibleWidth = 1f;
            detailPanelLe.preferredWidth = 440f;
            detailPanelLe.flexibleHeight = 1f;

            var detTitleGo = new GameObject("DetailTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(detTitleGo, "Create Detail Title");
            detTitleGo.transform.SetParent(detailPanel.transform, false);
            StretchTopFullWidth(detTitleGo.GetComponent<RectTransform>());
            var detTitle = detTitleGo.GetComponent<TextMeshProUGUI>();
            detTitle.fontSize = 26;
            detTitle.fontStyle = FontStyles.Bold;
            detTitle.color = new Color(1f, 0.92f, 0.6f, 1f);
            detTitle.alignment = TextAlignmentOptions.TopLeft;
            detTitle.textWrappingMode = TextWrappingModes.Normal;
            var detTitleLe = detTitleGo.GetComponent<LayoutElement>();
            detTitleLe.preferredHeight = 36f;
            detTitleLe.minHeight = 28f;

            var detBodyGo = new GameObject("DetailBody", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(detBodyGo, "Create Detail Body");
            detBodyGo.transform.SetParent(detailPanel.transform, false);
            StretchTopFullWidth(detBodyGo.GetComponent<RectTransform>());
            var detBody = detBodyGo.GetComponent<TextMeshProUGUI>();
            detBody.fontSize = 22;
            detBody.color = new Color(1f, 1f, 1f, 0.92f);
            detBody.alignment = TextAlignmentOptions.TopLeft;
            detBody.textWrappingMode = TextWrappingModes.Normal;
            var detBodyLe = detBodyGo.GetComponent<LayoutElement>();
            detBodyLe.flexibleHeight = 1f;
            detBodyLe.minHeight = 120f;

            // 포기(삭제) 버튼
            var abandonGo = new GameObject("AbandonButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(abandonGo, "Create Abandon Button");
            abandonGo.transform.SetParent(detailPanel.transform, false);
            StretchTopFullWidth(abandonGo.GetComponent<RectTransform>());
            var abandonImg = abandonGo.GetComponent<Image>();
            abandonImg.color = new Color(0.8f, 0.22f, 0.22f, 0.85f);
            var abandonBtn = abandonGo.GetComponent<Button>();
            abandonBtn.transition = Selectable.Transition.ColorTint;
            var abandonLe = abandonGo.GetComponent<LayoutElement>();
            abandonLe.preferredHeight = 44f;
            abandonLe.minHeight = 44f;

            var abandonLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(abandonLabelGo, "Create Abandon Label");
            abandonLabelGo.transform.SetParent(abandonGo.transform, false);
            var abandonLabelRt = abandonLabelGo.GetComponent<RectTransform>();
            abandonLabelRt.anchorMin = Vector2.zero;
            abandonLabelRt.anchorMax = Vector2.one;
            abandonLabelRt.offsetMin = new Vector2(12, 6);
            abandonLabelRt.offsetMax = new Vector2(-12, -6);
            var abandonTmp = abandonLabelGo.GetComponent<TextMeshProUGUI>();
            abandonTmp.text = "포기 (삭제)";
            abandonTmp.fontSize = 22;
            abandonTmp.color = Color.white;
            abandonTmp.alignment = TextAlignmentOptions.Center;

            // 확인 팝업(모달 내부)
            var confirmRoot = new GameObject("ConfirmDialog", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(confirmRoot, "Create Confirm Dialog");
            confirmRoot.transform.SetParent(modalGo.transform, false);
            var confirmRt = confirmRoot.GetComponent<RectTransform>();
            confirmRt.anchorMin = Vector2.zero;
            confirmRt.anchorMax = Vector2.one;
            confirmRt.offsetMin = Vector2.zero;
            confirmRt.offsetMax = Vector2.zero;
            var confirmBg = confirmRoot.GetComponent<Image>();
            confirmBg.color = new Color(0f, 0f, 0f, 0.55f);
            confirmBg.raycastTarget = true;

            var confirmPanel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(confirmPanel, "Create Confirm Panel");
            confirmPanel.transform.SetParent(confirmRoot.transform, false);
            var confirmPanelRt = confirmPanel.GetComponent<RectTransform>();
            confirmPanelRt.anchorMin = confirmPanelRt.anchorMax = confirmPanelRt.pivot = new Vector2(0.5f, 0.5f);
            confirmPanelRt.sizeDelta = new Vector2(520f, 220f);
            confirmPanelRt.anchoredPosition = Vector2.zero;
            var confirmPanelImg = confirmPanel.GetComponent<Image>();
            confirmPanelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
            confirmPanelImg.raycastTarget = true;

            var confirmVlg = confirmPanel.GetComponent<VerticalLayoutGroup>();
            confirmVlg.padding = new RectOffset(18, 18, 16, 16);
            confirmVlg.spacing = 16;
            confirmVlg.childAlignment = TextAnchor.UpperLeft;
            confirmVlg.childControlHeight = true;
            confirmVlg.childControlWidth = true;
            confirmVlg.childForceExpandHeight = false;
            confirmVlg.childForceExpandWidth = true;

            var confirmTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(confirmTextGo, "Create Confirm Text");
            confirmTextGo.transform.SetParent(confirmPanel.transform, false);
            StretchTopFullWidth(confirmTextGo.GetComponent<RectTransform>());
            var confirmTmp = confirmTextGo.GetComponent<TextMeshProUGUI>();
            confirmTmp.text = "이 퀘스트를 포기하고 삭제할까요?";
            confirmTmp.fontSize = 22;
            confirmTmp.color = new Color(1f, 1f, 1f, 0.92f);
            confirmTmp.alignment = TextAlignmentOptions.TopLeft;
            confirmTmp.textWrappingMode = TextWrappingModes.Normal;
            confirmTextGo.GetComponent<LayoutElement>().preferredHeight = 96f;

            var buttonsRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(buttonsRow, "Create Confirm Buttons");
            buttonsRow.transform.SetParent(confirmPanel.transform, false);
            StretchTopFullWidth(buttonsRow.GetComponent<RectTransform>());
            var hlg = buttonsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = true;
            buttonsRow.GetComponent<LayoutElement>().preferredHeight = 52f;

            Button MakeBtn(string name, string label, Color bgColor)
            {
                var bGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                Undo.RegisterCreatedObjectUndo(bGo, $"Create {name}");
                bGo.transform.SetParent(buttonsRow.transform, false);
                var img = bGo.GetComponent<Image>();
                img.color = bgColor;
                var le = bGo.GetComponent<LayoutElement>();
                le.preferredHeight = 44f;
                le.minHeight = 44f;
                le.flexibleWidth = 1f;

                var tGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(tGo, $"Create {name} Label");
                tGo.transform.SetParent(bGo.transform, false);
                var rt = tGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 6);
                rt.offsetMax = new Vector2(-10, -6);
                var tmp = tGo.GetComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.fontSize = 20;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;

                return bGo.GetComponent<Button>();
            }

            var noBtn = MakeBtn("NoButton", "취소", new Color(0.35f, 0.35f, 0.4f, 0.9f));
            var yesBtn = MakeBtn("YesButton", "삭제", new Color(0.8f, 0.22f, 0.22f, 0.9f));

            confirmRoot.SetActive(false);

            var emptyGo = new GameObject("EmptyHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(emptyGo, "Create Journal Empty Hint");
            emptyGo.transform.SetParent(detailPanel.transform, false);
            StretchTopFullWidth(emptyGo.GetComponent<RectTransform>());
            var emptyTmp = emptyGo.GetComponent<TextMeshProUGUI>();
            emptyTmp.text = "등록된 퀘스트가 없습니다.";
            emptyTmp.fontSize = 22;
            emptyTmp.color = new Color(1f, 1f, 1f, 0.55f);
            emptyTmp.alignment = TextAlignmentOptions.TopLeft;
            var emptyLe = emptyGo.GetComponent<LayoutElement>();
            emptyLe.preferredHeight = 80f;

            journalRowPrefab.gameObject.SetActive(false);

            SetPrivateField(journal, "scrollContent", contentGo.transform);
            SetPrivateField(journal, "rowPrefab", journalRowPrefab);
            SetPrivateField(journal, "scrollRect", scrollRect);
            SetPrivateField(journal, "detailTitle", detTitle);
            SetPrivateField(journal, "detailBody", detBody);
            SetPrivateField(journal, "abandonButton", abandonBtn);
            SetPrivateField(journal, "confirmRoot", confirmRoot);
            SetPrivateField(journal, "confirmText", confirmTmp);
            SetPrivateField(journal, "confirmYesButton", yesBtn);
            SetPrivateField(journal, "confirmNoButton", noBtn);
            SetPrivateField(journal, "emptyHint", emptyTmp);

            if (hud != null)
                SetPrivateField(hud, "questJournal", journal);

            Selection.activeGameObject = rootGo;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[QuestJournalUiBuilder] 생성 완료. QuestHudView 가 붙은 오브젝트에 버튼 OnClick → QuestHudView.ToggleQuestJournal 을 연결하세요.");
        }

        private static QuestTrackerRowView BuildJournalRowTemplate(Transform scrollContent)
        {
            var rowGo = new GameObject("JournalRowTemplate", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create Journal Row Template");
            rowGo.transform.SetParent(scrollContent, false);

            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(0f, 0f);

            var rowVlg = rowGo.GetComponent<VerticalLayoutGroup>();
            rowVlg.padding = new RectOffset(10, 10, 10, 10);
            rowVlg.spacing = 8;
            rowVlg.childAlignment = TextAnchor.UpperLeft;
            rowVlg.childControlHeight = true;
            rowVlg.childControlWidth = true;
            rowVlg.childForceExpandHeight = false;
            rowVlg.childForceExpandWidth = true;

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(1f, 1f, 1f, 0.07f);
            rowBg.raycastTarget = true;

            rowGo.AddComponent<LayoutElement>().minHeight = 56f;

            var rowView = rowGo.AddComponent<QuestTrackerRowView>();

            var rtTitleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(rtTitleGo, "Journal Row Title");
            rtTitleGo.transform.SetParent(rowGo.transform, false);
            StretchTopFullWidth(rtTitleGo.GetComponent<RectTransform>());
            var rtTitle = rtTitleGo.GetComponent<TextMeshProUGUI>();
            rtTitle.text = "Quest Title";
            rtTitle.fontSize = 22;
            rtTitle.color = new Color(1f, 0.92f, 0.6f, 1f);
            rtTitle.alignment = TextAlignmentOptions.TopLeft;
            rtTitle.textWrappingMode = TextWrappingModes.Normal;
            rtTitle.overflowMode = TextOverflowModes.Truncate;
            var rtTitleLe = rtTitleGo.AddComponent<LayoutElement>();
            rtTitleLe.preferredHeight = 30f;
            rtTitleLe.minHeight = 24f;

            var rtDetailGo = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(rtDetailGo, "Journal Row Detail");
            rtDetailGo.transform.SetParent(rowGo.transform, false);
            StretchTopFullWidth(rtDetailGo.GetComponent<RectTransform>());
            var rtDetail = rtDetailGo.GetComponent<TextMeshProUGUI>();
            rtDetail.text = "Detail";
            rtDetail.fontSize = 18;
            rtDetail.color = new Color(1f, 1f, 1f, 0.88f);
            rtDetail.alignment = TextAlignmentOptions.TopLeft;
            rtDetail.textWrappingMode = TextWrappingModes.Normal;
            var rtdCsf = rtDetailGo.GetComponent<ContentSizeFitter>();
            rtdCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rtdCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rtDetailGo.AddComponent<LayoutElement>().minHeight = 22f;

            SetPrivateField(rowView, "titleText", rtTitle);
            SetPrivateField(rowView, "detailText", rtDetail);

            return rowView;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchTopFullWidth(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var t = target.GetType();
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning($"[QuestJournalUiBuilder] Field not found: {t.Name}.{fieldName}");
                return;
            }
            f.SetValue(target, value);
            EditorUtility.SetDirty((Object)target);
        }
    }
}
