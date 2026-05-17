using System;
using System.Collections.Generic;
using System.Linq;
using DataDrivenDemo.UI;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>
    /// 여러 퀘스트를 HUD/저널에 표시하기 위한 데모용 static 레지스트리.
    /// HUD는 QuestTrackerListView가 상위 N개만; 저널은 전체를 스크롤로 표시.
    /// </summary>
    public static class QuestTrackerService
    {
        private static readonly Dictionary<string, QuestTrackerItem> items = new();

        public static IEnumerable<QuestTrackerItem> Items => items.Values;

        public static void Upsert(string questId, string title, string detail, QuestTrackerStatus status, long sortKey)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;

            if (!items.TryGetValue(questId, out var it) || it == null)
            {
                it = new QuestTrackerItem { questId = questId };
                items[questId] = it;
            }

            it.title = title ?? "";
            it.detail = detail ?? "";
            it.status = status;
            it.sortKey = sortKey;
        }

        public static void Remove(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return;
            items.Remove(questId);
        }

        public static void ClearAll() => items.Clear();

        public static IEnumerable<QuestTrackerItem> GetTop(int maxVisible)
        {
            return Items
                .Where(x => x != null)
                .OrderBy(x => StatusRank(x.status))
                .ThenByDescending(x => x.sortKey)
                .Take(Mathf.Max(1, maxVisible));
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

