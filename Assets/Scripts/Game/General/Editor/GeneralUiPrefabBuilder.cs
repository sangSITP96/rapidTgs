#if UNITY_EDITOR
using Game.Campaign;
using Game.Campaign.UI;
using Game.General.UI;
using Game.Troops;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.General.Editor
{
    /// <summary>
    /// Builds Phase 18 General placeholder prefabs and wires them into CampaignHudRoot + scene.
    /// </summary>
    public static class GeneralUiPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Scripts/Game/General/Prefabs";
        private const string CampaignPrefabFolder = "Assets/Scripts/Game/Campaign/Prefabs";
        private const string CampaignHudRootPath = CampaignPrefabFolder + "/CampaignHudRoot.prefab";
        private const string MainScenePath = "Assets/Scenes/RapidTgsPrototype_main.unity";
        private const string SessionWiredKey = "GeneralPhase18.PrefabsWired";

        /// <summary>
        /// Editor-only: after script reload, create/wire General prefabs if missing.
        /// Does not run in Play Mode.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void AutoWireAfterCompileIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (SessionState.GetBool(SessionWiredKey, false))
                    return;

                string briefingPath = $"{PrefabFolder}/GeneralBriefingPanel.prefab";
                bool prefabsExist = AssetDatabase.LoadAssetAtPath<GameObject>(briefingPath) != null;

                CampaignFlowController flow = Object.FindFirstObjectByType<CampaignFlowController>();
                bool sceneWired = false;
                if (flow != null)
                {
                    var so = new SerializedObject(flow);
                    var briefingProp = so.FindProperty("_generalBriefing");
                    sceneWired = briefingProp != null && briefingProp.objectReferenceValue != null;
                }

                if (prefabsExist && sceneWired)
                {
                    SessionState.SetBool(SessionWiredKey, true);
                    return;
                }

                // Only auto-wire when the main prototype scene is open (or HUD prefab exists).
                string activePath = SceneManager.GetActiveScene().path;
                if (!string.Equals(activePath, MainScenePath, System.StringComparison.OrdinalIgnoreCase) &&
                    AssetDatabase.LoadAssetAtPath<GameObject>(CampaignHudRootPath) == null)
                {
                    return;
                }

                Debug.Log("[General] Phase 18 prefabs/refs missing — auto-wiring in Editor (not Play Mode)...");
                WireIntoActiveScene();
                SessionState.SetBool(SessionWiredKey, true);
            };
        }

        [MenuItem("Tools/General/Build Placeholder UI Prefabs")]
        public static void BuildAllPrefabs()
        {
            EnsureFolder(PrefabFolder);
            BuildBriefing();
            BuildRouteAdvice();
            BuildMonitoring();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[General] Placeholder UI prefabs built under {PrefabFolder}");
        }

        [MenuItem("Tools/General/Wire General Into Campaign HUD (Active Scene)")]
        public static void WireIntoActiveScene()
        {
            BuildAllPrefabs();
            RebuildCampaignTroopSelectionWithAdvice();
            WireHudRootPrefabAsset();
            WireSceneObjects();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[General] Wired prefabs + scene. Flow: Offer → General Briefing → Troops → Route Advice → March → Monitoring.");
        }

        /// <summary>
        /// Batchmode entry: Unity.exe -batchmode -projectPath ... -executeMethod Game.General.Editor.GeneralUiPrefabBuilder.BatchSetupPhase18 -quit
        /// </summary>
        public static void BatchSetupPhase18()
        {
            try
            {
                if (!System.IO.File.Exists(MainScenePath))
                {
                    Debug.LogError($"[General] Scene missing: {MainScenePath}");
                    EditorApplication.Exit(1);
                    return;
                }

                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                WireIntoActiveScene();

                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[General] BatchSetupPhase18 complete.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[General] BatchSetupPhase18 failed: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void RebuildCampaignTroopSelectionWithAdvice()
        {
            // Rebuild campaign placeholder prefabs so troop popup includes GeneralAdvice text + Review Route.
            Game.Campaign.Editor.CampaignUiPrefabBuilder.BuildAllPrefabs();
        }

        private static void WireHudRootPrefabAsset()
        {
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignHudRootPath);
            if (hudPrefab == null)
            {
                Debug.LogError($"[General] Missing {CampaignHudRootPath}");
                return;
            }

            string briefingPath = $"{PrefabFolder}/GeneralBriefingPanel.prefab";
            string routePath = $"{PrefabFolder}/GeneralRouteAdvicePanel.prefab";
            string monitoringPath = $"{PrefabFolder}/GeneralMonitoringPanel.prefab";

            GameObject briefingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(briefingPath);
            GameObject routePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(routePath);
            GameObject monitoringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monitoringPath);
            if (briefingPrefab == null || routePrefab == null || monitoringPrefab == null)
            {
                Debug.LogError("[General] General panel prefabs missing after build.");
                return;
            }

            string tempPath = "Assets/Scripts/Game/General/Prefabs/_TempCampaignHudRootEdit.prefab";
            EnsureFolder(PrefabFolder);

            // Work on a contents root via PrefabUtility.
            GameObject contentsRoot = PrefabUtility.LoadPrefabContents(CampaignHudRootPath);
            try
            {
                Transform canvas = contentsRoot.transform;
                RemoveChildNamed(canvas, "GeneralBriefingPanel");
                RemoveChildNamed(canvas, "GeneralRouteAdvicePanel");
                RemoveChildNamed(canvas, "GeneralMonitoringPanel");

                GameObject briefingGo = (GameObject)PrefabUtility.InstantiatePrefab(briefingPrefab, canvas);
                briefingGo.name = "GeneralBriefingPanel";
                CenterPopup(briefingGo);
                briefingGo.SetActive(false);

                GameObject routeGo = (GameObject)PrefabUtility.InstantiatePrefab(routePrefab, canvas);
                routeGo.name = "GeneralRouteAdvicePanel";
                CenterPopup(routeGo);
                routeGo.SetActive(false);

                GameObject monitoringGo = (GameObject)PrefabUtility.InstantiatePrefab(monitoringPrefab, canvas);
                monitoringGo.name = "GeneralMonitoringPanel";
                PlaceMonitoring(monitoringGo);
                monitoringGo.SetActive(false);

                var flow = contentsRoot.GetComponent<CampaignFlowController>();
                if (flow != null)
                {
                    var so = new SerializedObject(flow);
                    so.FindProperty("_generalBriefing").objectReferenceValue =
                        briefingGo.GetComponent<GeneralBriefingView>();
                    so.FindProperty("_generalRouteAdvice").objectReferenceValue =
                        routeGo.GetComponent<GeneralRouteAdviceView>();
                    so.FindProperty("_generalMonitoring").objectReferenceValue =
                        monitoringGo.GetComponent<GeneralMonitoringView>();
                    so.FindProperty("_useGeneralBriefing").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contentsRoot, CampaignHudRootPath);
                Debug.Log($"[General] Updated {CampaignHudRootPath} with General panels + refs.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contentsRoot);
                if (AssetDatabase.LoadAssetAtPath<Object>(tempPath) != null)
                    AssetDatabase.DeleteAsset(tempPath);
            }
        }

        private static void WireSceneObjects()
        {
            GeneralAdvisor advisor = Object.FindFirstObjectByType<GeneralAdvisor>();
            if (advisor == null)
            {
                var go = new GameObject("GeneralAdvisor");
                Undo.RegisterCreatedObjectUndo(go, "GeneralAdvisor");
                advisor = go.AddComponent<GeneralAdvisor>();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            // Refresh scene HUD from updated prefab asset when possible.
            GameObject existingHud = GameObject.Find("CampaignHudRoot");
            var service = Object.FindFirstObjectByType<Game.Campaign.CampaignService>();
            var roster = Object.FindFirstObjectByType<Game.Troops.TemporaryAlphaTroopRoster>();

            if (existingHud != null)
            {
                bool isPrefabInstance = PrefabUtility.GetPrefabAssetType(existingHud) != PrefabAssetType.NotAPrefab;
                if (isPrefabInstance)
                {
                    PrefabUtility.RevertPrefabInstance(existingHud, InteractionMode.AutomatedAction);
                }
                else
                {
                    Object.DestroyImmediate(existingHud);
                    existingHud = null;
                }
            }

            if (existingHud == null)
            {
                GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampaignHudRootPath);
                existingHud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                existingHud.name = "CampaignHudRoot";
                Undo.RegisterCreatedObjectUndo(existingHud, "CampaignHudRoot");
            }

            var flow = existingHud.GetComponent<CampaignFlowController>();
            if (flow == null)
            {
                Debug.LogError("[General] CampaignFlowController missing on CampaignHudRoot.");
                return;
            }

            // Ensure nested General panels exist even if prefab revert kept old content.
            Transform canvas = existingHud.transform;
            GeneralBriefingView briefing = canvas.GetComponentInChildren<GeneralBriefingView>(true);
            GeneralRouteAdviceView route = canvas.GetComponentInChildren<GeneralRouteAdviceView>(true);
            GeneralMonitoringView monitoring = canvas.GetComponentInChildren<GeneralMonitoringView>(true);

            if (briefing == null)
            {
                briefing = EnsureChildView<GeneralBriefingView>(
                    canvas, "GeneralBriefingPanel", $"{PrefabFolder}/GeneralBriefingPanel.prefab");
                CenterPopup(briefing != null ? briefing.gameObject : null);
                if (briefing != null) briefing.gameObject.SetActive(false);
            }

            if (route == null)
            {
                route = EnsureChildView<GeneralRouteAdviceView>(
                    canvas, "GeneralRouteAdvicePanel", $"{PrefabFolder}/GeneralRouteAdvicePanel.prefab");
                CenterPopup(route != null ? route.gameObject : null);
                if (route != null) route.gameObject.SetActive(false);
            }

            if (monitoring == null)
            {
                monitoring = EnsureChildView<GeneralMonitoringView>(
                    canvas, "GeneralMonitoringPanel", $"{PrefabFolder}/GeneralMonitoringPanel.prefab");
                PlaceMonitoring(monitoring != null ? monitoring.gameObject : null);
                if (monitoring != null) monitoring.gameObject.SetActive(false);
            }

            var so = new SerializedObject(flow);
            if (service != null)
                so.FindProperty("_service").objectReferenceValue = service;
            if (roster != null)
                so.FindProperty("_roster").objectReferenceValue = roster;
            so.FindProperty("_generalAdvisor").objectReferenceValue = advisor;
            so.FindProperty("_generalBriefing").objectReferenceValue = briefing;
            so.FindProperty("_generalRouteAdvice").objectReferenceValue = route;
            so.FindProperty("_generalMonitoring").objectReferenceValue = monitoring;
            so.FindProperty("_useGeneralBriefing").boolValue = true;

            var nav = Object.FindFirstObjectByType<Game.Navigation.NavigationController>();
            var travel = Object.FindFirstObjectByType<Game.Travel.TroopTravelController>();
            if (nav != null)
                so.FindProperty("_navigation").objectReferenceValue = nav;
            if (travel != null)
                so.FindProperty("_travel").objectReferenceValue = travel;

            so.ApplyModifiedPropertiesWithoutUndo();
            EnsureTroopAdviceText(flow);

            // Apply overrides back onto prefab instance if connected.
            if (PrefabUtility.IsPartOfPrefabInstance(existingHud))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(flow);
                PrefabUtility.ApplyPrefabInstance(existingHud, InteractionMode.AutomatedAction);
            }

            Selection.activeGameObject = existingHud;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void RemoveChildNamed(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        private static void EnsureTroopAdviceText(CampaignFlowController flow)
        {
            if (flow == null)
                return;

            var flowSo = new SerializedObject(flow);
            var troopProp = flowSo.FindProperty("_troopSelectionPopup");
            var troop = troopProp != null ? troopProp.objectReferenceValue as CampaignTroopSelectionPopupView : null;
            if (troop == null)
                return;

            var troopSo = new SerializedObject(troop);
            if (troopSo.FindProperty("_generalAdviceText").objectReferenceValue != null)
                return;

            Transform root = troop.transform;
            TMP_Text advice = AddTmp(root, "GeneralAdvice", "The General…", 13, FontStyles.Italic, TextAlignmentOptions.TopLeft);
            var rt = advice.rectTransform;
            rt.anchorMin = new Vector2(0.06f, 0.62f);
            rt.anchorMax = new Vector2(0.94f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            advice.color = new Color(0.85f, 0.9f, 0.75f, 1f);

            troopSo.FindProperty("_generalAdviceText").objectReferenceValue = advice;
            troopSo.ApplyModifiedPropertiesWithoutUndo();

            var panelRt = troop.GetComponent<RectTransform>();
            if (panelRt != null && panelRt.sizeDelta.y < 520f)
                panelRt.sizeDelta = new Vector2(Mathf.Max(panelRt.sizeDelta.x, 460f), 520f);
        }

        private static T EnsureChildView<T>(Transform parent, string name, string prefabPath) where T : Component
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                T view = existing.GetComponent<T>();
                if (view != null)
                    return view;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[General] Missing prefab: {prefabPath}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            Undo.RegisterCreatedObjectUndo(instance, name);
            return instance.GetComponent<T>();
        }

        private static void BuildBriefing()
        {
            var root = CreatePanelRoot("GeneralBriefingPanel", new Vector2(520, 560));
            var view = root.AddComponent<GeneralBriefingView>();

            Image portrait = CreatePortrait(root.transform, "Portrait");
            TMP_Text name = AddTmp(root.transform, "Name", "CAMPAIGN SELECTION", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 0.88f, 0.96f, 96, 16);

            TMP_Text message = AddTmp(root.transform, "Message", "Briefing…", 15, FontStyles.Italic, TextAlignmentOptions.TopLeft);
            Stretch(message.rectTransform, 0.78f, 0.88f, 16, 16);
            message.color = new Color(0.9f, 0.92f, 0.75f);

            TMP_Text body = AddTmp(root.transform, "Body", "Campaign details…", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Stretch(body.rectTransform, 0.18f, 0.77f, 16, 16);

            Button decline = AddButton(root.transform, "Decline", "Decline", new Color(0.35f, 0.2f, 0.2f));
            PlaceButton(decline.GetComponent<RectTransform>(), 0.08f, 0.46f, 0.04f, 0.14f);

            Button accept = AddButton(root.transform, "Accept", "Accept", new Color(0.2f, 0.35f, 0.25f));
            PlaceButton(accept.GetComponent<RectTransform>(), 0.54f, 0.92f, 0.04f, 0.14f);

            Assign(view, "_root", root);
            Assign(view, "_portraitImage", portrait);
            Assign(view, "_nameText", name);
            Assign(view, "_messageText", message);
            Assign(view, "_bodyText", body);
            Assign(view, "_declineButton", decline);
            Assign(view, "_acceptButton", accept);

            SavePrefab(root, "GeneralBriefingPanel.prefab");
        }

        private static void BuildRouteAdvice()
        {
            var root = CreatePanelRoot("GeneralRouteAdvicePanel", new Vector2(480, 420));
            var view = root.AddComponent<GeneralRouteAdviceView>();

            Image portrait = CreatePortrait(root.transform, "Portrait");
            TMP_Text name = AddTmp(root.transform, "Name", "ROUTE APPROVAL", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 0.86f, 0.95f, 96, 16);

            TMP_Text message = AddTmp(root.transform, "Message", "Route advice…", 15, FontStyles.Italic, TextAlignmentOptions.TopLeft);
            Stretch(message.rectTransform, 0.74f, 0.86f, 16, 16);
            message.color = new Color(0.9f, 0.92f, 0.75f);

            TMP_Text body = AddTmp(root.transform, "Body", "Route details…", 14, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Stretch(body.rectTransform, 0.18f, 0.73f, 16, 16);

            Button back = AddButton(root.transform, "Back", "Back", new Color(0.3f, 0.3f, 0.35f));
            PlaceButton(back.GetComponent<RectTransform>(), 0.08f, 0.46f, 0.04f, 0.14f);

            Button approve = AddButton(root.transform, "ApproveMarch", "Approve & March", new Color(0.2f, 0.35f, 0.4f));
            PlaceButton(approve.GetComponent<RectTransform>(), 0.54f, 0.92f, 0.04f, 0.14f);

            Assign(view, "_root", root);
            Assign(view, "_portraitImage", portrait);
            Assign(view, "_nameText", name);
            Assign(view, "_messageText", message);
            Assign(view, "_bodyText", body);
            Assign(view, "_backButton", back);
            Assign(view, "_approveMarchButton", approve);

            SavePrefab(root, "GeneralRouteAdvicePanel.prefab");
        }

        private static void BuildMonitoring()
        {
            var root = CreatePanelRoot("GeneralMonitoringPanel", new Vector2(340, 260));
            var view = root.AddComponent<GeneralMonitoringView>();

            Image portrait = CreatePortrait(root.transform, "Portrait", 56f);
            TMP_Text name = AddTmp(root.transform, "Name", "MARCH STATUS", 16, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(name.rectTransform, 0.82f, 0.95f, 72, 10);

            TMP_Text message = AddTmp(root.transform, "Message", "Monitoring…", 13, FontStyles.Italic, TextAlignmentOptions.TopLeft);
            Stretch(message.rectTransform, 0.62f, 0.82f, 12, 12);
            message.color = new Color(0.9f, 0.92f, 0.75f);

            TMP_Text status = AddTmp(root.transform, "Status", "Status…", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Stretch(status.rectTransform, 0.28f, 0.62f, 12, 12);

            Button rest = AddButton(root.transform, "Rest", "Rest", new Color(0.25f, 0.32f, 0.4f));
            PlaceButton(rest.GetComponent<RectTransform>(), 0.08f, 0.46f, 0.06f, 0.22f);

            Button resume = AddButton(root.transform, "Resume", "Resume March", new Color(0.3f, 0.35f, 0.22f));
            PlaceButton(resume.GetComponent<RectTransform>(), 0.54f, 0.92f, 0.06f, 0.22f);

            Assign(view, "_root", root);
            Assign(view, "_portraitImage", portrait);
            Assign(view, "_nameText", name);
            Assign(view, "_messageText", message);
            Assign(view, "_statusText", status);
            Assign(view, "_restButton", rest);
            Assign(view, "_resumeButton", resume);

            SavePrefab(root, "GeneralMonitoringPanel.prefab");
        }

        private static Image CreatePortrait(Transform parent, string name, float size = 72f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(12f, -12f);
            rt.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.45f, 0.42f, 0.35f, 1f);
            return image;
        }

        private static GameObject CreatePanelRoot(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static Button AddButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            TMP_Text text = AddTmp(go.transform, "Label", label, 15, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f, 1f, 4, 4);
            return go.GetComponent<Button>();
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

        private static void PlaceMonitoring(GameObject go)
        {
            if (go == null)
                return;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
                return;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(16f, 16f);
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
