#if UNITY_EDITOR
using System.IO;
using Game.Battle;
using Game.Campaign.DebugTools;
using UnityEditor;
using UnityEngine;

namespace Game.Campaign.Editor
{
    public static class CampaignAssetsMenu
    {
        private const string SettingsPath = "Assets/Scripts/Game/Campaign/Config/CampaignSettings.asset";

        [MenuItem("Game/Campaign/Create Campaign Settings Asset")]
        public static void CreateCampaignSettings()
        {
            EnsureFolder("Assets/Scripts/Game/Campaign/Config");
            var settings = CreateOrLoad<CampaignSettings>(SettingsPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Campaign] Settings ready at {SettingsPath}");
        }

        [MenuItem("Game/Campaign/Create Campaign Runtime In Scene")]
        public static void CreateCampaignRuntimeInScene()
        {
            CreateCampaignSettings();

            var settings = AssetDatabase.LoadAssetAtPath<CampaignSettings>(SettingsPath);
            var battleSettings = FindBattleSettings();

            CampaignService service = Object.FindFirstObjectByType<CampaignService>();
            if (service == null)
            {
                var go = new GameObject("CampaignService");
                service = go.AddComponent<CampaignService>();
                Undo.RegisterCreatedObjectUndo(go, "Create CampaignService");
            }

            CampaignDebugController debug = Object.FindFirstObjectByType<CampaignDebugController>();
            if (debug == null)
                debug = service.gameObject.AddComponent<CampaignDebugController>();

            var so = new SerializedObject(service);
            so.FindProperty("_campaignSettings").objectReferenceValue = settings;
            if (battleSettings != null)
                so.FindProperty("_battleSettings").objectReferenceValue = battleSettings;
            so.ApplyModifiedPropertiesWithoutUndo();

            service.ResolveRefs();
            Selection.activeGameObject = service.gameObject;
            Debug.Log("[Campaign] CampaignService + CampaignDebugController ready in scene.");
        }

        private static BattleSettings FindBattleSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:BattleSettings");
            if (guids == null || guids.Length == 0)
                return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BattleSettings>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
#endif
