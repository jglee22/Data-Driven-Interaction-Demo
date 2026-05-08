using UnityEngine;

namespace DataDrivenDemo.Quest
{
    public static class QuestDefinitionLoader
    {
        public static QuestDefinition FromJsonText(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var def = JsonUtility.FromJson<QuestDefinition>(json);
            PostProcess(def);
            return def;
        }

        public static QuestDefinition FromTextAsset(TextAsset asset)
        {
            if (asset == null)
                return null;

            return FromJsonText(asset.text);
        }

        private static void PostProcess(QuestDefinition def)
        {
            if (def?.steps == null || def.steps.Length == 0)
                return;

            foreach (var step in def.steps)
            {
                if (step == null)
                    continue;

                // 이미 새 포맷이면 유지
                if (step.objectives != null && step.objectives.Length > 0)
                    continue;

                // 레거시(actionId/objective/requiredCount) -> objectives 1개로 승격
                if (!string.IsNullOrWhiteSpace(step.actionId) || !string.IsNullOrWhiteSpace(step.objective))
                {
                    step.objectives = new[]
                    {
                        new QuestObjective
                        {
                            // type/targetId는 레거시 actionId만 있는 경우 이벤트 파서가 추론해줌
                            type = "",
                            targetId = "",
                            actionId = step.actionId ?? "",
                            requiredCount = step.requiredCount,
                            uiText = step.objective ?? ""
                        }
                    };
                }
            }
        }
    }
}

