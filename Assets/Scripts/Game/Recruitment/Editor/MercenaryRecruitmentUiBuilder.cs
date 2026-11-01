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

            // Force placeholder offers into the asset (field initializers can be wiped on serialize).
            var so = new SerializedObject(asset);
            SerializedProperty offers = so.FindProperty("offers");
            offers.ClearArray();
            AddOffer(offers, "merc-infantry-squad", "Mercenary Infantry Squad", 0, 8, 0, 0, 40, 10);
            AddOffer(offers, "merc-mixed-platoon", "Mercenary Mixed Platoon", 1, 16, 8, 8, 120, 5);
            AddOffer(offers, "merc-archer-squad", "Mercenary Archer Squad", 0, 0, 0, 8, 55, 8);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log($"[Recruitment] Created {SettingsPath}");
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
            int initialAvailable)
        {
            int index = offers.arraySize;
            offers.InsertArrayElementAtIndex(index);
            SerializedProperty e = offers.GetArrayElementAtIndex(index);
            e.FindPropertyRelative("offerId").stringValue = offerId;
            e.FindPropertyRelative("displayName").stringValue = displayName;
            e.FindPropertyRelative("unitType").enumValueIndex = unitType;
            e.FindPropertyRelative("infantry").intValue = infantry;
            e.FindPropertyRelative("cavalry").intValue = cavalry;
            e.FindPropertyRelative("archers").intValue = archers;
            e.FindPropertyRelative("goldPrice").intValue = goldPrice;
            e.FindPropertyRelative("initialAvailable").intValue = initialAvailable;
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
            BuildAllPrefabs();

            MercenaryRecruitmentSettings settings =
                AssetDatabase.LoadAssetAtPath<MercenaryRecruitmentSettings>(SettingsPath);

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

        private static void BuildRowPrefab()
        {
            var root = new GameObject("MercenaryRecruitmentRow", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 78);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            var layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 78f;
            layout.preferredHeight = 78f;

            var button = root.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            var view = root.AddComponent<MercenaryRecruitmentRowView>();

            TMP_Text name = AddTmp(root.transform, "Name", "Mercenary Squad", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 0.55f, 0.95f, 12, 12);

            TMP_Text avail = AddTmp(root.transform, "Availability", "Availability: 10", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(avail.rectTransform, 0.32f, 0.55f, 12, 12);

            TMP_Text price = AddTmp(root.transform, "Price", "Price: 40 Gold", 14, FontStyles.Normal, TextAlignmentOptions.Right);
            var priceRt = price.rectTransform;
            priceRt.anchorMin = new Vector2(0.55f, 0.32f);
            priceRt.anchorMax = new Vector2(1f, 0.55f);
            priceRt.offsetMin = new Vector2(0, 0);
            priceRt.offsetMax = new Vector2(-12, 0);

            TMP_Text comp = AddTmp(root.transform, "Composition", "Squad  I:8 C:0 A:0", 13, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(comp.rectTransform, 0.05f, 0.32f, 12, 12);

            Assign(view, "_selectButton", button);
            Assign(view, "_background", bg);
            Assign(view, "_nameText", name);
            Assign(view, "_availabilityText", avail);
            Assign(view, "_priceText", price);
            Assign(view, "_compositionText", comp);

            SavePrefab(root, "MercenaryRecruitmentRow.prefab");
        }

        private static void BuildPanelPrefab()
        {
            var root = CreatePanelRoot("MercenaryRecruitmentPanel", new Vector2(520, 640));
            var view = root.AddComponent<MercenaryRecruitmentPanelView>();
            root.AddComponent<MercenaryRecruitmentController>();

            TMP_Text title = AddTmp(root.transform, "Title", "MERCENARY RECRUITMENT", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(title.rectTransform, 10, 36);

            TMP_Text gold = AddTmp(root.transform, "Gold", "Gold: 500", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(gold.rectTransform, 0.86f, 0.92f, 16, 16);
            gold.color = new Color(1f, 0.85f, 0.35f);

            TMP_Text header = AddTmp(root.transform, "ListHeader", "Available mercenaries:", 16, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(header.rectTransform, 0.80f, 0.86f, 20, 20);

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(root.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.05f, 0.38f);
            listRt.anchorMax = new Vector2(0.95f, 0.80f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = Vector2.zero;
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            var fitter = listGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/MercenaryRecruitmentRow.prefab");

            TMP_Text selected = AddTmp(root.transform, "Selected", "Selected: —", 15, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(selected.rectTransform, 0.30f, 0.36f, 16, 16);

            Button minus = AddButton(root.transform, "MinusButton", "-", new Color(0.3f, 0.3f, 0.35f));
            PlaceButton(minus.GetComponent<RectTransform>(), 0.22f, 0.34f, 0.22f, 0.29f);

            TMP_Text qty = AddTmp(root.transform, "Quantity", "1", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(qty.rectTransform, 0.22f, 0.29f, 150, 150);

            Button plus = AddButton(root.transform, "PlusButton", "+", new Color(0.3f, 0.3f, 0.35f));
            PlaceButton(plus.GetComponent<RectTransform>(), 0.66f, 0.78f, 0.22f, 0.29f);

            TMP_Text total = AddTmp(root.transform, "Total", "Total: 0 Gold", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(total.rectTransform, 0.16f, 0.22f, 16, 16);

            TMP_Text status = AddTmp(root.transform, "Status", string.Empty, 14, FontStyles.Italic, TextAlignmentOptions.Center);
            Stretch(status.rectTransform, 0.11f, 0.16f, 16, 16);

            Button hire = AddButton(root.transform, "HireButton", "HIRE", new Color(0.2f, 0.45f, 0.3f));
            PlaceButton(hire.GetComponent<RectTransform>(), 0.55f, 0.92f, 0.03f, 0.10f);

            Button close = AddButton(root.transform, "CloseButton", "CLOSE", new Color(0.35f, 0.3f, 0.3f));
            PlaceButton(close.GetComponent<RectTransform>(), 0.08f, 0.45f, 0.03f, 0.10f);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_goldText", gold);
            Assign(view, "_listRoot", listGo.transform);
            if (rowPrefab != null)
                Assign(view, "_rowPrefab", rowPrefab.GetComponent<MercenaryRecruitmentRowView>());
            Assign(view, "_selectedNameText", selected);
            Assign(view, "_quantityText", qty);
            Assign(view, "_totalText", total);
            Assign(view, "_statusText", status);
            Assign(view, "_minusButton", minus);
            Assign(view, "_plusButton", plus);
            Assign(view, "_hireButton", hire);
            Assign(view, "_closeButton", close);

            SavePrefab(root, "MercenaryRecruitmentPanel.prefab");
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
            image.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);
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
