using System;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Behavior")]
        [Min(1)]
        [SerializeField] private int maxVisible = 4;

        private readonly List<QuestTrackerRowView> rows = new();

        public void SetMaxVisible(int n) => maxVisible = Mathf.Max(1, n);

        public void Render(IEnumerable<QuestTrackerItem> items)
        {
            if (contentRoot == null || rowPrefab == null)
                return;

            var list = (items ?? Array.Empty<QuestTrackerItem>())
                .Where(x => x != null)
                // TurnInReady(보고 가능) > Active > Completed(완료)
                .OrderBy(x => StatusRank(x.status))
                // 그 다음 최신(큰 sortKey가 우선)
                .ThenByDescending(x => x.sortKey)
                .Take(Mathf.Max(1, maxVisible))
                .ToList();

            EnsureRows(list.Count);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (i >= list.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var it = list[i];
                row.gameObject.SetActive(true);
                row.Set(it.title, it.detail, it.status, it.questId);
            }

            RebuildLayouts();
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

