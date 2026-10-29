using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Travel;
using Game.Seasons.DebugTools;
using Game.Battle.DebugTools;
using Game.Campaign.DebugTools;
using Game.Campaign.UI;
using Game.General.UI;
using Game.Navigation;
using Game.UI;

public sealed class DevelopModeController : MonoBehaviour
{
    [Header("Sidebar")]
    [SerializeField] private Button _movementButton;
    [SerializeField] private Button _environmentButton;
    [SerializeField] private Button _travelDebugButton;
    [SerializeField] private Button _seasonDebugButton;
    [SerializeField] private Button _battleDebugButton;
    [SerializeField] private Button _campaignDebugButton;
    [SerializeField] private Button _generalButton;

    [Header("Content Panels")]
    [SerializeField] private GameObject _movementPanel;
    [SerializeField] private GameObject _environmentPanel;
    [SerializeField] private GameObject _travelDebugPanel;
    [SerializeField] private GameObject _seasonDebugPanel;
    [SerializeField] private GameObject _battleDebugPanel;
    [SerializeField] private GameObject _campaignDebugPanel;

    [Header("Travel Debug")]
    [SerializeField] private Toggle _showTravelOverlayToggle;
    [SerializeField] private TravelDebugController _travelDebug;

    [Header("Season Debug")]
    [SerializeField] private Toggle _showSeasonOverlayToggle;
    [SerializeField] private SeasonDebugController _seasonDebug;

    [Header("Battle Debug")]
    [SerializeField] private Toggle _showBattleOverlayToggle;
    [SerializeField] private BattleTestController _battleDebug;

    [Header("Campaign Debug")]
    [SerializeField] private Toggle _showCampaignOverlayToggle;
    [SerializeField] private CampaignDebugController _campaignDebug;
    [SerializeField] private CampaignFlowController _campaignFlow;
    [Tooltip("Legacy OnGUI overlay; keep off — use CampaignHudRoot Unity UI instead.")]
    [SerializeField] private bool _enableLegacyCampaignOnGui;

    [Header("General Intro")]
    [SerializeField] private GeneralIntroPanelView _generalIntroPanel;
    [Tooltip("Prefab asset at Assets/Scripts/Game/General/Prefabs/GeneralIntroPanel.prefab")]
    [SerializeField] private GeneralIntroPanelView _generalIntroPrefab;
    [SerializeField] private Sprite _generalPortrait;
    [SerializeField] private NavigationController _navigation;

    private const string GeneralIntroPrefabAssetPath =
        "Assets/Scripts/Game/General/Prefabs/GeneralIntroPanel.prefab";
    private const string GeneralIntroPrefabResourcePath = "General/GeneralIntroPanel";

    [Header("Movement Settings")]
    [SerializeField] private MarbleMovement _marbleMovement;

    [Header("Optional Title")]
    [SerializeField] private Text _contentTitle;

    [SerializeField] private ModeType _defaultMode = ModeType.Movement;

    private ModeType _currentMode = ModeType.Movement;

    public ModeType CurrentMode => _currentMode;

