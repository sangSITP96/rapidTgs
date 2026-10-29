using System.Collections.Generic;
using System.Text;
using Game.Core.WorldTime;
using Game.General;
using Game.General.UI;
using Game.Navigation;
using Game.Travel;
using Game.Troops;
using TGS;
using UnityEngine;

namespace Game.Campaign.UI
{
    /// <summary>
    /// Campaign placeholder UX + Phase 18 General advisor integration.
    /// Armed from Develop Mode → Campaign Debug.
    /// Closing Develop Mode hides Test Controls but keeps map offers armed
    /// so the player can tap the Colony Map (Develop Mode UI otherwise blocks the map).
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class CampaignFlowController : MonoBehaviour
    {
        [SerializeField] private CampaignService _service;
        [SerializeField] private TemporaryAlphaTroopRoster _roster;
        [SerializeField] private CampaignMapOfferHook _mapOfferHook;
        [SerializeField] private TgsBiomeMapData _biomeMapData;
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private GeneralAdvisor _generalAdvisor;
        [SerializeField] private NavigationController _navigation;
        [SerializeField] private TroopTravelController _travel;

        [Header("Main UX")]
        [SerializeField] private CampaignOfferPopupView _offerPopup;
        [SerializeField] private GeneralBriefingView _generalBriefing;
        [SerializeField] private CampaignTroopSelectionPopupView _troopSelectionPopup;
        [SerializeField] private GeneralRouteAdviceView _generalRouteAdvice;
        [SerializeField] private GeneralMonitoringView _generalMonitoring;
        [SerializeField] private CampaignActivePanelView _activePanel;
        [SerializeField] private CampaignResultPopupView _resultPopup;
        [SerializeField] private Sprite _generalPortrait;

        [Header("Test only")]
        [SerializeField] private CampaignTestControlsView _testControls;

        [Header("State")]
        [SerializeField] private bool _testingArmed;

        [Header("General Briefing")]
        [Tooltip("When true and GeneralBriefingView is assigned, Offer uses General briefing instead of legacy offer popup.")]
        [SerializeField] private bool _useGeneralBriefing = true;

        private List<TroopUnit> _pendingSelectedUnits = new List<TroopUnit>();
        private NavigationRoute _pendingApprovedRoute;
        private float _monitoringRefreshTimer;

        /// <summary>Map offers + Campaign popups + Test Controls are live.</summary>
        public bool IsTestingArmed => _testingArmed;

        /// <summary>Backward-compatible alias for DevelopModeController.</summary>
        public bool DevelopModeSessionActive => _testingArmed;

        private void Awake()
        {
            ResolveRefs();
            WireUi(true);
            SetTestingArmed(false);
        }

        private void OnEnable()
        {
            ResolveRefs();
            if (_testingArmed)
                SubscribeService(true);
        }

        private void OnDisable()
        {
            SubscribeService(false);
        }

        private void OnDestroy()
        {
            WireUi(false);
        }

        /// <summary>
        /// Arms/disarms Campaign map offers + flow UI.
        /// Test Controls show only when <paramref name="showTestControls"/> is true
        /// (Develop Mode → Campaign). General → Open Navigation arms without Test Controls.
        /// </summary>
        public void SetTestingArmed(bool armed, bool showTestControls = true)
        {
            ResolveRefs();
            _testingArmed = armed;

            if (_mapOfferHook == null)
                _mapOfferHook = FindFirstObjectByType<CampaignMapOfferHook>();
            if (_mapOfferHook != null)
                _mapOfferHook.HookEnabled = armed;

            if (!armed)
            {
                SubscribeService(false);
                HideAllUi();
                ClearRoutePreviewVisuals();
                if (_testControls != null)
                    _testControls.SetVisible(false);
                Debug.Log("[Campaign] Testing disarmed — map offers off.");
                return;
            }

            SubscribeService(true);
            if (_testControls != null)
                _testControls.SetVisible(showTestControls);
            RefreshVisibleStateFromCampaign();
            Debug.Log(
                showTestControls
                    ? "[Campaign] Testing armed — Test Controls on; close Develop Mode to tap the map."
                    : "[Campaign] Testing armed — map offers on; Test Controls hidden.");
        }

        /// <summary>Legacy alias — same as SetTestingArmed.</summary>
        public void SetDevelopModeSessionActive(bool active) => SetTestingArmed(active);

        /// <summary>Legacy no-op alias; visibility follows armed state.</summary>
        public void SetTestControlsVisible(bool visible)
        {
            if (_testControls != null && _testingArmed)
                _testControls.SetVisible(visible);
        }

        private void Update()
        {
            if (!_testingArmed)
                return;

            Campaign campaign = _service != null ? _service.ActiveCampaign : null;
            if (campaign == null)
            {
                _generalMonitoring?.Hide();
                return;
            }

            if (campaign.LifecycleState == CampaignLifecycleState.Offered &&
                campaign.OwnerResponse == CampaignResponse.Accepted)
            {
                RefreshActivePanel(campaign);
                RefreshMonitoringIfTraveling();
            }
            else if (campaign.LifecycleState == CampaignLifecycleState.Active)
            {
                RefreshActivePanel(campaign);
                RefreshMonitoringIfTraveling();
            }
            else
            {
                _generalMonitoring?.Hide();
            }
        }

        private void RefreshMonitoringIfTraveling()
        {
            if (_generalAdvisor == null || _generalMonitoring == null || _travel == null)
                return;

            TravelStatus status = _travel.Status;
            bool show =
                status == TravelStatus.Marching ||
                status == TravelStatus.Resting ||
                status == TravelStatus.Camped;

            if (!show)
            {
                _generalMonitoring.Hide();
                return;
            }

            _monitoringRefreshTimer -= Time.unscaledDeltaTime;
            if (_monitoringRefreshTimer > 0f && _generalMonitoring.gameObject.activeSelf)
                return;

            _monitoringRefreshTimer = 0.5f;
            GeneralMonitoringAdvice advice = _generalAdvisor.BuildMonitoringAdvice();
            if (!_generalMonitoring.gameObject.activeSelf)
                _generalMonitoring.Show("MARCH STATUS", advice, _generalPortrait);
            else
                _generalMonitoring.Refresh("MARCH STATUS", advice, _generalPortrait);
        }

        private void ResolveRefs()
        {
            if (_service == null)
                _service = FindFirstObjectByType<CampaignService>();
            if (_roster == null)
                _roster = FindFirstObjectByType<TemporaryAlphaTroopRoster>();
            if (_mapOfferHook == null)
                _mapOfferHook = FindFirstObjectByType<CampaignMapOfferHook>();
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            if (_generalAdvisor == null)
                _generalAdvisor = FindFirstObjectByType<GeneralAdvisor>();
            if (_navigation == null)
                _navigation = FindFirstObjectByType<NavigationController>();
            if (_travel == null)
                _travel = FindFirstObjectByType<TroopTravelController>();

            if (_biomeMapData == null)
            {
                var gen = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (gen != null)
                    _biomeMapData = gen.MapData;
            }
        }

        private void HideAllUi()
        {
            _offerPopup?.Hide();
            _generalBriefing?.Hide();
            _troopSelectionPopup?.Hide();
            _generalRouteAdvice?.Hide();
            _generalMonitoring?.Hide();
            _activePanel?.Hide();
            _resultPopup?.Hide();
        }

        private void RefreshVisibleStateFromCampaign()
        {
            Campaign campaign = _service != null ? _service.ActiveCampaign : null;
            if (campaign == null)
                return;

            if (campaign.LifecycleState == CampaignLifecycleState.Offered &&
                campaign.OwnerResponse == CampaignResponse.Pending)
            {
                OnCampaignOffered(campaign);
                return;
            }

            if (campaign.LifecycleState == CampaignLifecycleState.Completed)
            {
                OnCampaignCompleted(campaign);
                return;
            }

            if (campaign.LifecycleState == CampaignLifecycleState.Active ||
                (campaign.LifecycleState == CampaignLifecycleState.Offered &&
                 campaign.OwnerResponse == CampaignResponse.Accepted))
            {
                _activePanel?.Show();
                RefreshActivePanel(campaign);
            }
        }

        private void WireUi(bool bind)
        {
            if (_offerPopup != null)
            {
                if (bind)
                {
                    _offerPopup.AcceptClicked += OnAccept;
                    _offerPopup.DeclineClicked += OnDecline;
                }
                else
                {
                    _offerPopup.AcceptClicked -= OnAccept;
                    _offerPopup.DeclineClicked -= OnDecline;
                }
            }

            if (_generalBriefing != null)
            {
                if (bind)
                {
                    _generalBriefing.AcceptClicked += OnAccept;
                    _generalBriefing.DeclineClicked += OnDecline;
                }
                else
                {
                    _generalBriefing.AcceptClicked -= OnAccept;
                    _generalBriefing.DeclineClicked -= OnDecline;
                }
            }

            if (_troopSelectionPopup != null)
            {
                if (bind)
                {
                    _troopSelectionPopup.BackClicked += OnTroopBack;
                    _troopSelectionPopup.MarchClicked += OnReviewRoute;
                }
                else
                {
                    _troopSelectionPopup.BackClicked -= OnTroopBack;
                    _troopSelectionPopup.MarchClicked -= OnReviewRoute;
                }
            }

            if (_generalRouteAdvice != null)
            {
                if (bind)
                {
                    _generalRouteAdvice.BackClicked += OnRouteBack;
                    _generalRouteAdvice.ApproveMarchClicked += OnApproveMarch;
                }
                else
                {
                    _generalRouteAdvice.BackClicked -= OnRouteBack;
                    _generalRouteAdvice.ApproveMarchClicked -= OnApproveMarch;
                }
            }

            if (_generalMonitoring != null)
            {
                if (bind)
                {
                    _generalMonitoring.RestClicked += OnMonitoringRest;
                    _generalMonitoring.ResumeClicked += OnMonitoringResume;
                }
                else
                {
                    _generalMonitoring.RestClicked -= OnMonitoringRest;
                    _generalMonitoring.ResumeClicked -= OnMonitoringResume;
                }
            }

            if (_resultPopup != null)
            {
                if (bind)
                    _resultPopup.OkClicked += OnResultOk;
                else
                    _resultPopup.OkClicked -= OnResultOk;
            }

            if (_testControls != null)
            {
                if (bind)
                {
                    _testControls.AddReinforcementClicked += OnAddReinforcement;
                    _testControls.ForceBattleClicked += OnForceBattle;
                    _testControls.Advance2hClicked += OnAdvance2h;
                    _testControls.Advance6hClicked += OnAdvance6h;
                    _testControls.CompleteNowClicked += OnCompleteNow;
                }
                else
                {
                    _testControls.AddReinforcementClicked -= OnAddReinforcement;
                    _testControls.ForceBattleClicked -= OnForceBattle;
                    _testControls.Advance2hClicked -= OnAdvance2h;
                    _testControls.Advance6hClicked -= OnAdvance6h;
                    _testControls.CompleteNowClicked -= OnCompleteNow;
                }
            }
        }

        private void OnCampaignOffered(Campaign campaign)
        {
            if (!_testingArmed || campaign == null)
                return;

            // Clear stale Active panel from the previous Campaign (old Last Battle / Silver).
            _activePanel?.Hide();
            _troopSelectionPopup?.Hide();
            _generalRouteAdvice?.Hide();
            _generalMonitoring?.Hide();
            _resultPopup?.Hide();
            _pendingSelectedUnits.Clear();
            _pendingApprovedRoute = null;
            ClearRoutePreviewVisuals();
            _service?.ClearPendingMarchRoute();

            if (_useGeneralBriefing && _generalBriefing != null && _generalAdvisor != null)
            {
                _offerPopup?.Hide();
                GeneralBriefingAdvice advice = _generalAdvisor.BuildCampaignBriefing(campaign);
                _generalBriefing.Show("CAMPAIGN SELECTION", advice, _generalPortrait);
                return;
            }

            _generalBriefing?.Hide();
            if (_offerPopup == null)
                return;

            string label = CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData);
            _offerPopup.Show(label, CampaignTerritoryLabel.GetObjectivePlaceholder());
        }

