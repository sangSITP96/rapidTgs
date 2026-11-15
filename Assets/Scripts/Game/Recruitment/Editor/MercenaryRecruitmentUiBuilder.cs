#if UNITY_EDITOR
using Game.Campaign;
using Game.Currency;
using Game.Persistence;
using Game.Recruitment.UI;
using Game.Troops;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Recruitment.Editor
{
    public static class MercenaryRecruitmentUiBuilder
    {
        private const string PrefabFolder = "Assets/Scripts/Game/Recruitment/Prefabs";
        private const string SettingsFolder = "Assets/Scripts/Game/Recruitment/Config";
        private const string SettingsPath = SettingsFolder + "/MercenaryRecruitmentSettings.asset";

        [MenuItem("Tools/Recruitment/Delete Local Progress Save")]
        public static void DeleteLocalProgressSave()
        {
            var repo = new CommanderProgressRepository();
            repo.Delete();
            Debug.Log($"[Recruitment] Deleted local save (if any) at {repo.FilePath}");
        }

        [MenuItem("Tools/Recruitment/Create Default Settings Asset")]
        public static void CreateDefaultSettings()
        {
            EnsureFolder(SettingsFolder);
            var existing = AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                Debug.Log($"[Recruitment] Settings already exist at {SettingsPath}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<MercenaryRecruitmentSettings>();
            AssetDatabase.CreateAsset(asset, SettingsPath);
            WriteTerminalDefaultOffers(asset);
            Selection.activeObject = asset;
            Debug.Log($"[Recruitment] Created {SettingsPath}");
        }

        [MenuItem("Tools/Recruitment/Reset Settings Offers To Terminal Defaults")]
        public static void ResetSettingsOffers()
        {
            EnsureFolder(SettingsFolder);
            var asset = AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath);
            if (asset == null)
                asset = ScriptableObject.CreateInstance<MercenaryRecruitmentSettings>();
            if (AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath) == null)
                AssetDatabase.CreateAsset(asset, SettingsPath);

            WriteTerminalDefaultOffers(asset);
            Selection.activeObject = asset;
            Debug.Log($"[Recruitment] Reset terminal offers on {SettingsPath}");
        }

        private static void WriteTerminalDefaultOffers(MercenaryRecruitmentSettings asset)
        {
            var so = new SerializedObject(asset);
            SerializedProperty offers = so.FindProperty("offers");
            offers.ClearArray();
            // unitType: Squad=0 Platoon=1 Company=2 Battalion=3
            // season: Spring=0 Summer=1 Autumn=2 Winter=3
            // combat: Attack=0 Defense=1 Health=2
            AddOffer(offers, "merc-company-autumn-atk", null, 2, 128, 0, 0, 820, 4, 2, 0);
            AddOffer(offers, "merc-platoon-winter-def", null, 1, 32, 0, 0, 210, 6, 3, 1);
            AddOffer(offers, "merc-squad-spring-hp", null, 0, 8, 0, 0, 55, 10, 0, 2);
            AddOffer(offers, "merc-battalion-summer-atk", null, 3, 512, 0, 0, 2900, 2, 1, 0);
            AddOffer(offers, "merc-company-winter-def", null, 2, 64, 64, 0, 980, 3, 3, 1);
            AddOffer(offers, "merc-platoon-autumn-atk", null, 1, 0, 32, 0, 240, 5, 2, 0);
            AddOffer(offers, "merc-squad-summer-atk", null, 0, 0, 0, 8, 60, 8, 1, 0);
            AddOffer(offers, "merc-platoon-spring-hp", null, 1, 16, 0, 16, 260, 4, 0, 2);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static void AddOffer(
            SerializedProperty offers,
            string offerId,
            string displayName,
            int unitType,
            int infantry,
            int cavalry,
            int archers,
            int goldPrice,
            int initialAvailable,
            int seasonAffinity,
            int combatAffinity)
        {
            int index = offers.arraySize;
            offers.InsertArrayElementAtIndex(index);
            SerializedProperty e = offers.GetArrayElementAtIndex(index);
            e.FindPropertyRelative("offerId").stringValue = offerId;
            e.FindPropertyRelative("displayName").stringValue = displayName ?? string.Empty;
            e.FindPropertyRelative("unitType").enumValueIndex = unitType;
            e.FindPropertyRelative("infantry").intValue = infantry;
            e.FindPropertyRelative("cavalry").intValue = cavalry;
            e.FindPropertyRelative("archers").intValue = archers;
            e.FindPropertyRelative("goldPrice").intValue = goldPrice;
            e.FindPropertyRelative("initialAvailable").intValue = initialAvailable;
            e.FindPropertyRelative("seasonAffinity").enumValueIndex = seasonAffinity;
            e.FindPropertyRelative("combatAffinity").enumValueIndex = combatAffinity;
        }

        [MenuItem("Tools/Recruitment/Build Placeholder UI Prefabs")]
        public static void BuildAllPrefabs()
        {
            EnsureFolder(PrefabFolder);
            BuildRowPrefab();
            BuildPanelPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Recruitment] Prefabs built under {PrefabFolder}");
        }

        [MenuItem("Tools/Recruitment/Wire Recruitment Into Active Scene")]
        public static void WireIntoActiveScene()
        {
            CreateDefaultSettings();
            // Ensure offer list matches Terminal table placeholders (safe to re-run).
            MercenaryRecruitmentSettings settings =
                AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath);
            if (settings != null)
                WriteTerminalDefaultOffers(settings);

            BuildAllPrefabs();

            settings = AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath);

            // Runtime services — prefer next to CampaignService / roster.
            TemporaryAlphaTroopRoster roster = Object.FindFirstObjectByType<TemporaryAlphaTroopRoster>();
            GameObject serviceHost;
            if (roster != null)
                serviceHost = roster.gameObject;
            else
            {
                CampaignService campaign = Object.FindFirstObjectByType<CampaignService>();
                if (campaign != null)
                    serviceHost = campaign.gameObject;
                else
                {
                    serviceHost = new GameObject("RecruitmentService");
                    Undo.RegisterCreatedObjectUndo(serviceHost, "RecruitmentService");
                    roster = serviceHost.AddComponent<TemporaryAlphaTroopRoster>();
                }
            }

            if (roster == null)
                roster = serviceHost.GetComponent<TemporaryAlphaTroopRoster>()
                         ?? serviceHost.AddComponent<TemporaryAlphaTroopRoster>();

            MercenaryPool pool = serviceHost.GetComponent<MercenaryPool>()
                                 ?? Undo.AddComponent<MercenaryPool>(serviceHost);
            MercenaryHiringService hiring = serviceHost.GetComponent<MercenaryHiringService>()
                                           ?? Undo.AddComponent<MercenaryHiringService>(serviceHost);
            CommanderProgressPersistence persistence =
                serviceHost.GetComponent<CommanderProgressPersistence>()
                ?? Undo.AddComponent<CommanderProgressPersistence>(serviceHost);

            CurrencySystem currency = Object.FindFirstObjectByType<CurrencySystem>();
            if (currency == null)
            {
                var currencyGo = new GameObject("CurrencySystem");
                Undo.RegisterCreatedObjectUndo(currencyGo, "CurrencySystem");
                currency = currencyGo.AddComponent<CurrencySystem>();
                var currencySettings = AssetDatabase.LoadAssetAtPath<CurrencySettings>(
                    "Assets/Scripts/Game/Currency/CurrentSettings.asset");
                if (currencySettings != null)
                {
                    var cso = new SerializedObject(currency);
                    cso.FindProperty("settings").objectReferenceValue = currencySettings;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var poolSo = new SerializedObject(pool);
            poolSo.FindProperty("settings").objectReferenceValue = settings;
            poolSo.ApplyModifiedPropertiesWithoutUndo();

            var hireSo = new SerializedObject(hiring);
            hireSo.FindProperty("pool").objectReferenceValue = pool;
            hireSo.FindProperty("currency").objectReferenceValue = currency;
            hireSo.FindProperty("roster").objectReferenceValue = roster;
            hireSo.FindProperty("persistence").objectReferenceValue = persistence;
            hireSo.ApplyModifiedPropertiesWithoutUndo();

            var persistSo = new SerializedObject(persistence);
            persistSo.FindProperty("currency").objectReferenceValue = currency;
            persistSo.FindProperty("pool").objectReferenceValue = pool;
            persistSo.FindProperty("roster").objectReferenceValue = roster;
            persistSo.ApplyModifiedPropertiesWithoutUndo();

            // UI under CampaignHudRoot or any Canvas.
            Transform parent = null;
            GameObject hud = GameObject.Find("CampaignHudRoot");
            if (hud != null)
                parent = hud.transform;
            else
            {
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                    parent = canvas.transform;
            }

            if (parent == null)
            {
                Debug.LogError("[Recruitment] No Canvas / CampaignHudRoot found.");
                return;
            }

            Transform existing = parent.Find("MercenaryRecruitmentPanel");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/MercenaryRecruitmentPanel.prefab");
            if (panelPrefab == null)
            {
                Debug.LogError("[Recruitment] Panel prefab missing.");
                return;
            }

            GameObject panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, parent);
            panelInstance.name = "MercenaryRecruitmentPanel";
            panelInstance.SetActive(false);
            Undo.RegisterCreatedObjectUndo(panelInstance, "MercenaryRecruitmentPanel");

            MercenaryRecruitmentController controller =
                panelInstance.GetComponent<MercenaryRecruitmentController>()
                ?? panelInstance.AddComponent<MercenaryRecruitmentController>();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("hiringService").objectReferenceValue = hiring;
            controllerSo.FindProperty("panel").objectReferenceValue =
                panelInstance.GetComponent<MercenaryRecruitmentPanelView>();
            controllerSo.FindProperty("currency").objectReferenceValue = currency;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            DevelopModeController develop = Object.FindFirstObjectByType<DevelopModeController>(
                FindObjectsInactive.Include);
            if (develop != null)
            {
                Button recruitBtn = EnsureRecruitmentSidebarButton(develop);
                var dso = new SerializedObject(develop);
                SerializedProperty btnProp = dso.FindProperty("_recruitmentButton");
                SerializedProperty panelProp = dso.FindProperty("_recruitmentController");
                if (btnProp != null)
                    btnProp.objectReferenceValue = recruitBtn;
                if (panelProp != null)
                    panelProp.objectReferenceValue = controller;
                dso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(develop);

                if (recruitBtn != null)
                {
                    recruitBtn.onClick = new Button.ButtonClickedEvent();
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                        recruitBtn.onClick,
                        develop.OpenRecruitment);
                    EditorUtility.SetDirty(recruitBtn);
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Recruitment] DevelopModeController not found — panel wired but no sidebar button.");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = panelInstance;
            Debug.Log(
                "[Recruitment] Wired Phase 19: pool + hiring + persistence + UI. " +
                "Open via Develop Mode → Recruitment.");
        }

        private static Button EnsureRecruitmentSidebarButton(DevelopModeController develop)
        {
            Transform sidebar = null;
            Button[] buttons = develop.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "RecruitmentButton")
                    return buttons[i];
            }

            Button template = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;
                string n = buttons[i].gameObject.name;
                if (n == "GeneralButton" || n == "CampaignDebugButton" || n.Contains("Button"))
                {
                    template = buttons[i];
                    sidebar = buttons[i].transform.parent;
                    if (n == "GeneralButton")
                        break;
                }
            }

            if (template == null || sidebar == null)
                return null;

            GameObject buttonGo = Object.Instantiate(template.gameObject, sidebar);
            buttonGo.name = "RecruitmentButton";
            buttonGo.SetActive(true);
            Undo.RegisterCreatedObjectUndo(buttonGo, "RecruitmentButton");

            Text legacy = buttonGo.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = "Recruitment";
            else
            {
                TMP_Text tmp = buttonGo.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                    tmp.text = "Recruitment";
            }

            return buttonGo.GetComponent<Button>();
        }

        // Column x-fractions: UNIT TROOPS SEASON TYPE COMBAT GOLD HIRE
        private static readonly float[] ColMins = { 0.00f, 0.13f, 0.22f, 0.38f, 0.54f, 0.70f, 0.82f };
        private static readonly float[] ColMaxs = { 0.13f, 0.22f, 0.38f, 0.54f, 0.70f, 0.82f, 1.00f };
        private static readonly string[] ColHeaders =
        {
            "UNIT", "TROOPS", "SEASON AFFINITY", "TYPE", "COMBAT AFFINITY", "GOLD", "HIRE"
        };

        private static void BuildRowPrefab()
        {
            var root = new GameObject("MercenaryRecruitmentRow", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(860, 40);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.90f, 0.86f, 0.78f, 0.35f);
            var layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;
            layout.flexibleWidth = 1f;

            var view = root.AddComponent<MercenaryRecruitmentRowView>();

            TMP_Text unit = AddColumnText(root.transform, "Unit", "Company", 0, TextAlignmentOptions.Left);
            TMP_Text troops = AddColumnText(root.transform, "Troops", "128", 1, TextAlignmentOptions.Center);
            TMP_Text season = AddColumnText(root.transform, "Season", "Autumn", 2, TextAlignmentOptions.Center);
            TMP_Text type = AddColumnText(root.transform, "Type", "Infantry", 3, TextAlignmentOptions.Center);
            TMP_Text combat = AddColumnText(root.transform, "Combat", "Attack", 4, TextAlignmentOptions.Center);
            TMP_Text gold = AddColumnText(root.transform, "Gold", "820", 5, TextAlignmentOptions.Center);

            Button hire = CreateHireButton(root.transform, 6);
            TMP_Text hireLabel = hire.GetComponentInChildren<TMP_Text>();

            // Bottom rule
            var rule = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            rule.transform.SetParent(root.transform, false);
            var ruleRt = rule.GetComponent<RectTransform>();
            ruleRt.anchorMin = new Vector2(0f, 0f);
            ruleRt.anchorMax = new Vector2(1f, 0f);
            ruleRt.pivot = new Vector2(0.5f, 0f);
            ruleRt.sizeDelta = new Vector2(0f, 1f);
            rule.GetComponent<Image>().color = new Color(0.35f, 0.32f, 0.28f, 0.35f);
            rule.GetComponent<Image>().raycastTarget = false;

            Assign(view, "_background", bg);
            Assign(view, "_unitText", unit);
            Assign(view, "_troopsText", troops);
            Assign(view, "_seasonText", season);
            Assign(view, "_typeText", type);
            Assign(view, "_combatText", combat);
            Assign(view, "_goldText", gold);
            Assign(view, "_hireButton", hire);
            Assign(view, "_hireButtonLabel", hireLabel);

            SavePrefab(root, "MercenaryRecruitmentRow.prefab");
        }

        private static void BuildPanelPrefab()
        {
            var root = CreatePanelRoot("MercenaryRecruitmentPanel", new Vector2(920, 620));
            var view = root.AddComponent<MercenaryRecruitmentPanelView>();
            root.AddComponent<MercenaryRecruitmentController>();

            // Title with side rules look
            TMP_Text title = AddTmp(root.transform, "Title", "MERCENARY TERMINAL", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(title.rectTransform, 14, 42);
            title.color = new Color(0.18f, 0.16f, 0.14f, 1f);

            TMP_Text gold = AddTmp(root.transform, "Gold", "Gold: 500", 16, FontStyles.Bold, TextAlignmentOptions.Right);
            var goldRt = gold.rectTransform;
            goldRt.anchorMin = new Vector2(0.55f, 1f);
            goldRt.anchorMax = new Vector2(1f, 1f);
            goldRt.pivot = new Vector2(1f, 1f);
            goldRt.anchoredPosition = new Vector2(-24f, -18f);
            goldRt.sizeDelta = new Vector2(220f, 28f);
            gold.color = new Color(0.35f, 0.28f, 0.12f, 1f);

            // Column headers
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(root.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.04f, 0.82f);
            headerRt.anchorMax = new Vector2(0.96f, 0.88f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            headerGo.GetComponent<Image>().color = new Color(0.82f, 0.76f, 0.66f, 0.55f);
            headerGo.GetComponent<Image>().raycastTarget = false;
            for (int i = 0; i < ColHeaders.Length; i++)
            {
                TextAlignmentOptions align = i == 0 ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                TMP_Text h = AddColumnText(headerGo.transform, "H_" + ColHeaders[i], ColHeaders[i], i, align);
                h.fontStyle = FontStyles.Bold;
                h.fontSizeMax = 14f;
                h.fontSizeMin = 9f;
                h.color = new Color(0.18f, 0.16f, 0.14f, 1f);
            }

            // Scrollable body
            var scrollGo = new GameObject("TroopListScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.04f, 0.16f);
            scrollRt.anchorMax = new Vector2(0.96f, 0.82f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(0.93f, 0.89f, 0.80f, 0.25f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var listGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(viewportGo.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 1f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = Vector2.zero;
            listRt.sizeDelta = Vector2.zero;
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            var fitter = listGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = listRt;

            // Simple scrollbar
            var barGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            barGo.transform.SetParent(scrollGo.transform, false);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(1f, 0.5f);
            barRt.sizeDelta = new Vector2(10f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            barGo.GetComponent<Image>().color = new Color(0.75f, 0.70f, 0.60f, 0.5f);
            var scrollbar = barGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(barGo.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            handleGo.GetComponent<Image>().color = new Color(0.45f, 0.40f, 0.32f, 0.85f);
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.handleRect = handleRt;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/MercenaryRecruitmentRow.prefab");

            TMP_Text status = AddTmp(root.transform, "Status", string.Empty, 14, FontStyles.Italic, TextAlignmentOptions.Center);
            Stretch(status.rectTransform, 0.09f, 0.14f, 24, 24);
            status.color = new Color(0.20f, 0.35f, 0.18f, 1f);

            Button close = AddButton(root.transform, "CloseButton", "CLOSE", new Color(0.55f, 0.48f, 0.38f, 1f));
            PlaceButton(close.GetComponent<RectTransform>(), 0.38f, 0.62f, 0.03f, 0.08f);
            var closeLabel = close.GetComponentInChildren<TMP_Text>();
            if (closeLabel != null)
                closeLabel.color = new Color(0.18f, 0.16f, 0.14f, 1f);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_goldText", gold);
            Assign(view, "_headerRoot", headerGo.transform);
            Assign(view, "_listRoot", listGo.transform);
            if (rowPrefab != null)
                Assign(view, "_rowPrefab", rowPrefab.GetComponent<MercenaryRecruitmentRowView>());
            Assign(view, "_statusText", status);
            Assign(view, "_closeButton", close);
            Assign(view, "_scrollRect", scroll);

            SavePrefab(root, "MercenaryRecruitmentPanel.prefab");
        }

        private static TMP_Text AddColumnText(
            Transform parent,
            string name,
            string text,
            int columnIndex,
            TextAlignmentOptions align)
        {
            TMP_Text tmp = AddTmp(parent, name, text, 15, FontStyles.Normal, align);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 16f;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.color = new Color(0.18f, 0.16f, 0.14f, 1f);
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(ColMins[columnIndex], 0.1f);
            rt.anchorMax = new Vector2(ColMaxs[columnIndex], 0.9f);
            rt.offsetMin = new Vector2(4f, 0f);
            rt.offsetMax = new Vector2(-4f, 0f);
            return tmp;
        }

        private static Button CreateHireButton(Transform parent, int columnIndex)
        {
            var go = new GameObject("HireButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ColMins[columnIndex], 0.15f);
            rt.anchorMax = new Vector2(ColMaxs[columnIndex], 0.85f);
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.88f, 0.82f, 0.70f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = AddTmp(go.transform, "Label", "Hire", 14, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = new Color(0.18f, 0.16f, 0.14f, 1f);
            Stretch(label.rectTransform, 0f, 1f, 0, 0);
            return button;
        }

        private static GameObject CreatePanelRoot(string name, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var image = root.GetComponent<Image>();
            // Parchment / ledger background
            image.color = new Color(0.89f, 0.84f, 0.74f, 0.97f);
            return root;
        }

        private static TMP_Text AddTmp(
            Transform parent,
            string name,
            string text,
            float size,
            FontStyles style,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button AddButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = AddTmp(go.transform, "Label", label, 18, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f, 1f, 0, 0);
            return button;
        }

        private static void StretchTop(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(-40f, height);
        }

        private static void Stretch(RectTransform rt, float anchorMinY, float anchorMaxY, float left, float right)
        {
            rt.anchorMin = new Vector2(0f, anchorMinY);
            rt.anchorMax = new Vector2(1f, anchorMaxY);
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-right, 0f);
        }

        private static void PlaceButton(RectTransform rt, float minX, float maxX, float minY, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Assign(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SavePrefab(GameObject root, string fileName)
        {
            string path = $"{PrefabFolder}/{fileName}";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
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