    private void Awake()
    {
        if (_travelDebug == null)
            _travelDebug = FindFirstObjectByType<TravelDebugController>();

        if (_battleDebug == null)
            _battleDebug = FindFirstObjectByType<BattleTestController>();

        if (_campaignDebug == null)
            _campaignDebug = FindFirstObjectByType<CampaignDebugController>();

        if (_campaignFlow == null)
            _campaignFlow = FindFirstObjectByType<CampaignFlowController>(FindObjectsInactive.Include);

        if (_marbleMovement == null)
            _marbleMovement = FindFirstObjectByType<MarbleMovement>();

        if (_showTravelOverlayToggle != null)
        {
            _showTravelOverlayToggle.onValueChanged.RemoveListener(SetTravelOverlayVisible);
            _showTravelOverlayToggle.onValueChanged.AddListener(SetTravelOverlayVisible);

            if (_travelDebug != null)
                _showTravelOverlayToggle.SetIsOnWithoutNotify(_travelDebug.ShowOverlay);
        }

        if (_showSeasonOverlayToggle != null)
        {
            _showSeasonOverlayToggle.onValueChanged.RemoveListener(SetSeasonOverlayVisible);
            _showSeasonOverlayToggle.onValueChanged.AddListener(SetSeasonOverlayVisible);
            _showSeasonOverlayToggle.SetIsOnWithoutNotify(false);
        }

        if (_showBattleOverlayToggle != null)
        {
            _showBattleOverlayToggle.onValueChanged.RemoveListener(SetBattleOverlayVisible);
            _showBattleOverlayToggle.onValueChanged.AddListener(SetBattleOverlayVisible);
            _showBattleOverlayToggle.SetIsOnWithoutNotify(false);
        }

        if (_showCampaignOverlayToggle != null)
        {
            _showCampaignOverlayToggle.onValueChanged.RemoveListener(SetCampaignOverlayVisible);
            _showCampaignOverlayToggle.onValueChanged.AddListener(SetCampaignOverlayVisible);
            _showCampaignOverlayToggle.SetIsOnWithoutNotify(false);
        }

        if (_battleDebug != null)
            _battleDebug.ShowOverlay = false;

        if (_campaignDebug != null)
            _campaignDebug.ShowOverlay = false;

        EnsureCampaignFlow();
        _campaignFlow?.SetTestingArmed(false);
        _campaignFlow?.SetTestControlsVisible(false);

        if (_navigation == null)
            _navigation = FindFirstObjectByType<NavigationController>();

        EnsureGeneralIntroPanel();
        WireGeneralIntroPanel(true);

        WireSidebarButtons();
        SetMode(_defaultMode);
    }

    /// <summary>
    /// Rebind sidebar clicks at runtime. Cloned buttons keep Inspector persistent
    /// onClick from the template (e.g. Campaign Debug cloned from Movement still
    /// called SetMovementMode). RemoveAllListeners() does not clear those.
    /// </summary>
    private void WireSidebarButtons()
    {
        EnsureCampaignDebugSidebarButton();
        EnsureGeneralSidebarButton();

        if (_campaignDebugButton == null)
            _campaignDebugButton = FindSidebarButtonByName("CampaignDebugButton", "Campaign Debug");
        if (_generalButton == null)
            _generalButton = FindSidebarButtonByName("GeneralButton", "General");

        BindButton(_movementButton, SetMovementMode);
        BindButton(_environmentButton, SetEnvironmentMode);
        BindButton(_travelDebugButton, SetTravelDebugMode);
        BindButton(_seasonDebugButton, SetSeasonDebugMode);
        BindButton(_battleDebugButton, SetBattleDebugMode);
        BindButton(_campaignDebugButton, SetCampaignDebugMode);
        BindButton(_generalButton, OpenGeneralIntro);
    }

    /// <summary>
    /// Scene may not have Campaign Debug yet — clone from an existing sidebar button at runtime.
    /// </summary>
    private void EnsureCampaignDebugSidebarButton()
    {
        if (_campaignDebugButton != null)
            return;

        Button existing = FindSidebarButtonByName("CampaignDebugButton", "Campaign Debug");
        if (existing != null)
        {
            _campaignDebugButton = existing;
            return;
        }

        Transform sidebar = FindSidebarTransform();
        if (sidebar == null)
        {
            Debug.LogWarning("[DevelopMode] Sidebar not found — cannot create Campaign Debug button.");
            return;
        }

        Button template = _battleDebugButton != null
            ? _battleDebugButton
            : _seasonDebugButton != null
                ? _seasonDebugButton
                : sidebar.GetComponentInChildren<Button>(true);

        if (template == null)
            return;

        GameObject buttonGo = Instantiate(template.gameObject, sidebar);
        buttonGo.name = "CampaignDebugButton";
        buttonGo.SetActive(true);

        SetSidebarButtonLabel(buttonGo, "Campaign Debug");
        _campaignDebugButton = buttonGo.GetComponent<Button>();
        Debug.Log("[DevelopMode] Created missing Campaign Debug sidebar button at runtime.");
    }

