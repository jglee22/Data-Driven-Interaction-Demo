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
        private string[] testJsonStrings;
        private QuestDefinition[] testDefinitions;

        public void Rebuild()
        {
            byId.Clear();

            if (questJsons != null)
            {
                foreach (var asset in questJsons)
                {
                    var def = QuestDefinitionLoader.FromTextAsset(asset);
                    if (def == null || string.IsNullOrWhiteSpace(def.id))
                        continue;
                    byId[def.id] = def;
                }
            }

            if (testJsonStrings != null)
            {
                foreach (var json in testJsonStrings)
                {
                    var def = QuestDefinitionLoader.FromJsonText(json);
                    if (def == null || string.IsNullOrWhiteSpace(def.id))
                        continue;
                    byId[def.id] = def;
                }
            }

            if (testDefinitions == null)
                return;

            foreach (var def in testDefinitions)
            {
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

        /// <summary>Edit Mode 테스트: JSON TextAsset 목록을 주입합니다.</summary>
        public void SetQuestJsonsForTests(TextAsset[] jsons)
        {
            questJsons = jsons;
            Rebuild();
        }

        /// <summary>Edit Mode 테스트: JSON 문자열로 카탈로그를 채웁니다(TextAsset 인스턴스 불필요).</summary>
        public void SetQuestJsonStringsForTests(params string[] jsonStrings)
        {
            testJsonStrings = jsonStrings;
            Rebuild();
        }

        /// <summary>Edit Mode 테스트: 코드로 만든 정의를 등록합니다(JsonUtility 중첩 배열 이슈 회피).</summary>
        public void RegisterDefinitionsForTests(params QuestDefinition[] definitions)
        {
            testDefinitions = definitions;
            Rebuild();
        }
    }
}

