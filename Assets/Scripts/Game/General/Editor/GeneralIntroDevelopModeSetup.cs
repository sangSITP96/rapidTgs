#if UNITY_EDITOR
using Game.General.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.General.Editor
{
    public static class GeneralIntroDevelopModeSetup
    {
        private const string PrefabFolder = "Assets/Scripts/Game/General/Prefabs";
        private const string PrefabPath = PrefabFolder + "/GeneralIntroPanel.prefab";
        private const string PortraitPath = "Assets/Sprites/UI/General/General_BG.jpg";
        private const string MainScenePath = "Assets/Scenes/RapidTgsPrototype_main.unity";

        [MenuItem("Tools/General/Wire General Intro Panel (Active Scene)")]
        public static void WireIntoActiveScene()
        {
            EnsureFolder(PrefabFolder);
            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
            if (portrait == null)
            {
                // Force reimport as Sprite if needed.
                var importer = AssetImporter.GetAtPath(PortraitPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                    portrait = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
                }
            }

            if (portrait == null)
                Debug.LogWarning($"[General] Portrait missing at {PortraitPath}. Panel will use empty image.");

            GameObject prefabRoot = BuildIntroPanelPrefab(portrait);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Object.DestroyImmediate(prefabRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DevelopModeController develop = Object.FindFirstObjectByType<DevelopModeController>(
                FindObjectsInactive.Include);
            if (develop == null)
            {
                Debug.LogError("[General] DevelopModeController not found in active scene.");
                return;
            }

            Transform canvasParent = null;
            GameObject hud = GameObject.Find("CampaignHudRoot");
            if (hud != null)
                canvasParent = hud.transform;
            else
            {
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                    canvasParent = canvas.transform;
            }

            if (canvasParent == null)
            {
                Debug.LogError("[General] No Canvas / CampaignHudRoot found for GeneralIntroPanel.");
                return;
            }

            Transform existing = canvasParent.Find("GeneralIntroPanel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasParent);
            instance.name = "GeneralIntroPanel";
            instance.SetActive(false);
            Undo.RegisterCreatedObjectUndo(instance, "GeneralIntroPanel");

            var view = instance.GetComponent<GeneralIntroPanelView>();

            // Ensure sidebar General button exists.
            Button generalBtn = EnsureGeneralSidebarButton(develop);

            var so = new SerializedObject(develop);
            so.FindProperty("_generalButton").objectReferenceValue = generalBtn;
            so.FindProperty("_generalIntroPanel").objectReferenceValue = view;
            so.FindProperty("_generalIntroPrefab").objectReferenceValue =
                prefab != null ? prefab.GetComponent<GeneralIntroPanelView>() : null;
            so.FindProperty("_generalPortrait").objectReferenceValue = portrait;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(develop);

            if (generalBtn != null)
            {
                generalBtn.onClick = new Button.ButtonClickedEvent();
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    generalBtn.onClick,
                    develop.OpenGeneralIntro);
                EditorUtility.SetDirty(generalBtn);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = instance;
            Debug.Log(
                "[General] Wired General Intro: Develop Mode → General → panel → Open Navigation → Plan March.");
        }

        [MenuItem("Tools/General/Batch Wire General Intro (Main Scene)")]
        public static void BatchWireMainScene()
        {
            if (!System.IO.File.Exists(MainScenePath))
            {
                Debug.LogError($"[General] Missing scene {MainScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            WireIntoActiveScene();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[General] Batch Wire General Intro complete.");
            EditorApplication.Exit(0);
        }

        private static GameObject BuildIntroPanelPrefab(Sprite portrait)
        {
            var root = new GameObject("GeneralIntroPanel", typeof(RectTransform), typeof(Image));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            var dim = root.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.82f);
            dim.raycastTarget = true;

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(root.transform, false);
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0.05f, 0.16f);
            portraitRt.anchorMax = new Vector2(0.95f, 0.97f);
            portraitRt.offsetMin = Vector2.zero;
            portraitRt.offsetMax = Vector2.zero;
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
            portraitImage.sprite = portrait;
            portraitImage.raycastTarget = false;

            GameObject speechBubble = GeneralIntroSpeechBubbleFactory.Create(
                root.transform,
                out TMP_Text nameText,
                out TMP_Text messageText);

            Button open = CreateButton(
                root.transform,
                "OpenNavigation",
                "Open Navigation",
                new Color(0.16f, 0.34f, 0.28f, 1f),
                new Vector2(0.28f, 0.035f),
                new Vector2(0.72f, 0.13f));

            Button close = CreateButton(
                root.transform,
                "Close",
                "X",
                new Color(0.35f, 0.18f, 0.18f, 1f),
                new Vector2(0.93f, 0.93f),
                new Vector2(0.985f, 0.985f));

            var view = root.AddComponent<GeneralIntroPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_backgroundImage").objectReferenceValue = portraitImage;
            so.FindProperty("_speechBubbleRoot").objectReferenceValue = speechBubble;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_messageText").objectReferenceValue = messageText;
            so.FindProperty("_openNavigationButton").objectReferenceValue = open;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Color color,
            Vector2 min,
            Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = name == "Close" ? 20 : 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return go.GetComponent<Button>();
        }

        private static Button EnsureGeneralSidebarButton(DevelopModeController develop)
        {
            Transform sidebar = null;
            var so = new SerializedObject(develop);
            var campaignBtn = so.FindProperty("_campaignDebugButton")?.objectReferenceValue as Button;
            var battleBtn = so.FindProperty("_battleDebugButton")?.objectReferenceValue as Button;
            if (campaignBtn != null)
                sidebar = campaignBtn.transform.parent;
            else if (battleBtn != null)
                sidebar = battleBtn.transform.parent;
            else
            {
                Button any = develop.GetComponentInChildren<Button>(true);
                if (any != null)
                    sidebar = any.transform.parent;
            }

            if (sidebar == null)
                return null;

            Transform existing = sidebar.Find("GeneralButton");
            if (existing != null)
                return existing.GetComponent<Button>();

            Button template = campaignBtn != null
                ? campaignBtn
                : battleBtn != null
                    ? battleBtn
                    : sidebar.GetComponentInChildren<Button>(true);

            GameObject buttonGo = Object.Instantiate(template.gameObject, sidebar);
            buttonGo.name = "GeneralButton";
            buttonGo.SetActive(true);
            Undo.RegisterCreatedObjectUndo(buttonGo, "GeneralButton");

            Text legacy = buttonGo.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = "General";
            TMP_Text tmp = buttonGo.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = "General";

            return buttonGo.GetComponent<Button>();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
