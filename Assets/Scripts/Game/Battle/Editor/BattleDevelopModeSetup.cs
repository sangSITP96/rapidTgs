#if UNITY_EDITOR
using Game.Battle.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.Battle.Editor
{
    public static class BattleDevelopModeSetup
    {
        private const string BattleTestPrefabPath =
            "Assets/Scripts/Game/Battle/Prefabs/BattleTestPanel.prefab";

        [MenuItem("Tools/Battle/Wire Battle Debug Into Develop Mode (Active Scene)")]
        public static void WireActiveScene()
        {
            DevelopModeController develop = Object.FindFirstObjectByType<DevelopModeController>(
                FindObjectsInactive.Include);
            if (develop == null)
            {
                Debug.LogError("[Battle] DevelopModeController not found in active scene.");
                return;
            }

            BattleTestController battle = EnsureBattleTestController();
            GameObject contentRoot = FindContentRoot(develop);
            Transform sidebar = FindSidebar(develop);

            if (contentRoot == null || sidebar == null)
            {
                Debug.LogError("[Battle] Could not find Develop Mode content root or sidebar.");
                return;
            }

            GameObject panel = EnsureBattleDebugPanel(contentRoot.transform);
            Toggle toggle = panel.GetComponentInChildren<Toggle>(true);
            Button button = EnsureBattleDebugSidebarButton(sidebar, develop);

            SerializedObject so = new SerializedObject(develop);
            so.FindProperty("_battleDebugButton").objectReferenceValue = button;
            so.FindProperty("_battleDebugPanel").objectReferenceValue = panel;
            so.FindProperty("_showBattleOverlayToggle").objectReferenceValue = toggle;
            so.FindProperty("_battleDebug").objectReferenceValue = battle;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (battle != null)
                battle.ShowOverlay = false;

            EditorUtility.SetDirty(develop);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[Battle] Wired Battle Debug into Develop Mode. Use sidebar 'Battle Debug' + Show Overlay toggle.");
        }

        private static BattleTestController EnsureBattleTestController()
        {
            BattleTestController existing = Object.FindFirstObjectByType<BattleTestController>(
                FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleTestPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Battle] Missing prefab at {BattleTestPrefabPath}. Run Create Default Battle Assets first.");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "BattleTestPanel";
            Undo.RegisterCreatedObjectUndo(instance, "Create BattleTestPanel");
            return instance.GetComponent<BattleTestController>();
        }

        private static GameObject FindContentRoot(DevelopModeController develop)
        {
            SerializedObject so = new SerializedObject(develop);
            GameObject seasonPanel = so.FindProperty("_seasonDebugPanel").objectReferenceValue as GameObject;
            if (seasonPanel != null && seasonPanel.transform.parent != null)
                return seasonPanel.transform.parent.gameObject;

            Transform[] all = develop.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == "Root" && all[i].Find("SeasonDebugSettings") != null)
                    return all[i].gameObject;
            }

            return null;
        }

        private static Transform FindSidebar(DevelopModeController develop)
        {
            SerializedObject so = new SerializedObject(develop);
            Button seasonButton = so.FindProperty("_seasonDebugButton").objectReferenceValue as Button;
            if (seasonButton != null)
                return seasonButton.transform.parent;

            Transform[] all = develop.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == "SeasonDebug" && all[i].parent != null)
                    return all[i].parent;
            }

            return null;
        }

        private static GameObject EnsureBattleDebugPanel(Transform contentRoot)
        {
            Transform existing = contentRoot.Find("BattleDebugSettings");
            if (existing != null)
                return existing.gameObject;

            GameObject seasonPanel = contentRoot.Find("SeasonDebugSettings")?.gameObject;
            GameObject panel;
            if (seasonPanel != null)
            {
                panel = Object.Instantiate(seasonPanel, contentRoot);
                panel.name = "BattleDebugSettings";
                RenameToggleLabel(panel, "Show Overlay (Battle Debug)");
            }
            else
            {
                panel = CreateMinimalTogglePanel(contentRoot);
            }

            panel.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panel, "Create BattleDebugSettings");
            return panel;
        }

        private static void RenameToggleLabel(GameObject panel, string label)
        {
            Text[] texts = panel.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].text.Contains("Overlay") || texts[i].text.Contains("Season") ||
                    texts[i].text.Contains("Travel") || texts[i].text.Contains("Battle"))
                {
                    texts[i].text = label;
                    EditorUtility.SetDirty(texts[i]);
                }
            }

            Toggle toggle = panel.GetComponentInChildren<Toggle>(true);
            if (toggle != null)
            {
                toggle.isOn = false;
                toggle.onValueChanged = new Toggle.ToggleEvent();
                EditorUtility.SetDirty(toggle);
            }
        }

        private static GameObject CreateMinimalTogglePanel(Transform contentRoot)
        {
            GameObject panel = new GameObject("BattleDebugSettings", typeof(RectTransform));
            panel.transform.SetParent(contentRoot, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f, 10f);
            rt.offsetMax = new Vector2(-20f, -10f);

            GameObject toggleGo = new GameObject("ShowBattleOverlayToggle", typeof(RectTransform));
            toggleGo.transform.SetParent(panel.transform, false);
            Toggle toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = false;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(toggleGo.transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = "Show Overlay (Battle Debug)";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(48f, 0f);
            labelRt.offsetMax = Vector2.zero;

            return panel;
        }

        private static Button EnsureBattleDebugSidebarButton(Transform sidebar, DevelopModeController develop)
        {
            Transform existing = sidebar.Find("BattleDebug");
            Button button;
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
            else
            {
                Transform season = sidebar.Find("SeasonDebug");
                GameObject buttonGo;
                if (season != null)
                {
                    buttonGo = Object.Instantiate(season.gameObject, sidebar);
                    buttonGo.name = "BattleDebug";
                }
                else
                {
                    buttonGo = new GameObject("BattleDebug", typeof(RectTransform), typeof(Image), typeof(Button));
                    buttonGo.transform.SetParent(sidebar, false);
                    RectTransform rt = buttonGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0f, 100f);
                }

                Undo.RegisterCreatedObjectUndo(buttonGo, "Create BattleDebug Button");
                button = buttonGo.GetComponent<Button>();

                Text label = buttonGo.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = "Battle Debug";
            }

            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    button.onClick,
                    develop.SetBattleDebugMode);
                EditorUtility.SetDirty(button);
            }

            return button;
        }
    }
}
#endif