    /// <summary>
    /// Scene may not have General sidebar button yet — clone from an existing sidebar button.
    /// </summary>
    private void EnsureGeneralSidebarButton()
    {
        if (_generalButton != null)
            return;

        Button existing = FindSidebarButtonByName("GeneralButton", "General");
        if (existing != null)
        {
            _generalButton = existing;
            return;
        }

        Transform sidebar = FindSidebarTransform();
        if (sidebar == null)
        {
            Debug.LogWarning("[DevelopMode] Sidebar not found — cannot create General button.");
            return;
        }

        Button template = _campaignDebugButton != null
            ? _campaignDebugButton
            : _battleDebugButton != null
                ? _battleDebugButton
                : sidebar.GetComponentInChildren<Button>(true);

        if (template == null)
            return;

        GameObject buttonGo = Instantiate(template.gameObject, sidebar);
        buttonGo.name = "GeneralButton";
        buttonGo.SetActive(true);
        SetSidebarButtonLabel(buttonGo, "General");
        _generalButton = buttonGo.GetComponent<Button>();
        Debug.Log("[DevelopMode] Created missing General sidebar button at runtime.");
    }

    private Transform FindSidebarTransform()
    {
        if (_battleDebugButton != null)
            return _battleDebugButton.transform.parent;
        if (_seasonDebugButton != null)
            return _seasonDebugButton.transform.parent;
        if (_travelDebugButton != null)
            return _travelDebugButton.transform.parent;
        if (_movementButton != null)
            return _movementButton.transform.parent;
        return null;
    }

    private static void SetSidebarButtonLabel(GameObject buttonGo, string text)
    {
        Text legacy = buttonGo.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            legacy.text = text;
            return;
        }

