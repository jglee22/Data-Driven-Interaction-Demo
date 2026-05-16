using System;
using System.Collections.Generic;
using DataDrivenDemo.Core.Save;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>메인 메뉴 Continue/New Game 등 데모용 저장 일괄 조회·삭제.</summary>
    public static class QuestDemoSaveHelper
    {
        public static bool HasAnySavedProgress(QuestCatalog catalog)
        {
            if (HasNonEmptyAcceptedList())
                return true;

            foreach (var id in CollectQuestIds(catalog))
            {
                if (HasLocalState(id))
                    return true;
            }

            return false;
        }

        public static void ClearAllProgress(QuestCatalog catalog, Action onCompleted = null)
        {
            var ids = CollectQuestIds(catalog);
            if (ids.Count == 0)
            {
                PlayerPrefs.DeleteKey(QuestSaveKeys.AcceptedList);
                PlayerPrefs.Save();
                onCompleted?.Invoke();
                return;
            }

            var remaining = ids.Count;
            foreach (var id in ids)
            {
                SaveServices.QuestSave.ClearQuestState(id, () =>
                {
                    remaining--;
                    if (remaining > 0)
                        return;

                    PlayerPrefs.DeleteKey(QuestSaveKeys.AcceptedList);
                    PlayerPrefs.Save();
                    onCompleted?.Invoke();
                });
            }
        }

        private static bool HasNonEmptyAcceptedList()
        {
            if (!PlayerPrefs.HasKey(QuestSaveKeys.AcceptedList))
                return false;

            var raw = PlayerPrefs.GetString(QuestSaveKeys.AcceptedList, "");
            return !string.IsNullOrWhiteSpace(raw);
        }

        private static bool HasLocalState(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return false;

            var key = QuestSaveKeys.StateKey(questId);
            if (!PlayerPrefs.HasKey(key))
                return false;

            var json = PlayerPrefs.GetString(key, "");
            return !string.IsNullOrWhiteSpace(json);
        }

        private static List<string> CollectQuestIds(QuestCatalog catalog)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal)
            {
                "quest_001"
            };

            if (catalog != null)
            {
                catalog.Rebuild();
                foreach (var def in catalog.All())
                {
                    if (def != null && !string.IsNullOrWhiteSpace(def.id))
                        ids.Add(def.id);
                }
            }

            return new List<string>(ids);
        }
    }
}
