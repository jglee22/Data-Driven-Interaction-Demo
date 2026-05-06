using UnityEngine;

namespace DataDrivenDemo.Quest
{
    public static class QuestDefinitionLoader
    {
        public static QuestDefinition FromJsonText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonUtility.FromJson<QuestDefinition>(json);
        }

        public static QuestDefinition FromTextAsset(TextAsset asset)
        {
            if (asset == null)
                return null;

            return FromJsonText(asset.text);
        }
    }
}