        TMP_Text tmp = buttonGo.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = text;
    }

    private Button FindSidebarButtonByName(params string[] names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            string goName = button.gameObject.name;
            for (int n = 0; n < names.Length; n++)
            {
                if (goName == names[n])
                    return button;
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (label.text == names[n])
                        return button;
                }
            }

            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                for (int n = 0; n < names.Length; n++)
                {
                    if (tmp.text == names[n])
                        return button;
                }
            }
        }

        return null;
    }

    private static void BindButton(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        // Replace the UnityEvent so Inspector persistent listeners cannot override mode.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private void OnEnable()
    {
        if (_showSeasonOverlayToggle != null && _seasonDebug != null)
            _showSeasonOverlayToggle.SetIsOnWithoutNotify(_seasonDebug.ShowOverlay);

        if (_showBattleOverlayToggle != null && _battleDebug != null)
            _showBattleOverlayToggle.SetIsOnWithoutNotify(_battleDebug.ShowOverlay);

        if (_showCampaignOverlayToggle != null)
        {
            bool armed = _campaignFlow != null && _campaignFlow.IsTestingArmed;
            _showCampaignOverlayToggle.SetIsOnWithoutNotify(armed && _currentMode == ModeType.CampaignDebug);
        }

        // Re-open Develop Mode: if still on Campaign Debug, keep testing armed.
        if (_currentMode == ModeType.CampaignDebug)
        {
            EnsureCampaignFlow();
            _campaignFlow?.SetTestingArmed(true);
        }
    }

    private void OnDisable()
    {
        // Closing Develop Mode must NOT hide Campaign Test Controls if testing is armed.
        // User needs Force Battle / +Hours while looking at the map (Develop Mode panel blocks it).
        // Disarm only when switching away from Campaign Debug tab or destroying DevelopMode.
    }

    private void OnDestroy()
    {
        if (_showTravelOverlayToggle != null)
            _showTravelOverlayToggle.onValueChanged.RemoveListener(SetTravelOverlayVisible);

        if (_showSeasonOverlayToggle != null)
            _showSeasonOverlayToggle.onValueChanged.RemoveListener(SetSeasonOverlayVisible);

        if (_showBattleOverlayToggle != null)
            _showBattleOverlayToggle.onValueChanged.RemoveListener(SetBattleOverlayVisible);

        if (_showCampaignOverlayToggle != null)
            _showCampaignOverlayToggle.onValueChanged.RemoveListener(SetCampaignOverlayVisible);

        WireGeneralIntroPanel(false);
        EnsureCampaignFlow();
        _campaignFlow?.SetTestingArmed(false);
    }

    public void SetMovementMode()
    {
        SetMode(ModeType.Movement);
    }

    public void SetEnvironmentMode()
    {
        SetMode(ModeType.Environment);
    }

    public void SetTravelDebugMode()
    {
        SetMode(ModeType.TravelDebug);
    }

    public void SetSeasonDebugMode()
    {
        SetMode(ModeType.SeasonDebug);
    }

    public void SetBattleDebugMode()
    {
        SetMode(ModeType.BattleDebug);
    }

    public void SetCampaignDebugMode()
    {
        SetMode(ModeType.CampaignDebug);
    }

    /// <summary>
    /// Develop Mode → General: close Develop panel, show General intro artwork.
    /// </summary>
    public void OpenGeneralIntro()
    {
        EnsureGeneralIntroPanel();
        WireGeneralIntroPanel(true);

        UiOverlayExclusive.Instance?.CloseDevelopModeUi();

        if (_generalIntroPanel == null)
        {
            Debug.LogWarning(
                "[DevelopMode] GeneralIntroPanel missing. Run Tools/General/Wire General Intro Panel.");
            return;
        }

        _generalIntroPanel.Show(_generalPortrait);
        Debug.Log("[DevelopMode] General intro opened.");
    }

    private void OnGeneralOpenNavigation()
    {
        _generalIntroPanel?.Hide();

        // Same session as Develop Mode → Campaign: arm map offers + General
        // Briefing → Troops → Route Advice → March (not legacy Plan March tap).
        EnsureCampaignFlow();
        if (_campaignFlow == null)
        {
            Debug.LogWarning(
                "[DevelopMode] CampaignFlowController missing — cannot start General Campaign flow. " +
                "Run Tools/Campaign/Create Campaign UI In Active Scene.");
            return;
        }

        if (_navigation == null)
            _navigation = FindFirstObjectByType<NavigationController>();
        _navigation?.SetDestinationSelectMode(false);

        // Arm map-offer / General campaign flow only — do not show Campaign Test Controls.
        _campaignFlow.SetTestingArmed(true, showTestControls: false);
        UiOverlayExclusive.Instance?.CloseDevelopModeUi();
        Debug.Log("[DevelopMode] General Open Navigation → Campaign map-offer flow armed (no Test Controls).");
    }

    private void OnGeneralIntroClosed()
    {
        _generalIntroPanel?.Hide();
    }

    private void WireGeneralIntroPanel(bool bind)
    {
        if (_generalIntroPanel == null)
            return;

        _generalIntroPanel.OpenNavigationClicked -= OnGeneralOpenNavigation;
        _generalIntroPanel.CloseClicked -= OnGeneralIntroClosed;
        if (!bind)
            return;

        _generalIntroPanel.OpenNavigationClicked += OnGeneralOpenNavigation;
        _generalIntroPanel.CloseClicked += OnGeneralIntroClosed;
    }

    private void EnsureGeneralIntroPanel()
    {
        if (_generalIntroPanel != null)
            return;

        _generalIntroPanel = FindFirstObjectByType<GeneralIntroPanelView>(FindObjectsInactive.Include);
        if (_generalIntroPanel != null)
            return;

        GeneralIntroPanelView prefab = ResolveGeneralIntroPrefab();
        if (prefab == null)
        {
            Debug.LogWarning(
                "[DevelopMode] GeneralIntroPanel prefab missing. Assign _generalIntroPrefab on " +
                "DevelopModeController, or run Tools/General/Wire General Intro Panel.");
            return;
        }

        Transform parent = ResolveGeneralIntroParent();
        _generalIntroPanel = Instantiate(prefab, parent);
        _generalIntroPanel.name = "GeneralIntroPanel";
        _generalIntroPanel.gameObject.SetActive(false);
        Debug.Log("[DevelopMode] Instantiated GeneralIntroPanel from prefab asset.");
    }

    private static Transform ResolveGeneralIntroParent()
    {
        GameObject hud = GameObject.Find("CampaignHudRoot");
        if (hud != null)
            return hud.transform;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
            return canvas.transform;

        return null;
    }

    private GeneralIntroPanelView ResolveGeneralIntroPrefab()
    {
        if (_generalIntroPrefab != null)
            return _generalIntroPrefab;

#if UNITY_EDITOR
        GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GeneralIntroPrefabAssetPath);
        if (asset != null)
        {
            _generalIntroPrefab = asset.GetComponent<GeneralIntroPanelView>();
            if (_generalIntroPrefab != null)
                return _generalIntroPrefab;
        }
#endif

        GameObject resource = Resources.Load<GameObject>(GeneralIntroPrefabResourcePath);
        if (resource != null)
            _generalIntroPrefab = resource.GetComponent<GeneralIntroPanelView>();

        return _generalIntroPrefab;
    }

    public void SetTravelOverlayVisible(bool visible)
    {
        if (_travelDebug == null)
            _travelDebug = FindFirstObjectByType<TravelDebugController>();

        if (_travelDebug != null)
            _travelDebug.ShowOverlay = visible;
    }

    public void SetSeasonOverlayVisible(bool visible)
    {
        if (_seasonDebug != null)
            _seasonDebug.ShowOverlay = visible;
    }

    public void SetBattleOverlayVisible(bool visible)
    {
        if (_battleDebug == null)
            _battleDebug = FindFirstObjectByType<BattleTestController>();

        if (_battleDebug != null)
            _battleDebug.ShowOverlay = visible;
    }

    public void SetCampaignOverlayVisible(bool visible)
    {
        EnsureCampaignFlow();
        if (visible)
        {
            if (_currentMode != ModeType.CampaignDebug)
                SetMode(ModeType.CampaignDebug);
            else
                _campaignFlow?.SetTestingArmed(true);
        }
        else
        {
            _campaignFlow?.SetTestingArmed(false);
        }

        if (_enableLegacyCampaignOnGui)
        {
            if (_campaignDebug == null)
                _campaignDebug = FindFirstObjectByType<CampaignDebugController>();
            if (_campaignDebug != null)
                _campaignDebug.ShowOverlay = visible;
        }
    }

    public void SetMode(ModeType mode)
    {
        if (_currentMode == ModeType.Movement && mode != ModeType.Movement)
            _marbleMovement?.SaveMovementSettingsFromUi();

        _currentMode = mode;

        if (_movementPanel != null)
            _movementPanel.SetActive(mode == ModeType.Movement);

        if (_environmentPanel != null)
            _environmentPanel.SetActive(mode == ModeType.Environment);

        if (_travelDebugPanel != null)
            _travelDebugPanel.SetActive(mode == ModeType.TravelDebug);

        if (_seasonDebugPanel != null)
            _seasonDebugPanel.SetActive(mode == ModeType.SeasonDebug);

        if (_battleDebugPanel != null)
            _battleDebugPanel.SetActive(mode == ModeType.BattleDebug);

        if (_campaignDebugPanel != null)
            _campaignDebugPanel.SetActive(mode == ModeType.CampaignDebug);

        EnsureCampaignFlow();
        if (mode == ModeType.CampaignDebug)
        {
            // Arm map offers + Test Controls (stay visible even after Develop Mode is closed).
            _campaignFlow?.SetTestingArmed(true);
        }
        else
        {
            // Leaving Campaign Debug tab disarms map offers and hides Test Controls.
            _campaignFlow?.SetTestingArmed(false);
        }

        if (_campaignDebug != null)
            _campaignDebug.ShowOverlay = _enableLegacyCampaignOnGui && mode == ModeType.CampaignDebug;

        if (_showCampaignOverlayToggle != null)
            _showCampaignOverlayToggle.SetIsOnWithoutNotify(mode == ModeType.CampaignDebug);

        if (mode == ModeType.Movement)
            _marbleMovement?.ApplyMovementSettingsToUi();

        if (_contentTitle != null)
        {
            _contentTitle.text = mode switch
            {
                ModeType.Movement => "Movement Settings",
                ModeType.Environment => "Environment",
                ModeType.TravelDebug => "Travel Debug",
                ModeType.SeasonDebug => "Season Debug",
                ModeType.BattleDebug => "Battle Debug",
                ModeType.CampaignDebug => "Campaign Debug",
                _ => "Develop Mode"
            };
        }
    }

    private void SetCampaignDevelopSession(bool active)
    {
        EnsureCampaignFlow();
        if (_campaignFlow == null && active)
        {
            Debug.LogWarning(
                "[DevelopMode] CampaignHudRoot / CampaignFlowController not found in scene. " +
                "Run menu: Tools/Campaign/Create Campaign UI In Active Scene");
            return;
        }

        _campaignFlow?.SetDevelopModeSessionActive(active);
    }

    private void EnsureCampaignFlow()
    {
        if (_campaignFlow == null)
            _campaignFlow = FindFirstObjectByType<CampaignFlowController>(FindObjectsInactive.Include);
    }
}
