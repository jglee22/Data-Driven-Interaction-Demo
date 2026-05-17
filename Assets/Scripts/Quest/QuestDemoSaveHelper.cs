using System;
using System.Collections.Generic;
using DataDrivenDemo.Core.Save;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>메인 메뉴 Continue/New Game 등 데모용 저장 일괄 조회/삭제.</summary>
    public static class QuestDemoSaveHelper
    {
        public static bool HasAnySavedProgress(QuestCatalog catalog)
        {
            return PlayerPrefsSaveService.ProbeLocal(CollectQuestIds(catalog));
        }

        public static void HasAnySavedProgressAsync(QuestCatalog catalog, Action<bool> onResult)
        {
            if (onResult == null)
                return;

            var ids = CollectQuestIds(catalog);
            SaveServices.QuestSave.ProbeAnySavedProgressAsync(ids, onResult);
        }

        public static void ClearAllProgress(QuestCatalog catalog, Action onCompleted = null)
        {
            var ids = CollectQuestIds(catalog);
            if (ids.Count == 0)
            {
                SaveServices.QuestSave.SaveAcceptedQuestIdsAsync(Array.Empty<string>(), onCompleted);
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

                    SaveServices.QuestSave.SaveAcceptedQuestIdsAsync(Array.Empty<string>(), onCompleted);
                });
            }
        }

        public static List<string> CollectQuestIds(QuestCatalog catalog)
        {
            var ids = new List<string>();
            if (catalog == null)
                return ids;

            catalog.Rebuild();
            foreach (var def in catalog.All())
            {
                if (def != null && !string.IsNullOrWhiteSpace(def.id))
                    ids.Add(def.id);
            }

            return ids;
        }
    }
}
