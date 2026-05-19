using UnityEditor;
using UnityEngine;

namespace DataDrivenDemo.EditorTools
{
    /// <summary><see cref="QuestOfferWireSettings"/> 자산 생성 헬퍼.</summary>
    public static class QuestOfferWireSettingsMenu
    {
        [MenuItem("Tools/DataDrivenDemo/Create Quest Offer Wire Settings")]
        private static void CreateSettingsAsset()
        {
            var asset = ScriptableObject.CreateInstance<QuestOfferWireSettings>();
            asset.questGiverNpcId = "npc_010";
            asset.questGiverSpawnTemplateNpcId = "npc_001";
            asset.offeredQuestIds = QuestOfferWireResolver.DiscoverOfferQuestIdsFromProjectJson();

            var path = EditorUtility.SaveFilePanelInProject(
                "Quest Offer Wire Settings 저장",
                "QuestOfferWireSettings",
                "asset",
                "Wire 메뉴가 참조할 설정 자산 경로를 선택합니다.",
                "Assets/Data");

            if (string.IsNullOrEmpty(path))
            {
                Object.DestroyImmediate(asset);
                return;
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log($"[QuestOfferWireSettings] 생성됨: {path}");
        }
    }
}