        private void OnBattleResolved(Campaign campaign, CampaignBattleRecord record)
        {
            if (!_testingArmed || campaign == null)
                return;

            _activePanel?.Show();
            RefreshActivePanel(campaign);
        }

        private void OnAccept()
        {
            if (!_testingArmed || _service == null)
                return;

            if (!_service.OwnerAccept())
                return;

            _offerPopup?.Hide();
            _generalBriefing?.Hide();
            ShowTroopSelection();
        }

        private void OnDecline()
        {
            if (!_testingArmed)
                return;

            _service?.OwnerDecline();
            _service?.ClearPendingMarchRoute();
            ClearRoutePreviewVisuals();
            _offerPopup?.Hide();
            _generalBriefing?.Hide();
            _troopSelectionPopup?.Hide();
            _generalRouteAdvice?.Hide();
            _pendingSelectedUnits.Clear();
            _pendingApprovedRoute = null;
        }

        private void OnCampaignAccepted(Campaign campaign)
        {
        }

        private void OnCampaignDeclined(Campaign campaign)
        {
            if (!_testingArmed)
                return;

            _offerPopup?.Hide();
            _generalBriefing?.Hide();
            _troopSelectionPopup?.Hide();
            _generalRouteAdvice?.Hide();
            _generalMonitoring?.Hide();
            _activePanel?.Hide();
            ClearRoutePreviewVisuals();
            _pendingSelectedUnits.Clear();
            _pendingApprovedRoute = null;
        }

