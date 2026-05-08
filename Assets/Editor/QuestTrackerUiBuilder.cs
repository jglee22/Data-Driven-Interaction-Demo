using DataDrivenDemo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.EditorTools
{
    public static class QuestTrackerUiBuilder
    {
        [MenuItem("Tools/DataDrivenDemo/Build Quest Tracker UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[QuestTrackerUiBuilder] No active scene loaded.");
                return;
            }

            var hud = Object.FindFirstObjectByType<QuestHudView>(FindObjectsInactive.Include);
            if (hud == null)
            {
                Debug.LogWarning("[QuestTrackerUiBuilder] QuestHudView not found in scene.");
                return;
            }

            var hudGo = hud.gameObject;

            var trackerRoot = new GameObject("QuestTracker", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(trackerRoot, "Create QuestTracker");
            trackerRoot.transform.SetParent(hudGo.transform, false);
            var rootRt = trackerRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = new Vector2(30, -30);
            // 가로 고정, 세로는 자식(제목+리스트) 합에 맞춰 CSF가 늘림
            rootRt.sizeDelta = new Vector2(520f, 0f);

            var rootVlg = trackerRoot.GetComponent<VerticalLayoutGroup>();
            rootVlg.padding = new RectOffset(16, 16, 12, 16);
            rootVlg.spacing = 8;
            rootVlg.childAlignment = TextAnchor.UpperLeft;
            rootVlg.childControlHeight = true;
            rootVlg.childControlWidth = true;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childForceExpandWidth = true;

            var rootCsf = trackerRoot.GetComponent<ContentSizeFitter>();
            rootCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var bg = trackerRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.35f);

            var titleGo = new GameObject("TrackerTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(titleGo, "Create TrackerTitle");
            titleGo.transform.SetParent(trackerRoot.transform, false);
            var title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "퀘스트";
            title.fontSize = 26;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Left;
            var titleRt = titleGo.GetComponent<RectTransform>();
            StretchTopFullWidth(titleRt);
            titleRt.sizeDelta = new Vector2(0f, 36f);
            var titleLe = titleGo.GetComponent<LayoutElement>();
            titleLe.minHeight = 36f;
            titleLe.preferredHeight = 36f;
            titleLe.flexibleHeight = 0f;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            Undo.RegisterCreatedObjectUndo(contentGo, "Create TrackerContent");
            contentGo.transform.SetParent(trackerRoot.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            // 부모를 꽉 채우는 스트레치 제거: 리스트 높이 = 행 합계로 선호 높이 계산
            StretchTopFullWidth(contentRt);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var contentLe = contentGo.GetComponent<LayoutElement>();
            contentLe.flexibleWidth = 1f;
            contentLe.flexibleHeight = 0f;

            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 8;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;

            // HUD Content: CSF 없이 VLG만(스크롤 없음, 최대 4행). 선호 높이는 자식 VLG가 보고.

            // Row prefab (as disabled child template) — VLG로 제목/상세 세로 배치(겹침 방지)
            var rowGo = new GameObject("RowTemplate", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(rowGo, "Create RowTemplate");
            rowGo.transform.SetParent(contentGo.transform, false);
            rowGo.SetActive(false);

            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(0f, 0f);

            var rowVlg = rowGo.GetComponent<VerticalLayoutGroup>();
            rowVlg.padding = new RectOffset(12, 12, 8, 10);
            rowVlg.spacing = 6;
            rowVlg.childAlignment = TextAnchor.UpperLeft;
            rowVlg.childControlHeight = true;
            rowVlg.childControlWidth = true;
            rowVlg.childForceExpandHeight = false;
            rowVlg.childForceExpandWidth = true;

            var rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(1f, 1f, 1f, 0.06f);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.minHeight = 56f;
            rowLe.flexibleHeight = 0f;

            var row = rowGo.AddComponent<QuestTrackerRowView>();

            var rowTitleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(rowTitleGo, "Create RowTitle");
            rowTitleGo.transform.SetParent(rowGo.transform, false);
            StretchTopFullWidth(rowTitleGo.GetComponent<RectTransform>());
            var rowTitle = rowTitleGo.GetComponent<TextMeshProUGUI>();
            rowTitle.text = "Quest Title";
            rowTitle.fontSize = 22;
            rowTitle.color = new Color(1f, 0.92f, 0.6f, 1f);
            rowTitle.alignment = TextAlignmentOptions.TopLeft;
            rowTitle.textWrappingMode = TextWrappingModes.Normal;
            rowTitle.overflowMode = TextOverflowModes.Truncate;

            var rowTitleLe = rowTitleGo.AddComponent<LayoutElement>();
            rowTitleLe.minHeight = 24f;
            rowTitleLe.preferredHeight = 28f;
            rowTitleLe.flexibleHeight = 0f;

            var rowDetailGo = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(rowDetailGo, "Create RowDetail");
            rowDetailGo.transform.SetParent(rowGo.transform, false);
            StretchTopFullWidth(rowDetailGo.GetComponent<RectTransform>());

            var rowDetail = rowDetailGo.GetComponent<TextMeshProUGUI>();
            rowDetail.text = "Detail";
            rowDetail.fontSize = 18;
            rowDetail.color = new Color(1f, 1f, 1f, 0.85f);
            rowDetail.alignment = TextAlignmentOptions.TopLeft;
            rowDetail.textWrappingMode = TextWrappingModes.Normal;

            var detailCsf = rowDetailGo.GetComponent<ContentSizeFitter>();
            // 가로 폭은 부모 VLG 스트레치만 쓰고, 세로는 줄바꿈된 텍스트 기준 높이
            detailCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            detailCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var detailLe = rowDetailGo.AddComponent<LayoutElement>();
            detailLe.minHeight = 22f;
            detailLe.flexibleHeight = 0f;

            // Hook up row view serialized fields by name via reflection (keep builder self-contained)
            SetPrivateField(row, "titleText", rowTitle);
            SetPrivateField(row, "detailText", rowDetail);

            var list = trackerRoot.AddComponent<QuestTrackerListView>();
            list.SetMaxVisible(4);
            SetPrivateField(list, "contentRoot", contentGo.transform);
            SetPrivateField(list, "rowPrefab", row);

            // Hook list into QuestHudView
            SetPrivateField(hud, "trackerList", list);

            Selection.activeGameObject = trackerRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[QuestTrackerUiBuilder] Tracker UI created and wired to QuestHudView.");
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
                Debug.LogWarning($"[QuestTrackerUiBuilder] Field not found: {t.Name}.{fieldName}");
                return;
            }
            f.SetValue(target, value);
            EditorUtility.SetDirty((Object)target);
        }
    }
}

