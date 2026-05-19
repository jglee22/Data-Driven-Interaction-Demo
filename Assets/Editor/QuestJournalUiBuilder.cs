using DataDrivenDemo.Interaction;
using DataDrivenDemo.Quest;
using DataDrivenDemo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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

            // 퀘스트 포기 버튼
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
            abandonTmp.text = "퀘스트 포기";
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

    /// <summary>
    /// 의뢰 목록 UI + 의뢰 NPC 자동 연결.
    /// NPC·의뢰 id는 <see cref="QuestOfferWireSettings"/> 또는 <see cref="QuestOfferWireResolver"/>가 결정합니다.
    /// </summary>
    public static class QuestOfferUiBuilder
    {
        private const string OfferRootName = "QuestOffer";

        public static bool TryBuildOfferUi(out QuestOfferView offerView)
        {
            offerView = null;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestOfferUiBuilder] 활성 씬이 없습니다.");
                return false;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("[QuestOfferUiBuilder] Canvas 를 찾을 수 없습니다.");
                return false;
            }

            if (canvas.gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
                EditorUtility.SetDirty(canvas.gameObject);
            }

            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            {
                Debug.LogWarning(
                    "[QuestOfferUiBuilder] 씬에 EventSystem 이 없습니다. 수락/버튼 클릭이 안 될 수 있으니 Hierarchy 에 GameObject > UI > Event System 을 추가하세요.");
            }

            if (Object.FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include) != null)
            {
                Debug.LogWarning("[QuestOfferUiBuilder] 이미 QuestOffer 가 있습니다.");
                offerView = Object.FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include);
                return false;
            }

            var rootGo = new GameObject(OfferRootName, typeof(RectTransform), typeof(QuestOfferView));
            Undo.RegisterCreatedObjectUndo(rootGo, "Create QuestOffer");
            rootGo.transform.SetParent(canvas.transform, false);
            rootGo.SetActive(false);

            var rootRt = rootGo.GetComponent<RectTransform>();
            OfferStretchFull(rootRt);

            var offer = rootGo.GetComponent<QuestOfferView>();

            var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(QuestOfferBackdropCloser));
            Undo.RegisterCreatedObjectUndo(backdropGo, "Offer Backdrop");
            backdropGo.transform.SetParent(rootGo.transform, false);
            OfferStretchFull(backdropGo.GetComponent<RectTransform>());
            var backdropImg = backdropGo.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.5f);

            var modalGo = new GameObject("Modal", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(modalGo, "Offer Modal");
            modalGo.transform.SetParent(rootGo.transform, false);
            var modalRt = modalGo.GetComponent<RectTransform>();
            modalRt.anchorMin = modalRt.anchorMax = modalRt.pivot = new Vector2(0.5f, 0.5f);
            modalRt.sizeDelta = new Vector2(520f, 560f);
            modalRt.anchoredPosition = Vector2.zero;

            var modalImg = modalGo.GetComponent<Image>();
            modalImg.color = new Color(0.06f, 0.06f, 0.09f, 0.96f);
            modalImg.raycastTarget = true;

            var modalVlg = modalGo.GetComponent<VerticalLayoutGroup>();
            modalVlg.padding = new RectOffset(18, 18, 14, 16);
            modalVlg.spacing = 14;
            modalVlg.childAlignment = TextAnchor.UpperLeft;
            modalVlg.childControlHeight = true;
            modalVlg.childControlWidth = true;
            modalVlg.childForceExpandHeight = false;
            modalVlg.childForceExpandWidth = true;

            var headerGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(headerGo, "Offer Title");
            headerGo.transform.SetParent(modalGo.transform, false);
            OfferStretchTopFullWidth(headerGo.GetComponent<RectTransform>());
            var headerTmp = headerGo.GetComponent<TextMeshProUGUI>();
            headerTmp.text = "\uC758\uB8B0 \uBAA9\uB85D";
            headerTmp.fontSize = 26;
            headerTmp.fontStyle = FontStyles.Bold;
            headerTmp.color = Color.white;
            headerTmp.alignment = TextAlignmentOptions.Left;
            headerGo.GetComponent<LayoutElement>().preferredHeight = 36f;

            var scrollArea = new GameObject("ScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(scrollArea, "Offer Scroll");
            scrollArea.transform.SetParent(modalGo.transform, false);
            var scrollAreaRt = scrollArea.GetComponent<RectTransform>();
            scrollAreaRt.sizeDelta = Vector2.zero;
            scrollArea.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);
            scrollArea.GetComponent<Image>().raycastTarget = true;

            var scrollLe = scrollArea.GetComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;
            scrollLe.preferredHeight = 240f;
            scrollLe.minHeight = 180f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            Undo.RegisterCreatedObjectUndo(viewportGo, "Offer Viewport");
            viewportGo.transform.SetParent(scrollArea.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            OfferStretchFull(viewportRt);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(contentGo, "Offer Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            var contentVlg = contentGo.GetComponent<VerticalLayoutGroup>();
            contentVlg.padding = new RectOffset(6, 6, 6, 6);
            contentVlg.spacing = 10;
            contentVlg.childAlignment = TextAnchor.UpperLeft;
            contentVlg.childControlHeight = true;
            contentVlg.childControlWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childForceExpandWidth = true;
            contentGo.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var rowPrefab = BuildOfferRowTemplate(contentGo.transform);

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(hintGo, "Offer Hint");
            hintGo.transform.SetParent(modalGo.transform, false);
            OfferStretchTopFullWidth(hintGo.GetComponent<RectTransform>());
            var hintTmp = hintGo.GetComponent<TextMeshProUGUI>();
            hintTmp.text = "";
            hintTmp.fontSize = 16;
            hintTmp.color = new Color(1f, 1f, 1f, 0.55f);
            hintTmp.textWrappingMode = TextWrappingModes.Normal;
            hintGo.GetComponent<LayoutElement>().preferredHeight = 24f;

            // 상세 텍스트: Overflow 그대로면 레이아웃 높이를 무시하고 모달 밖으로 그려지므로 Mask + 세로 스크롤 영역 고정.
            var detailScrollArea =
                new GameObject("DetailScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(detailScrollArea, "Offer Detail Scroll");
            detailScrollArea.transform.SetParent(modalGo.transform, false);
            OfferStretchTopFullWidth(detailScrollArea.GetComponent<RectTransform>());
            var detailScrollImg = detailScrollArea.GetComponent<Image>();
            detailScrollImg.color = new Color(1f, 1f, 1f, 0.02f);
            detailScrollImg.raycastTarget = true;

            var detailScrollLe = detailScrollArea.GetComponent<LayoutElement>();
            detailScrollLe.preferredHeight = 152f;
            detailScrollLe.minHeight = 96f;
            detailScrollLe.flexibleHeight = 0f;

            var dViewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            Undo.RegisterCreatedObjectUndo(dViewportGo, "Offer Detail Viewport");
            dViewportGo.transform.SetParent(detailScrollArea.transform, false);
            var dViewportRt = dViewportGo.GetComponent<RectTransform>();
            OfferStretchFull(dViewportRt);
            var dVpImg = dViewportGo.GetComponent<Image>();
            dVpImg.color = new Color(1f, 1f, 1f, 0.001f);
            dVpImg.raycastTarget = false;

            var detailContentGo =
                new GameObject("DetailContent", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(detailContentGo, "Offer Detail Content");
            detailContentGo.transform.SetParent(dViewportGo.transform, false);
            var dContentRt = detailContentGo.GetComponent<RectTransform>();
            dContentRt.anchorMin = new Vector2(0f, 1f);
            dContentRt.anchorMax = new Vector2(1f, 1f);
            dContentRt.pivot = new Vector2(0.5f, 1f);
            dContentRt.anchoredPosition = Vector2.zero;
            dContentRt.sizeDelta = Vector2.zero;

            var dCsf = detailContentGo.GetComponent<ContentSizeFitter>();
            dCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            dCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var detailTmp = detailContentGo.GetComponent<TextMeshProUGUI>();
            detailTmp.text = "";
            detailTmp.fontSize = 18;
            detailTmp.color = new Color(1f, 1f, 1f, 0.9f);
            detailTmp.textWrappingMode = TextWrappingModes.Normal;
            detailTmp.overflowMode = TextOverflowModes.Overflow;
            detailTmp.margin = new Vector4(4f, 2f, 4f, 6f);
            detailTmp.richText = true;

            var dScrollRect = detailScrollArea.GetComponent<ScrollRect>();
            dScrollRect.viewport = dViewportRt;
            dScrollRect.content = dContentRt;
            dScrollRect.horizontal = false;
            dScrollRect.vertical = true;
            dScrollRect.movementType = ScrollRect.MovementType.Clamped;
            dScrollRect.scrollSensitivity = 24f;

            var btnRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(btnRow, "Offer Buttons Row");
            btnRow.transform.SetParent(modalGo.transform, false);
            OfferStretchTopFullWidth(btnRow.GetComponent<RectTransform>());
            var offerBtnHlg = btnRow.GetComponent<HorizontalLayoutGroup>();
            offerBtnHlg.childAlignment = TextAnchor.MiddleRight;
            offerBtnHlg.spacing = 14;
            offerBtnHlg.childForceExpandHeight = false;
            offerBtnHlg.childForceExpandWidth = false;
            offerBtnHlg.childControlHeight = true;
            offerBtnHlg.childControlWidth = true;
            btnRow.GetComponent<LayoutElement>().preferredHeight = 48f;

            var acceptBtn = BuildOfferTextButton(btnRow.transform, "AcceptBtn", "\uC218\uB77D");
            var closeBtn = BuildOfferTextButton(btnRow.transform, "CloseBtn", "\uB2EB\uAE30");

            OfferSetPrivateField(offer, "panelRoot", rootGo);
            OfferSetPrivateField(offer, "scrollContent", contentGo.transform);
            OfferSetPrivateField(offer, "rowPrefab", rowPrefab);
            OfferSetPrivateField(offer, "detailText", detailTmp);
            OfferSetPrivateField(offer, "hintText", hintTmp);
            OfferSetPrivateField(offer, "acceptButton", acceptBtn);
            OfferSetPrivateField(offer, "closeButton", closeBtn);

            Selection.activeGameObject = rootGo;
            EditorSceneManager.MarkSceneDirty(scene);
            offerView = offer;
            Debug.Log("[QuestOfferUiBuilder] QuestOffer UI 생성 완료.");
            return true;
        }

        [MenuItem("Tools/DataDrivenDemo/Build Quest Offer UI")]
        [MenuItem("DataDrivenDemo/Build Quest Offer UI", false, 1)]
        private static void BuildOfferMenuItem()
        {
            TryBuildOfferUi(out _);
        }

        [MenuItem("Tools/DataDrivenDemo/Wire Quest Offer + Quest Giver")]
        [MenuItem("DataDrivenDemo/Wire Quest Offer + Quest Giver", false, 2)]
        private static void WireOfferAndQuestGiverNpc()
        {
            QuestOfferWireResolver.TryFindSettings(out var wireSettings);
            QuestOfferWireResolver.ResolveNpcIds(wireSettings, out var questGiverNpcId, out var questGiverSpawnTemplateNpcId);
            var offeredQuestIds = QuestOfferWireResolver.ResolveOfferedQuestIds(wireSettings);

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestOfferUiBuilder] Wire: 활성 씬이 없습니다.");
                return;
            }

            QuestOfferView offer =
                Object.FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include);
            if (offer == null)
            {
                TryBuildOfferUi(out offer);
                offer = Object.FindFirstObjectByType<QuestOfferView>(FindObjectsInactive.Include);
            }

            if (offer == null)
            {
                Debug.LogWarning("[QuestOfferUiBuilder] Wire: QuestOfferView 를 만들 수 없습니다.");
                return;
            }

            // 1) 이미 동일 id QuestGiver 가 있으면 연결만 갱신
            foreach (var existing in Object.FindObjectsByType<QuestGiverInteractable>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (existing == null || !string.Equals(existing.Id, questGiverNpcId, System.StringComparison.Ordinal))
                    continue;

                ApplyQuestGiverSerialized(existing, offer, offeredQuestIds, questGiverNpcId);
                EditorUtility.SetDirty(existing.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                DisableQuestDebugAcceptShortcuts();
                Selection.activeGameObject = existing.gameObject;
                Debug.Log($"[QuestOfferUiBuilder] QuestGiver({questGiverNpcId}) 가 이미 있어 Offer 연결만 갱신했습니다.");
                return;
            }

            // 2) 동일 id NpcInteractable → QuestGiver 로 교체
            foreach (var npc in Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (npc == null || !string.Equals(npc.Id, questGiverNpcId, System.StringComparison.Ordinal))
                    continue;

                var go = npc.gameObject;
                Undo.DestroyObjectImmediate(npc);
                var giver = Undo.AddComponent<QuestGiverInteractable>(go);
                ApplyQuestGiverSerialized(giver, offer, offeredQuestIds, questGiverNpcId);
                EditorUtility.SetDirty(go);
                EditorSceneManager.MarkSceneDirty(scene);
                DisableQuestDebugAcceptShortcuts();
                Selection.activeGameObject = go;
                Debug.Log($"[QuestOfferUiBuilder] NpcInteractable({questGiverNpcId}) 을 QuestGiver 로 바꿨습니다.");
                return;
            }

            // 3) 의뢰 NPC 가 없으면 템플릿 Npc 를 복제해 생성
            var talkRoot = FindGameObjectWithSerializedInteractableId(questGiverSpawnTemplateNpcId);
            if (talkRoot == null)
            {
                Debug.LogWarning(
                    $"[QuestOfferUiBuilder] Wire: id 가 {questGiverSpawnTemplateNpcId} 인 Interactable 이 씬에 없습니다.");
                return;
            }

            if (talkRoot.GetComponents<QuestGiverInteractable>().Length > 0)
            {
                RestoreTalkNpcOnGuideObject(talkRoot, questGiverSpawnTemplateNpcId);
                EditorUtility.SetDirty(talkRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(
                    $"[QuestOfferUiBuilder] 템플릿 NPC({questGiverSpawnTemplateNpcId}) 에서 QuestGiverInteractable 을 제거하고 NpcInteractable(Talk) 을 복구했습니다.");
            }

            var templateNpc = FindNpcInteractableBySerializedId(questGiverSpawnTemplateNpcId);
            if (templateNpc == null)
            {
                Debug.LogWarning(
                    $"[QuestOfferUiBuilder] Wire: {questGiverSpawnTemplateNpcId} 복구 후에도 NpcInteractable 을 찾지 못했습니다.");
                return;
            }

            var srcGo = templateNpc.gameObject;
            var dup = Object.Instantiate(srcGo);
            Undo.RegisterCreatedObjectUndo(dup, $"Create Quest Giver {questGiverNpcId}");
            dup.name = $"NPC_QuestGiver_{SanitizeForObjectName(questGiverNpcId)}";
            dup.transform.SetParent(srcGo.transform.parent, true);
            dup.transform.position = srcGo.transform.position + new Vector3(1.75f, 0f, 0.2f);
            dup.transform.rotation = srcGo.transform.rotation;
            dup.transform.localScale = srcGo.transform.localScale;

            var dupNpc = dup.GetComponent<NpcInteractable>();
            if (dupNpc != null)
                Undo.DestroyObjectImmediate(dupNpc);

            var newGiver = Undo.AddComponent<QuestGiverInteractable>(dup);
            ApplyQuestGiverSerialized(newGiver, offer, offeredQuestIds, questGiverNpcId);

            EditorUtility.SetDirty(dup);
            EditorSceneManager.MarkSceneDirty(scene);
            DisableQuestDebugAcceptShortcuts();
            Selection.activeGameObject = dup;
            Debug.Log(
                $"[QuestOfferUiBuilder] {questGiverSpawnTemplateNpcId} 옆에 QuestGiver({questGiverNpcId}) 를 복제 생성했습니다. Talk 목표는 템플릿 NPC 를 사용하세요.");
        }

        private static string SanitizeForObjectName(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
                return "Giver";
            return rawId.Trim().Replace(" ", "_").Replace(".", "_");
        }

        private static NpcInteractable FindNpcInteractableBySerializedId(string npcId)
        {
            foreach (var npc in Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (npc == null)
                    continue;
                var so = new SerializedObject(npc);
                var idProp = so.FindProperty("id");
                var raw = idProp != null ? idProp.stringValue : "";
                if (string.Equals(raw, npcId, System.StringComparison.Ordinal))
                    return npc;
            }

            return null;
        }

        private static GameObject FindGameObjectWithSerializedInteractableId(string rawId)
        {
            foreach (var b in Object.FindObjectsByType<InteractableBase>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (b == null)
                    continue;
                var so = new SerializedObject(b);
                var idProp = so.FindProperty("id");
                var raw = idProp != null ? idProp.stringValue : "";
                if (string.Equals(raw, rawId, System.StringComparison.Ordinal))
                    return b.gameObject;
            }

            return null;
        }

        /// <summary>템플릿 NPC 오브젝트에서 QuestGiver 를 제거하고 Talk용 NpcInteractable id 를 복구합니다.</summary>
        private static void RestoreTalkNpcOnGuideObject(GameObject go, string templateNpcId)
        {
            if (go == null)
                return;

            foreach (var qg in go.GetComponents<QuestGiverInteractable>())
            {
                if (qg != null)
                    Undo.DestroyObjectImmediate(qg);
            }

            var npc = go.GetComponent<NpcInteractable>();
            if (npc == null)
                npc = Undo.AddComponent<NpcInteractable>(go);

            var soNpc = new SerializedObject(npc);
            var idProp = soNpc.FindProperty("id");
            if (idProp != null)
                idProp.stringValue = templateNpcId;
            var dn = soNpc.FindProperty("displayName");
            if (dn != null)
                dn.stringValue = "\uC548\uB0B4 \uC694\uC6D0";
            var aid = soNpc.FindProperty("actionId");
            if (aid != null)
                aid.stringValue = "talk_npc";
            soNpc.ApplyModifiedProperties();
        }

        private static void ApplyQuestGiverSerialized(
            QuestGiverInteractable giver,
            QuestOfferView offer,
            string[] offeredQuestIds,
            string giverNpcId)
        {
            var so = new SerializedObject(giver);
            so.FindProperty("offerView").objectReferenceValue = offer;
            var arr = so.FindProperty("offeredQuestIds");
            arr.ClearArray();
            var ids = offeredQuestIds ?? System.Array.Empty<string>();
            arr.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
                arr.GetArrayElementAtIndex(i).stringValue = ids[i];
            so.ApplyModifiedProperties();

            var soBase = new SerializedObject(giver);
            var idProp = soBase.FindProperty("id");
            if (idProp != null)
                idProp.stringValue = giverNpcId;
            var dn = soBase.FindProperty("displayName");
            if (dn != null)
                dn.stringValue = "\uC758\uB8B0 \uC9C0\uC815";
            soBase.ApplyModifiedProperties();
        }

        private static void DisableQuestDebugAcceptShortcuts()
        {
            foreach (var dbg in Object.FindObjectsByType<QuestDebugAccepter>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (dbg == null)
                    continue;
                var soDbg = new SerializedObject(dbg);
                var p = soDbg.FindProperty("acceptShortcutKeys");
                if (p != null)
                    p.boolValue = false;
                soDbg.ApplyModifiedProperties();
                EditorUtility.SetDirty(dbg);
            }
        }

        private static QuestOfferRowView BuildOfferRowTemplate(Transform parent)
        {
            var rowGo = new GameObject("OfferRowTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(rowGo, "Offer Row Template");
            rowGo.transform.SetParent(parent, false);

            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = Vector2.zero;

            var bg = rowGo.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.08f);
            bg.raycastTarget = true;

            var btn = rowGo.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;

            rowGo.GetComponent<LayoutElement>().minHeight = 52f;

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 8, 8);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(titleGo, "Offer Row Title");
            titleGo.transform.SetParent(rowGo.transform, false);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "";
            titleTmp.fontSize = 22;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.92f, 0.6f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.raycastTarget = false;
            var titleLe = titleGo.GetComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;
            titleLe.preferredHeight = 32f;

            var statusGo = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(statusGo, "Offer Row Status");
            statusGo.transform.SetParent(rowGo.transform, false);
            var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
            statusTmp.text = "";
            statusTmp.fontSize = 18;
            statusTmp.color = new Color(1f, 1f, 1f, 0.75f);
            statusTmp.alignment = TextAlignmentOptions.Right;
            statusTmp.raycastTarget = false;
            var statusLe = statusGo.GetComponent<LayoutElement>();
            statusLe.preferredWidth = 120f;
            statusLe.minWidth = 100f;

            var rowView = rowGo.AddComponent<QuestOfferRowView>();
            OfferSetPrivateField(rowView, "titleText", titleTmp);
            OfferSetPrivateField(rowView, "statusText", statusTmp);
            OfferSetPrivateField(rowView, "button", btn);
            OfferSetPrivateField(rowView, "background", bg);

            rowGo.SetActive(false);
            return rowView;
        }

        private static Button BuildOfferTextButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(go, "Offer Btn " + name);
            go.transform.SetParent(parent, false);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.32f, 0.42f, 0.92f);

            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.preferredWidth = 132f;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, "Offer Btn Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f, 4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return btn;
        }

        private static void OfferStretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void OfferStretchTopFullWidth(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void OfferSetPrivateField(object target, string fieldName, object value)
        {
            var t = target.GetType();
            var f = t.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning($"[QuestOfferUiBuilder] Field missing: {t.Name}.{fieldName}");
                return;
            }

            f.SetValue(target, value);
            if (target is Object uo)
                EditorUtility.SetDirty(uo);
        }
    }

    /// <summary>
    /// 의뢰 UI Wire 메뉴가 사용할 NPC id·의뢰 퀘스트 id 목록.
    /// 비어 있으면 <see cref="QuestOfferWireResolver"/>가 JSON/EditorPrefs로 채웁니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestOfferWireSettings",
        menuName = "DataDrivenDemo/Quest Offer Wire Settings",
        order = 52)]
    public sealed class QuestOfferWireSettings : ScriptableObject
    {
        [Tooltip("QuestGiverInteractable 의 targetId. 비우면 EditorPrefs 또는 기본값.")]
        public string questGiverNpcId;

        [Tooltip("의뢰 NPC가 없을 때 복제할 NpcInteractable 의 id (Talk 전용 NPC). 비우면 EditorPrefs 또는 기본값.")]
        public string questGiverSpawnTemplateNpcId;

        [Tooltip("의뢰 패널에 노출할 퀘스트 id. 비우면 Assets/Data/Json 의 quest_*.json 에서 id를 수집합니다.")]
        public string[] offeredQuestIds;
    }

    /// <summary>
    /// Wire 메뉴용: ScriptableObject → 없으면 JSON 스캔 + EditorPrefs 기본값.
    /// </summary>
    public static class QuestOfferWireResolver
    {
        private const string JsonFolder = "Assets/Data/Json";
        private const string PrefsGiverKey = "DataDrivenDemo.QuestOfferWire.GiverNpcId";
        private const string PrefsTemplateKey = "DataDrivenDemo.QuestOfferWire.TemplateNpcId";

        private const string DefaultGiverNpcId = "npc_010";
        private const string DefaultTemplateNpcId = "npc_001";

        [System.Serializable]
        private class QuestIdStub
        {
            public string id;
        }

        /// <summary>프로젝트에 <see cref="QuestOfferWireSettings"/>가 하나 이상 있으면 경로순 첫 자산.</summary>
        public static bool TryFindSettings(out QuestOfferWireSettings settings)
        {
            settings = null;
            var guids = AssetDatabase.FindAssets("t:QuestOfferWireSettings");
            if (guids == null || guids.Length == 0)
                return false;

            System.Array.Sort(guids, (a, b) => string.Compare(
                AssetDatabase.GUIDToAssetPath(a),
                AssetDatabase.GUIDToAssetPath(b),
                System.StringComparison.Ordinal));

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<QuestOfferWireSettings>(path);
            return settings != null;
        }

        public static void ResolveNpcIds(QuestOfferWireSettings settings, out string giverId, out string templateId)
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.questGiverNpcId))
                giverId = settings.questGiverNpcId.Trim();
            else
                giverId = EditorPrefs.GetString(PrefsGiverKey, DefaultGiverNpcId);

            if (settings != null && !string.IsNullOrWhiteSpace(settings.questGiverSpawnTemplateNpcId))
                templateId = settings.questGiverSpawnTemplateNpcId.Trim();
            else
                templateId = EditorPrefs.GetString(PrefsTemplateKey, DefaultTemplateNpcId);
        }

        /// <summary>설정에 목록이 있으면 그대로, 없으면 JSON에서 id 수집.</summary>
        public static string[] ResolveOfferedQuestIds(QuestOfferWireSettings settings)
        {
            if (settings != null && settings.offeredQuestIds != null && settings.offeredQuestIds.Length > 0)
            {
                var copy = new string[settings.offeredQuestIds.Length];
                System.Array.Copy(settings.offeredQuestIds, copy, copy.Length);
                return copy;
            }

            return DiscoverOfferQuestIdsFromProjectJson();
        }

        /// <summary><c>Assets/Data/Json</c> 아래 TextAsset JSON에서 최상위 <c>id</c>만 읽어 정렬합니다.</summary>
        public static string[] DiscoverOfferQuestIdsFromProjectJson()
        {
            var list = new System.Collections.Generic.List<string>();
            if (!AssetDatabase.IsValidFolder(JsonFolder))
                return System.Array.Empty<string>();

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { JsonFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null || string.IsNullOrWhiteSpace(ta.text))
                    continue;

                var stub = JsonUtility.FromJson<QuestIdStub>(ta.text);
                if (stub == null || string.IsNullOrWhiteSpace(stub.id))
                    continue;
                if (list.Contains(stub.id))
                    continue;
                list.Add(stub.id);
            }

            list.Sort(System.StringComparer.Ordinal);
            return list.ToArray();
        }
    }
}