        private void ShowTroopSelection()
        {
            if (!_testingArmed || _troopSelectionPopup == null)
                return;

            IReadOnlyList<TroopUnit> available = _roster != null
                ? _roster.GetAvailableUnits()
                : System.Array.Empty<TroopUnit>();

            string adviceText = null;
            if (_generalAdvisor != null)
            {
                GeneralTroopAdvice troopAdvice = _generalAdvisor.BuildTroopAdvice(_service != null ? _service.ActiveCampaign : null);
                adviceText = FormatTroopAdvice(troopAdvice);
            }

            _troopSelectionPopup.Show(available, adviceText);
        }

        private static string FormatTroopAdvice(GeneralTroopAdvice advice)
        {
            if (advice == null)
                return null;

            var sb = new StringBuilder();
            sb.Append("THE GENERAL\n");
            sb.Append(advice.GeneralMessage);
            if (advice.AvailableUnits != null && advice.AvailableUnits.Count > 0)
            {
                sb.Append("\n\nAvailable:");
                for (int i = 0; i < advice.AvailableUnits.Count; i++)
                {
                    AvailableTroopUnitSummary u = advice.AvailableUnits[i];
                    sb.Append("\n• ").Append(u.Name)
                        .Append(" — I:").Append(u.Composition.Infantry)
                        .Append(" C:").Append(u.Composition.Cavalry)
                        .Append(" A:").Append(u.Composition.Archers);
                }
            }

            if (!string.IsNullOrEmpty(advice.TroopConditionNote))
                sb.Append("\n\n").Append(advice.TroopConditionNote);
            if (!string.IsNullOrEmpty(advice.UncertaintyNote))
                sb.Append("\n").Append(advice.UncertaintyNote);
            return sb.ToString();
        }

