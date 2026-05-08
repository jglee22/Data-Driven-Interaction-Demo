using System.Collections.Generic;
using UnityEngine;

namespace DataDrivenDemo.Quest
{
    /// <summary>퀘스트 정의(TextAsset JSON) 모음. 런타임에서 id로 조회.</summary>
    [DisallowMultipleComponent]
    public sealed class QuestCatalog : MonoBehaviour
    {
        [SerializeField] private TextAsset[] questJsons;

        private readonly Dictionary<string, QuestDefinition> byId = new();

        public void Rebuild()
        {
            byId.Clear();
            if (questJsons == null) return;

            foreach (var asset in questJsons)
            {
                var def = QuestDefinitionLoader.FromTextAsset(asset);
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                    continue;
                byId[def.id] = def;
            }
        }

        public QuestDefinition Get(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
                return null;
            if (byId.Count == 0)
                Rebuild();
            return byId.TryGetValue(questId, out var def) ? def : null;
        }

        public IEnumerable<QuestDefinition> All()
        {
            if (byId.Count == 0)
                Rebuild();
            return byId.Values;
        }
    }
}

