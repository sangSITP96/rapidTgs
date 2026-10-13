#if UNITY_EDITOR
using Game.Campaign.UI;
using Game.Troops;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.Editor
{
    /// <summary>
    /// Builds simple placeholder Campaign UI prefabs (Unity UI + TextMeshPro).
    /// </summary>
    public static class CampaignUiPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Scripts/Game/Campaign/Prefabs";

        [MenuItem("Tools/Campaign/Build Placeholder UI Prefabs")]
        public static void BuildAllPrefabs()
        {
            EnsureFolder(PrefabFolder);

            BuildOfferPopup();
            BuildTroopRow();
            BuildTroopSelectionPopup();
            BuildActivePanel();
            BuildResultPopup();
            BuildTestControls();
            BuildHudRoot();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Campaign] Placeholder UI prefabs built under {PrefabFolder}");
        }

        [MenuItem("Tools/Campaign/Create Campaign UI In Active Scene")]
        public static void CreateInScene()
        {
            BuildAllPrefabs();

            CampaignService service = Object.FindFirstObjectByType<CampaignService>();
            if (service == null)
            {
                var go = new GameObject("CampaignService");
                Undo.RegisterCreatedObjectUndo(go, "CampaignService");
                service = go.AddComponent<CampaignService>();
                go.AddComponent<TemporaryAlphaTroopRoster>();
                go.AddComponent<CampaignMapOfferHook>();
            }
            else
            {
                if (service.GetComponent<TemporaryAlphaTroopRoster>() == null)
                    service.gameObject.AddComponent<TemporaryAlphaTroopRoster>();
                if (service.GetComponent<CampaignMapOfferHook>() == null)
                    service.gameObject.AddComponent<CampaignMapOfferHook>();
            }

            CampaignAssetsMenu.CreateCampaignSettings();
            var settings = AssetDatabase.LoadAssetAtPath<CampaignSettings>(
                "Assets/Scripts/Game/Campaign/Config/CampaignSettings.asset");
            var battleSettings = FindBattleSettings();
            var so = new SerializedObject(service);
            so.FindProperty("_campaignSettings").objectReferenceValue = settings;
            if (battleSettings != null)
                so.FindProperty("_battleSettings").objectReferenceValue = battleSettings;
            so.FindProperty("_ownerRoster").objectReferenceValue =
                service.GetComponent<TemporaryAlphaTroopRoster>();
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PrefabFolder}/CampaignHudRoot.prefab");
            if (hudPrefab == null)
            {
                Debug.LogError("[Campaign] CampaignHudRoot.prefab missing.");
                return;
            }

            GameObject existing = GameObject.Find("CampaignHudRoot");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            instance.name = "CampaignHudRoot";
            Undo.RegisterCreatedObjectUndo(instance, "CampaignHudRoot");

            var flow = instance.GetComponent<CampaignFlowController>();
            if (flow != null)
            {
                var flowSo = new SerializedObject(flow);
                flowSo.FindProperty("_service").objectReferenceValue = service;
                flowSo.FindProperty("_roster").objectReferenceValue =
                    service.GetComponent<TemporaryAlphaTroopRoster>();
                flowSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Disable OnGUI debug as primary UX if present.
            var onGui = Object.FindFirstObjectByType<Game.Campaign.DebugTools.CampaignDebugController>();
            if (onGui != null)
                onGui.ShowOverlay = false;

            Selection.activeGameObject = instance;
            Debug.Log("[Campaign] Scene wired: tap Colony Map territory → Offer → Accept → Select Troops → March.");
        }

        private static Game.Battle.BattleSettings FindBattleSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:BattleSettings");
            if (guids == null || guids.Length == 0)
                return null;
            return AssetDatabase.LoadAssetAtPath<Game.Battle.BattleSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void BuildOfferPopup()
        {
            var root = CreatePanelRoot("CampaignOfferPopup", new Vector2(420, 240));
            var view = root.AddComponent<CampaignOfferPopupView>();

            TMP_Text title = AddTmp(root.transform, "Title", "CAMPAIGN", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            RectTransform titleRt = title.rectTransform;
            StretchTop(titleRt, 16, 50);

            TMP_Text territory = AddTmp(root.transform, "Territory", "Forest Territory", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(territory.rectTransform, 0.55f, 0.72f, 20, 20);

            TMP_Text objective = AddTmp(root.transform, "Objective", "Objective: Capture Territory", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(objective.rectTransform, 0.42f, 0.55f, 20, 20);

            Button decline = AddButton(root.transform, "DeclineButton", "DECLINE", new Color(0.45f, 0.2f, 0.2f));
            PlaceButton(decline.GetComponent<RectTransform>(), 0.08f, 0.48f, 0.08f, 0.28f);

            Button accept = AddButton(root.transform, "AcceptButton", "ACCEPT", new Color(0.2f, 0.45f, 0.25f));
            PlaceButton(accept.GetComponent<RectTransform>(), 0.52f, 0.92f, 0.08f, 0.28f);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_territoryText", territory);
            Assign(view, "_objectiveText", objective);
            Assign(view, "_declineButton", decline);
            Assign(view, "_acceptButton", accept);

            SavePrefab(root, "CampaignOfferPopup.prefab");
        }

        private static void BuildTroopRow()
        {
            var root = new GameObject("CampaignTroopSelectionRow", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 64);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
            var layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 64f;
            layout.preferredHeight = 64f;

            var view = root.AddComponent<CampaignTroopSelectionRowView>();

            Toggle toggle = CreateToggle(root.transform, "Toggle");
            var toggleRt = toggle.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0f, 0.2f);
            toggleRt.anchorMax = new Vector2(0f, 0.8f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.anchoredPosition = new Vector2(12, 0);
            toggleRt.sizeDelta = new Vector2(28, 28);

            TMP_Text name = AddTmp(root.transform, "Name", "Platoon A", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 0.52f, 0.95f, 50, 90);

            TMP_Text capacity = AddTmp(root.transform, "Capacity", "32", 18, FontStyles.Bold, TextAlignmentOptions.Right);
            var capRt = capacity.rectTransform;
            capRt.anchorMin = new Vector2(1f, 0.52f);
            capRt.anchorMax = new Vector2(1f, 0.95f);
            capRt.pivot = new Vector2(1f, 1f);
            capRt.anchoredPosition = new Vector2(-12, 0);
            capRt.sizeDelta = new Vector2(60, 28);

            TMP_Text comp = AddTmp(root.transform, "Composition", "I:16 C:8 A:8", 14, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(comp.rectTransform, 0.08f, 0.48f, 50, 12);

            Assign(view, "_toggle", toggle);
            Assign(view, "_nameText", name);
            Assign(view, "_capacityText", capacity);
            Assign(view, "_compositionText", comp);

            SavePrefab(root, "CampaignTroopSelectionRow.prefab");
        }

        private static void BuildTroopSelectionPopup()
        {
            var root = CreatePanelRoot("CampaignTroopSelectionPopup", new Vector2(440, 420));
            var view = root.AddComponent<CampaignTroopSelectionPopupView>();

            TMP_Text title = AddTmp(root.transform, "Title", "SELECT TROOPS", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(title.rectTransform, 12, 48);

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(root.transform, false);
            var listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0.05f, 0.28f);
            listRt.anchorMax = new Vector2(0.95f, 0.86f);
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
                $"{PrefabFolder}/CampaignTroopSelectionRow.prefab");

            TMP_Text selected = AddTmp(root.transform, "SelectedTotal", "Selected: 0", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(selected.rectTransform, 0.18f, 0.28f, 20, 20);

            Button back = AddButton(root.transform, "BackButton", "BACK", new Color(0.3f, 0.3f, 0.35f));
            PlaceButton(back.GetComponent<RectTransform>(), 0.08f, 0.48f, 0.06f, 0.16f);

            Button march = AddButton(root.transform, "MarchButton", "MARCH", new Color(0.2f, 0.4f, 0.55f));
            PlaceButton(march.GetComponent<RectTransform>(), 0.52f, 0.92f, 0.06f, 0.16f);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_selectedTotalText", selected);
            Assign(view, "_listRoot", listGo.transform);
            if (rowPrefab != null)
                Assign(view, "_rowPrefab", rowPrefab.GetComponent<CampaignTroopSelectionRowView>());
            Assign(view, "_backButton", back);
            Assign(view, "_marchButton", march);

            SavePrefab(root, "CampaignTroopSelectionPopup.prefab");
        }

        private static void BuildActivePanel()
        {
            var root = CreatePanelRoot("CampaignActivePanel", new Vector2(320, 360));
            var view = root.AddComponent<CampaignActivePanelView>();
            // Dock top-left for compact HUD
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16, -16);

            float y = 0.92f;
            TMP_Text title = AddLabeled(root.transform, "Title", "CAMPAIGN", ref y, 22, true);
            TMP_Text territory = AddLabeled(root.transform, "Territory", "Forest Territory", ref y, 16, false);
            TMP_Text time = AddLabeled(root.transform, "Time", "Time: 0h / 24h", ref y, 15, false);
            TMP_Text controller = AddLabeled(root.transform, "Controller", "Control: F.I.S.T.", ref y, 15, false);
            TMP_Text troops = AddLabeled(root.transform, "Troops", "Troops: —", ref y, 15, false);
            TMP_Text controlTime = AddLabeled(root.transform, "ControlTime", "CONTROL TIME\nCommander: 0h\nF.I.S.T.: 0h", ref y, 14, false, 0.18f);
            TMP_Text lastBattle = AddLabeled(root.transform, "LastBattle", "LAST BATTLE\n—", ref y, 14, false, 0.18f);
            TMP_Text silver = AddLabeled(root.transform, "Silver", "Silver: 100", ref y, 16, true);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_territoryText", territory);
            Assign(view, "_timeText", time);
            Assign(view, "_controllerText", controller);
            Assign(view, "_troopsStatusText", troops);
            Assign(view, "_controlTimeText", controlTime);
            Assign(view, "_lastBattleText", lastBattle);
            Assign(view, "_silverText", silver);

            SavePrefab(root, "CampaignActivePanel.prefab");
        }

        private static void BuildResultPopup()
        {
            var root = CreatePanelRoot("CampaignResultPopup", new Vector2(400, 280));
            var view = root.AddComponent<CampaignResultPopupView>();

            TMP_Text title = AddTmp(root.transform, "Title", "CAMPAIGN COMPLETE", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(title.rectTransform, 14, 50);

            TMP_Text outcome = AddTmp(root.transform, "Outcome", "COMMANDER WON", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(outcome.rectTransform, 0.62f, 0.78f, 20, 20);

            TMP_Text control = AddTmp(root.transform, "ControlTime", "Control Time\nCommander: 14h\nF.I.S.T.: 10h", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(control.rectTransform, 0.32f, 0.60f, 20, 20);

            TMP_Text silver = AddTmp(root.transform, "Silver", "Silver: +125", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(silver.rectTransform, 0.20f, 0.32f, 20, 20);

            Button ok = AddButton(root.transform, "OkButton", "OK", new Color(0.25f, 0.35f, 0.5f));
            PlaceButton(ok.GetComponent<RectTransform>(), 0.3f, 0.7f, 0.06f, 0.18f);

            Assign(view, "_root", root);
            Assign(view, "_titleText", title);
            Assign(view, "_outcomeText", outcome);
            Assign(view, "_controlTimeText", control);
            Assign(view, "_silverText", silver);
            Assign(view, "_okButton", ok);

            SavePrefab(root, "CampaignResultPopup.prefab");
        }

        private static void BuildTestControls()
        {
            var root = CreatePanelRoot("CampaignTestControls", new Vector2(260, 260));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16, 16);

            var view = root.AddComponent<CampaignTestControlsView>();
            TMP_Text hint = AddTmp(root.transform, "Hint", "Campaign Test Controls", 14, FontStyles.Italic, TextAlignmentOptions.Center);
            StretchTop(hint.rectTransform, 8, 36);

            Button reinforce = AddButton(root.transform, "AddReinforcement", "Add Reinforcement", new Color(0.35f, 0.3f, 0.2f));
            PlaceButton(reinforce.GetComponent<RectTransform>(), 0.08f, 0.92f, 0.72f, 0.86f);

            Button force = AddButton(root.transform, "ForceBattle", "Force Battle", new Color(0.35f, 0.25f, 0.25f));
            PlaceButton(force.GetComponent<RectTransform>(), 0.08f, 0.92f, 0.58f, 0.72f);

            Button a2 = AddButton(root.transform, "Advance2h", "+2 Game Hours", new Color(0.25f, 0.3f, 0.35f));
            PlaceButton(a2.GetComponent<RectTransform>(), 0.08f, 0.92f, 0.44f, 0.58f);

            Button a6 = AddButton(root.transform, "Advance6h", "+6 Game Hours", new Color(0.25f, 0.3f, 0.35f));
            PlaceButton(a6.GetComponent<RectTransform>(), 0.08f, 0.92f, 0.30f, 0.44f);

            Button complete = AddButton(root.transform, "CompleteNow", "Complete Now", new Color(0.3f, 0.2f, 0.35f));
            PlaceButton(complete.GetComponent<RectTransform>(), 0.08f, 0.92f, 0.14f, 0.28f);

            Assign(view, "_root", root);
            Assign(view, "_hintText", hint);
            Assign(view, "_addReinforcementButton", reinforce);
            Assign(view, "_forceBattleButton", force);
            Assign(view, "_advance2hButton", a2);
            Assign(view, "_advance6hButton", a6);
            Assign(view, "_completeNowButton", complete);

            SavePrefab(root, "CampaignTestControls.prefab");
        }

        private static void BuildHudRoot()
        {
            var canvasGo = new GameObject("CampaignHudRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var flow = canvasGo.AddComponent<CampaignFlowController>();

            GameObject offer = InstantiatePrefabChild(canvasGo.transform, "CampaignOfferPopup.prefab");
            GameObject select = InstantiatePrefabChild(canvasGo.transform, "CampaignTroopSelectionPopup.prefab");
            GameObject active = InstantiatePrefabChild(canvasGo.transform, "CampaignActivePanel.prefab");
            GameObject result = InstantiatePrefabChild(canvasGo.transform, "CampaignResultPopup.prefab");
            GameObject test = InstantiatePrefabChild(canvasGo.transform, "CampaignTestControls.prefab");

            // Center popups
            CenterPopup(offer);
            CenterPopup(select);
            CenterPopup(result);

            Assign(flow, "_offerPopup", offer != null ? offer.GetComponent<CampaignOfferPopupView>() : null);
            Assign(flow, "_troopSelectionPopup", select != null ? select.GetComponent<CampaignTroopSelectionPopupView>() : null);
            Assign(flow, "_activePanel", active != null ? active.GetComponent<CampaignActivePanelView>() : null);
            Assign(flow, "_resultPopup", result != null ? result.GetComponent<CampaignResultPopupView>() : null);
            Assign(flow, "_testControls", test != null ? test.GetComponent<CampaignTestControlsView>() : null);

            // Start hidden
            if (offer != null) offer.SetActive(false);
            if (select != null) select.SetActive(false);
            if (active != null) active.SetActive(false);
            if (result != null) result.SetActive(false);

            SavePrefab(canvasGo, "CampaignHudRoot.prefab");
        }

        private static GameObject InstantiatePrefabChild(Transform parent, string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{prefabName}");
            if (prefab == null)
                return null;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance;
        }

        private static void CenterPopup(GameObject go)
        {
            if (go == null)
                return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
                return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreatePanelRoot(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            return go;
        }

        private static TMP_Text AddTmp(
            Transform parent,
            string name,
            string text,
            int size,
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
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static TMP_Text AddLabeled(
            Transform parent,
            string name,
            string text,
            ref float topAnchor,
            int size,
            bool bold,
            float height = 0.08f)
        {
            float bottom = topAnchor - height;
            TMP_Text tmp = AddTmp(
                parent,
                name,
                text,
                size,
                bold ? FontStyles.Bold : FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            Stretch(tmp.rectTransform, bottom, topAnchor, 14, 14);
            topAnchor = bottom - 0.01f;
            return tmp;
        }

        private static Button AddButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var button = go.GetComponent<Button>();

            TMP_Text text = AddTmp(go.transform, "Label", label, 16, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f, 1f, 4, 4);
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var toggle = go.GetComponent<Toggle>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0.2f, 0.22f, 0.26f, 1f);
            Stretch(bg.GetComponent<RectTransform>(), 0f, 1f, 0, 0);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            var checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(0.35f, 0.85f, 0.45f, 1f);
            Stretch(check.GetComponent<RectTransform>(), 0.15f, 0.85f, 0, 0);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            return toggle;
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