        private void OnTroopBack()
        {
            if (!_testingArmed)
                return;

            _troopSelectionPopup?.Hide();
            _generalRouteAdvice?.Hide();
            ClearRoutePreviewVisuals();
            _service?.ClearPendingMarchRoute();
            _pendingSelectedUnits.Clear();
            _pendingApprovedRoute = null;

            Campaign campaign = _service != null ? _service.ActiveCampaign : null;
            if (campaign == null)
                return;

            if (_useGeneralBriefing && _generalBriefing != null && _generalAdvisor != null)
            {
                GeneralBriefingAdvice advice = _generalAdvisor.BuildCampaignBriefing(campaign);
                _generalBriefing.Show("CAMPAIGN SELECTION", advice, _generalPortrait);
                return;
            }

            if (_offerPopup != null)
            {
                string label = CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData);
                _offerPopup.Show(label, CampaignTerritoryLabel.GetObjectivePlaceholder());
            }
        }

        private void OnReviewRoute()
        {
            if (!_testingArmed || _service == null || _troopSelectionPopup == null)
                return;

            List<TroopUnit> selected = _troopSelectionPopup.GetSelectedUnits();
            if (selected.Count == 0)
                return;

            _pendingSelectedUnits = selected;
            _pendingApprovedRoute = null;

            if (!_service.TryPreviewOwnerMarchRoute(out NavigationRoute route, out string error))
            {
                GeneralRouteAdvice failed = _generalAdvisor != null
                    ? _generalAdvisor.BuildRouteAdvice(route, GetDestinationOrDefault())
                    : new GeneralRouteAdvice
                    {
                        IsValid = false,
                        FailureReason = error,
                        GeneralMessage = "Route preview failed.",
                        BodyLines = { error ?? "Route preview failed." }
                    };

                _troopSelectionPopup.Hide();
                _navigation?.ShowRoutePreview(route);
                if (_generalRouteAdvice != null)
                    _generalRouteAdvice.Show("ROUTE APPROVAL", failed, _generalPortrait);
                else
                    Debug.LogWarning($"[Campaign] Route preview failed: {error}");
                return;
            }

            Vector3 destination = route.Destination;
            GeneralRouteAdvice advice = _generalAdvisor != null
                ? _generalAdvisor.BuildRouteAdvice(route, destination)
                : new GeneralRouteAdvice
                {
                    IsValid = true,
                    Route = route,
                    ApproximateDistance = route.ApproximateDistance,
                    GeneralMessage = "Route ready for your approval.",
                    BodyLines =
                    {
                        $"Distance ≈ {route.ApproximateDistance:0.00}"
                    }
                };

            _pendingApprovedRoute = route;
            _troopSelectionPopup.Hide();
            _navigation?.ShowRoutePreview(route);
            if (_generalRouteAdvice != null)
            {
                _generalRouteAdvice.Show("ROUTE APPROVAL", advice, _generalPortrait);
            }
            else
            {
                // Fallback without route UI: do not auto-march — require view.
                Debug.LogWarning("[Campaign] GeneralRouteAdviceView missing — cannot approve march.");
            }
        }

        private Vector3 GetDestinationOrDefault()
        {
            if (_service != null && _service.TryGetCampaignDestination(out Vector3 dest))
                return dest;
            return Vector3.zero;
        }

        private void OnRouteBack()
        {
            if (!_testingArmed)
                return;

            _generalRouteAdvice?.Hide();
            ClearRoutePreviewVisuals();
            _service?.ClearPendingMarchRoute();
            _pendingApprovedRoute = null;
            ShowTroopSelection();

            // Re-select is cleared when popup rebuilds — Commander must select again.
            _pendingSelectedUnits.Clear();
        }

        private void OnApproveMarch()
        {
            if (!_testingArmed || _service == null)
                return;

            if (_pendingSelectedUnits == null || _pendingSelectedUnits.Count == 0)
            {
                Debug.LogWarning("[Campaign] Approve March ignored — no selected units.");
                return;
            }

            NavigationRoute approved = _pendingApprovedRoute;
            if (approved == null || !approved.IsValid)
                approved = _service.PendingMarchRoute;

            if (approved == null || !approved.IsValid)
            {
                Debug.LogWarning("[Campaign] Approve March ignored — no valid approved route.");
                return;
            }

            if (!_service.MarchSelectedOwnerUnits(_pendingSelectedUnits, approved))
                return;

            _generalRouteAdvice?.Hide();
            ClearRoutePreviewVisuals();
            _pendingSelectedUnits.Clear();
            _pendingApprovedRoute = null;
            _activePanel?.Show();
            RefreshActivePanel(_service.ActiveCampaign);
            _monitoringRefreshTimer = 0f;
            RefreshMonitoringIfTraveling();
        }

        private void OnMonitoringRest()
        {
            if (!_testingArmed || _travel == null)
                return;

            _travel.StartRest();
            _monitoringRefreshTimer = 0f;
            RefreshMonitoringIfTraveling();
        }

        private void OnMonitoringResume()
        {
            if (!_testingArmed || _travel == null)
                return;

            _travel.ResumeMarch();
            _monitoringRefreshTimer = 0f;
            RefreshMonitoringIfTraveling();
        }

        private void ClearRoutePreviewVisuals()
        {
            _navigation?.ClearRoutePreview();
        }

        private void OnCampaignActivated(Campaign campaign)
        {
            if (!_testingArmed)
                return;

            _activePanel?.Show();
            RefreshActivePanel(campaign);
        }

        private void OnCampaignChanged(Campaign campaign)
        {
            if (!_testingArmed || campaign == null)
                return;

            if (campaign.LifecycleState == CampaignLifecycleState.Active ||
                (campaign.LifecycleState == CampaignLifecycleState.Offered &&
                 campaign.OwnerResponse == CampaignResponse.Accepted))
            {
                _activePanel?.Show();
                RefreshActivePanel(campaign);
            }
        }

        private void OnCampaignCompleted(Campaign campaign)
        {
            if (!_testingArmed)
                return;

            _activePanel?.Hide();
            _generalMonitoring?.Hide();
            ClearRoutePreviewVisuals();
            if (_resultPopup == null || campaign == null || _service == null)
                return;

            int silver = CampaignRewardCalculator.CalculateSilverAmount(campaign, _service.Settings);
            _resultPopup.Show(campaign, silver);
        }

        private void OnResultOk()
        {
            if (!_testingArmed)
                return;

            _resultPopup?.Hide();
        }

        private void OnAddReinforcement()
        {
            if (!_testingArmed)
            {
                Debug.LogWarning("[Campaign] Add Reinforcement ignored — Campaign Test not armed (Develop Mode → Campaign Debug).");
                return;
            }

            ResolveRefs();
            if (_service == null)
            {
                Debug.LogWarning("[Campaign] Add Reinforcement failed — CampaignService missing.");
                return;
            }

            if (_roster == null)
            {
                Debug.LogWarning("[Campaign] Add Reinforcement failed — TemporaryAlphaTroopRoster missing.");
                return;
            }

            Campaign campaign = _service.ActiveCampaign;
            if (campaign == null)
            {
                Debug.LogWarning("[Campaign] Add Reinforcement failed — no active Campaign. Offer → Accept → March first.");
                return;
            }

            // Reinforcement is for an ongoing Campaign (Owner already InCamp / Active).
            if (campaign.LifecycleState != CampaignLifecycleState.Active)
            {
                Debug.LogWarning(
                    $"[Campaign] Add Reinforcement ignored — Campaign is {campaign.LifecycleState} " +
                    "(wait until Active / At Camp).");
                return;
            }

            const string supportId = "support-commander-b";
            _service.SupportAccept(supportId);
            TroopUnit unit = _roster.CreateReinforcementUnit(
                "Support Platoon",
                TroopUnitType.Platoon,
                16, 8, 8);
            var list = new List<TroopUnit> { unit };
            bool ok = _service.MarchSelectedSupportUnits(supportId, list);
            if (!ok)
            {
                Debug.LogWarning("[Campaign] Add Reinforcement failed — could not march/join Support unit.");
                return;
            }

            Debug.Log(
                $"[Campaign] Reinforcement added: {unit.Name} for {supportId}. " +
                $"InCamp total={_service.CountCommanderSideInCampTroops()} " +
                $"(Support={_service.CountSupportInCampTroops()}).");

            _activePanel?.Show();
            RefreshActivePanel(_service.ActiveCampaign);
        }

        private void OnForceBattle()
        {
            if (!_testingArmed)
                return;
            _service?.DebugForceResolveBattle();
        }

        private void OnAdvance2h() => AdvanceHours(2);

        private void OnAdvance6h() => AdvanceHours(6);

        private void AdvanceHours(int hours)
        {
            if (!_testingArmed)
                return;
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            _worldTime?.Advance(hours * (double)WorldTime.SecondsPerHour);
            _monitoringRefreshTimer = 0f;
            RefreshMonitoringIfTraveling();
        }

        private void OnCompleteNow()
        {
            if (!_testingArmed)
                return;
            _service?.CompleteCampaign();
        }

        private void SubscribeService(bool bind)
        {
            if (_service == null)
                return;

            // Avoid double-subscribe.
            _service.CampaignOffered -= OnCampaignOffered;
            _service.CampaignAccepted -= OnCampaignAccepted;
            _service.CampaignDeclined -= OnCampaignDeclined;
            _service.CampaignActivated -= OnCampaignActivated;
            _service.CampaignCompleted -= OnCampaignCompleted;
            _service.CampaignChanged -= OnCampaignChanged;
            _service.BattleResolved -= OnBattleResolved;

            if (!bind)
                return;

            _service.CampaignOffered += OnCampaignOffered;
            _service.CampaignAccepted += OnCampaignAccepted;
            _service.CampaignDeclined += OnCampaignDeclined;
            _service.CampaignActivated += OnCampaignActivated;
            _service.CampaignCompleted += OnCampaignCompleted;
            _service.CampaignChanged += OnCampaignChanged;
            _service.BattleResolved += OnBattleResolved;
        }

        private void RefreshActivePanel(Campaign campaign)
        {
            if (!_testingArmed || _activePanel == null || campaign == null || _service == null)
                return;

            string label = CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData);
            int silver = CampaignRewardCalculator.CalculateSilverAmount(campaign, _service.Settings);
            _activePanel.Refresh(
                campaign,
                _service.Settings,
                _worldTime,
                label,
                silver,
                _service.CountCommanderSideMarchingTroops(),
                _service.CountCommanderSideInCampTroops());
        }
    }
}
