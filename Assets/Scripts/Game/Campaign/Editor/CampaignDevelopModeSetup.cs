#if UNITY_EDITOR
using Game.Battle;
using Game.Campaign.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Campaign.Editor
{
    public static class CampaignDevelopModeSetup
    {
        private const string SettingsPath = "Assets/Scripts/Game/Campaign/Config/CampaignSettings.asset";

        [MenuItem("Tools/Campaign/Wire Campaign Debug Into Develop Mode (Active Scene)")]
        public static void WireActiveScene()
        {
            DevelopModeController develop = Object.FindFirstObjectByType<DevelopModeController>(
                FindObjectsInactive.Include);
            if (develop == null)
            {
                Debug.LogError("[Campaign] DevelopModeController not found in active scene.");
                return;
            }

            CampaignAssetsMenu.CreateCampaignSettings();
            CampaignSettings settings = AssetDatabase.LoadAssetAtPath<CampaignSettings>(SettingsPath);
            BattleSettings battleSettings = FindBattleSettings();

            CampaignService service = Object.FindFirstObjectByType<CampaignService>(FindObjectsInactive.Include);
            if (service == null)
            {
                var go = new GameObject("CampaignService");
                Undo.RegisterCreatedObjectUndo(go, "Create CampaignService");
                service = go.AddComponent<CampaignService>();
            }

            var serviceSo = new SerializedObject(service);
            serviceSo.FindProperty("_campaignSettings").objectReferenceValue = settings;
            if (battleSettings != null)
                serviceSo.FindProperty("_battleSettings").objectReferenceValue = battleSettings;
            serviceSo.ApplyModifiedPropertiesWithoutUndo();
            service.ResolveRefs();

            CampaignDebugController debug = service.GetComponent<CampaignDebugController>();
            if (debug == null)
                debug = service.gameObject.AddComponent<CampaignDebugController>();

            GameObject contentRoot = FindContentRoot(develop);
            Transform sidebar = FindSidebar(develop);
            if (contentRoot == null || sidebar == null)
            {
                Debug.LogError("[Campaign] Could not find Develop Mode content root or sidebar.");
                return;
            }

            GameObject panel = EnsureCampaignDebugPanel(contentRoot.transform);
            Toggle toggle = panel.GetComponentInChildren<Toggle>(true);
            Button button = EnsureCampaignDebugSidebarButton(sidebar, develop);

            SerializedObject so = new SerializedObject(develop);
            so.FindProperty("_campaignDebugButton").objectReferenceValue = button;
            so.FindProperty("_campaignDebugPanel").objectReferenceValue = panel;
            so.FindProperty("_showCampaignOverlayToggle").objectReferenceValue = toggle;
            so.FindProperty("_campaignDebug").objectReferenceValue = debug;
            so.ApplyModifiedPropertiesWithoutUndo();

            debug.ShowOverlay = false;
            EditorUtility.SetDirty(develop);
            EditorUtility.SetDirty(service);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                "[Campaign] Wired Campaign Debug into Develop Mode. " +
                "Sidebar 'Campaign Debug' + Show Overlay toggle. Overlay is OnGUI-driven.");
        }

        private static BattleSettings FindBattleSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:BattleSettings");
            if (guids == null || guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<BattleSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject FindContentRoot(DevelopModeController develop)
        {
            SerializedObject so = new SerializedObject(develop);
            GameObject battlePanel = so.FindProperty("_battleDebugPanel").objectReferenceValue as GameObject;
            if (battlePanel != null && battlePanel.transform.parent != null)
                return battlePanel.transform.parent.gameObject;

            GameObject seasonPanel = so.FindProperty("_seasonDebugPanel").objectReferenceValue as GameObject;
            if (seasonPanel != null && seasonPanel.transform.parent != null)
                return seasonPanel.transform.parent.gameObject;

            return null;
        }

        private static Transform FindSidebar(DevelopModeController develop)
        {
            SerializedObject so = new SerializedObject(develop);
            Button battleButton = so.FindProperty("_battleDebugButton").objectReferenceValue as Button;
            if (battleButton != null)
                return battleButton.transform.parent;

            Button seasonButton = so.FindProperty("_seasonDebugButton").objectReferenceValue as Button;
            if (seasonButton != null)
                return seasonButton.transform.parent;

            return null;
        }

        private static GameObject EnsureCampaignDebugPanel(Transform contentRoot)
        {
            Transform existing = contentRoot.Find("CampaignDebugSettings");
            if (existing != null)
                return existing.gameObject;

            GameObject panel = new GameObject("CampaignDebugSettings", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(panel, "Create CampaignDebugSettings");
            panel.transform.SetParent(contentRoot, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject labelGo = new GameObject("Info", typeof(RectTransform));
            labelGo.transform.SetParent(panel.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.text =
                "CAMPAIGN DEBUG\n\n" +
                "1) Enter this tab to ARM map offers.\n" +
                "2) CLOSE Develop Mode (checkbox) so the map is free.\n" +
                "3) Tap a Colony Map territory → Campaign Offer.\n" +
                "4) Accept → Select Troops → March.\n\n" +
                "Test Controls (bottom-right) only show while this tab is open.\n" +
                "Closing Develop Mode hides Test Controls but keeps map offers armed.";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.55f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(12f, 0f);
            labelRt.offsetMax = new Vector2(-12f, -12f);

            GameObject toggleGo = new GameObject("ShowCampaignOverlay", typeof(RectTransform));
            toggleGo.transform.SetParent(panel.transform, false);
            var toggle = toggleGo.AddComponent<Toggle>();
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0f, 0.42f);
            toggleRt.anchorMax = new Vector2(0.5f, 0.52f);
            toggleRt.offsetMin = new Vector2(12f, 0f);
            toggleRt.offsetMax = Vector2.zero;

            GameObject bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(toggleGo.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            toggle.targetGraphic = bgImage;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            GameObject check = new GameObject("Checkmark", typeof(RectTransform));
            check.transform.SetParent(bg.transform, false);
            var checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.8f, 0.4f, 1f);
            toggle.graphic = checkImage;
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.1f, 0.1f);
            checkRt.anchorMax = new Vector2(0.9f, 0.9f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            GameObject toggleLabel = new GameObject("Label", typeof(RectTransform));
            toggleLabel.transform.SetParent(toggleGo.transform, false);
            var toggleText = toggleLabel.AddComponent<Text>();
            toggleText.text = "Show Campaign Overlay";
            toggleText.font = text.font;
            toggleText.fontSize = 14;
            toggleText.color = Color.white;
            var toggleLabelRt = toggleLabel.GetComponent<RectTransform>();
            toggleLabelRt.anchorMin = new Vector2(1.05f, 0f);
            toggleLabelRt.anchorMax = new Vector2(3f, 1f);
            toggleLabelRt.offsetMin = Vector2.zero;
            toggleLabelRt.offsetMax = Vector2.zero;

            panel.SetActive(false);
            return panel;
        }

        private static Button EnsureCampaignDebugSidebarButton(Transform sidebar, DevelopModeController develop)
        {
            Transform existing = sidebar.Find("CampaignDebugButton");
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                WireButton(existingButton, develop);
                return existingButton;
            }

            Button template = sidebar.GetComponentInChildren<Button>(true);
            GameObject buttonGo;
            if (template != null)
            {
                buttonGo = Object.Instantiate(template.gameObject, sidebar);
                buttonGo.name = "CampaignDebugButton";
            }
            else
            {
                buttonGo = new GameObject("CampaignDebugButton", typeof(RectTransform));
                buttonGo.transform.SetParent(sidebar, false);
                buttonGo.AddComponent<Image>();
                buttonGo.AddComponent<Button>();
            }

            Undo.RegisterCreatedObjectUndo(buttonGo, "Create CampaignDebugButton");
            var label = buttonGo.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = "Campaign Debug";

            var button = buttonGo.GetComponent<Button>();
            WireButton(button, develop);
            return button;
        }

        private static void WireButton(Button button, DevelopModeController develop)
        {
            if (button == null || develop == null)
                return;

            // Replace UnityEvent — RemoveAllListeners does not clear Inspector persistent listeners
            // left over when this button was cloned from Movement Settings.
            button.onClick = new Button.ButtonClickedEvent();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                button.onClick,
                develop.SetCampaignDebugMode);
            EditorUtility.SetDirty(button);
        }
    }
}
#endif
