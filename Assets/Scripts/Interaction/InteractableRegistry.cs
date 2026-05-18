using System;
using System.Collections.Generic;

namespace DataDrivenDemo.Interaction
{
    /// <summary>
    /// targetId 기준 상호작용 오브젝트 조회. Awake/OnEnable 시 등록해 FindObjectsByType 전체 순회를 피합니다.
    /// </summary>
    public static class InteractableRegistry
    {
        private static readonly Dictionary<string, InteractableBase> Primary =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, InteractableBase> GiverFallback =
            new(StringComparer.OrdinalIgnoreCase);

        public static void Register(InteractableBase interactable)
        {
            if (interactable == null)
                return;

            var key = NormalizeId(interactable.Id);
            if (key == null)
                return;

            if (interactable is QuestGiverInteractable)
                GiverFallback[key] = interactable;
            else
                Primary[key] = interactable;
        }

        public static void Unregister(InteractableBase interactable)
        {
            if (interactable == null)
                return;

            var key = NormalizeId(interactable.Id);
            if (key == null)
                return;

            if (interactable is QuestGiverInteractable)
            {
                if (GiverFallback.TryGetValue(key, out var cur) && cur == interactable)
                    GiverFallback.Remove(key);
            }
            else if (Primary.TryGetValue(key, out var cur) && cur == interactable)
            {
                Primary.Remove(key);
            }
        }

        public static bool TryGet(string targetId, out InteractableBase found)
        {
            found = null;
            var key = NormalizeId(targetId);
            if (key == null)
                return false;

            if (Primary.TryGetValue(key, out found) && found != null)
                return true;

            return GiverFallback.TryGetValue(key, out found) && found != null;
        }

        /// <summary>월드 목표 마커용: NPC/Item/Terminal 우선, 없을 때만 QuestGiver 폴백.</summary>
        public static bool TryGetForObjectiveAnchor(string targetId, out InteractableBase found)
        {
            return TryGet(targetId, out found);
        }

        public static void ClearForTests()
        {
            Primary.Clear();
            GiverFallback.Clear();
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return id.Trim();
        }
    }
}
