using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DataDrivenDemo.UI
{
    [Serializable]
    public sealed class QuestTrackerItem
    {
        public string questId;
        public string title;
        public string detail;
        public QuestTrackerStatus status;
        public long sortKey; // 최근 업데이트 등(클수록 우선)
    }

    [DisallowMultipleComponent]
    public sealed class QuestTrackerListView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private QuestTrackerRowView rowPrefab;
        [Tooltip("비어 있으면 contentRoot 상위에서 ScrollRect 를 찾습니다. 있으면 행 수 제한 없이 스크롤로 전부 표시합니다.")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Behavior")]
        [Min(1)]
        [SerializeField] private int maxVisible = 4;

        [Header("Tracker panel size")]
        [SerializeField] private float minPanelWidth = 160f;
        [SerializeField] private float maxPanelWidth = 560f;
        [Tooltip("스크롤 영역 최대 높이(행이 많을 때).")]
        [SerializeField] private float maxScrollHeight = 420f;

        private readonly List<QuestTrackerRowView> rows = new();
        private LayoutElement scrollAreaLayout;
        private VerticalLayoutGroup rootLayoutGroup;

        private void Awake()
        {
            if (scrollRect == null && contentRoot != null)
                scrollRect = contentRoot.GetComponentInParent<ScrollRect>();
            scrollAreaLayout = scrollRect != null ? scrollRect.GetComponent<LayoutElement>() : null;
            rootLayoutGroup = GetComponent<VerticalLayoutGroup>();
        }

        public void SetMaxVisible(int n) => maxVisible = Mathf.Max(1, n);

        public void Render(IEnumerable<QuestTrackerItem> items)
        {
            if (contentRoot == null || rowPrefab == null)
                return;

            var ordered = (items ?? Array.Empty<QuestTrackerItem>())
                .Where(x => x != null)
                .OrderBy(x => StatusRank(x.status))
                .ThenByDescending(x => x.sortKey)
                .ToList();

            var list = scrollRect != null
                ? ordered
                : ordered.Take(Mathf.Max(1, maxVisible)).ToList();

            EnsureRows(list.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (i >= list.Count)
                {
                    row.SetSelected(false);
                    row.gameObject.SetActive(false);
                    continue;
                }

                var it = list[i];
                row.gameObject.SetActive(true);
                row.Set(it.title, it.detail, it.status, it.questId);
            }

            RebuildLayouts();
            ApplyTrackerChromeLayout();
        }

        private void ApplyTrackerChromeLayout()
        {
            var panelRt = (RectTransform)transform;
            var pad = rootLayoutGroup != null
                ? rootLayoutGroup.padding
                : new RectOffset(0, 0, 0, 0);

            var innerW = 32f;

            var headerTr = transform.Find("TrackerTitle");
            if (headerTr != null)
            {
                var headerTmp = headerTr.GetComponent<TextMeshProUGUI>();
                if (headerTmp != null)
                {
                    headerTmp.ForceMeshUpdate();
                    innerW = Mathf.Max(
                        innerW,
                        headerTmp.GetPreferredValues(headerTmp.text, float.PositiveInfinity, float.PositiveInfinity).x);
                }
            }

            foreach (var row in rows)
            {
                if (row == null || !row.gameObject.activeSelf)
                    continue;
                innerW = Mathf.Max(innerW, row.MeasurePreferredWidth());
            }

            var outerW = Mathf.Clamp(innerW + pad.left + pad.right, minPanelWidth, maxPanelWidth);
            panelRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, outerW);

            if (scrollRect != null && scrollAreaLayout != null)
            {
                var activeCount = rows.Count(r => r != null && r.gameObject.activeSelf);
                if (activeCount == 0)
                {
                    scrollRect.gameObject.SetActive(false);
                    scrollAreaLayout.minHeight = 0f;
                    scrollAreaLayout.preferredHeight = 0f;
                }
                else
                {
                    scrollRect.gameObject.SetActive(true);

                    var contentVlg = contentRoot != null ? contentRoot.GetComponent<VerticalLayoutGroup>() : null;
                    var sum = contentVlg != null ? contentVlg.padding.vertical : 0f;
                    var spacing = contentVlg != null ? contentVlg.spacing : 8f;
                    var any = false;

                    foreach (var row in rows)
                    {
                        if (row == null || !row.gameObject.activeSelf)
                            continue;
                        any = true;
                        var rrt = row.GetComponent<RectTransform>();
                        if (rrt == null)
                            continue;
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rrt);
                        sum += LayoutUtility.GetPreferredHeight(rrt);
                        sum += spacing;
                    }

                    if (any)
                        sum -= spacing;

                    sum = Mathf.Max(sum, 72f);
                    var capH = Mathf.Min(sum, maxScrollHeight);
                    scrollAreaLayout.minHeight = 0f;
                    scrollAreaLayout.preferredHeight = capH;
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
        }

        private void RebuildLayouts()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var row in rows)
            {
                if (row == null || !row.gameObject.activeSelf) continue;
                var rt = row.GetComponent<RectTransform>();
                if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
            if (contentRoot is RectTransform contentRt)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
                var p = contentRt.parent as RectTransform;
                if (p != null) LayoutRebuilder.ForceRebuildLayoutImmediate(p);
                var gp = p?.parent as RectTransform;
                if (gp != null) LayoutRebuilder.ForceRebuildLayoutImmediate(gp);
            }

            if (scrollRect != null && scrollRect.content is RectTransform scrollContentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContentRt);
        }

        private void EnsureRows(int needed)
        {
            while (rows.Count < needed)
            {
                var go = Instantiate(rowPrefab.gameObject, contentRoot);
                var row = go.GetComponent<QuestTrackerRowView>();
                if (row == null)
                {
                    Destroy(go);
                    Debug.LogError("[QuestTrackerListView] rowPrefab 에 QuestTrackerRowView 가 없습니다.");
                    break;
                }
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
        }

        private static int StatusRank(QuestTrackerStatus status)
        {
            return status switch
            {
                QuestTrackerStatus.TurnInReady => 0,
                QuestTrackerStatus.Active => 1,
                QuestTrackerStatus.Completed => 2,
                _ => 99
            };
        }
    }
}
