using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Troops;
using TGS;
using UnityEngine;

namespace Game.Campaign.UI
{
    /// <summary>
    /// Campaign placeholder UX. Armed from Develop Mode → Campaign Debug.
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

        [Header("Main UX")]
        [SerializeField] private CampaignOfferPopupView _offerPopup;
        [SerializeField] private CampaignTroopSelectionPopupView _troopSelectionPopup;
        [SerializeField] private CampaignActivePanelView _activePanel;
        [SerializeField] private CampaignResultPopupView _resultPopup;

        [Header("Test only")]
        [SerializeField] private CampaignTestControlsView _testControls;

        [Header("State")]
        [SerializeField] private bool _testingArmed;

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
        /// Arms/disarms Campaign testing (map tap → Offer, Active/Result UI).
        /// Enter Campaign Debug to arm; switch to another Develop Mode tab to disarm.
        /// </summary>
        public void SetTestingArmed(bool armed)
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
                if (_testControls != null)
                    _testControls.SetVisible(false);
                Debug.Log("[Campaign] Testing disarmed — map offers off.");
                return;
            }

            SubscribeService(true);
            if (_testControls != null)
                _testControls.SetVisible(true);
            RefreshVisibleStateFromCampaign();
            Debug.Log("[Campaign] Testing armed — Test Controls stay on; close Develop Mode to tap the map.");
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
            if (campaign == null || _activePanel == null)
                return;

            if (campaign.LifecycleState == CampaignLifecycleState.Offered &&
                campaign.OwnerResponse == CampaignResponse.Accepted)
            {
                RefreshActivePanel(campaign);
            }
            else if (campaign.LifecycleState == CampaignLifecycleState.Active)
            {
                RefreshActivePanel(campaign);
            }
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
            _troopSelectionPopup?.Hide();
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

            if (_troopSelectionPopup != null)
            {
                if (bind)
                {
                    _troopSelectionPopup.BackClicked += OnTroopBack;
                    _troopSelectionPopup.MarchClicked += OnMarch;
                }
                else
                {
                    _troopSelectionPopup.BackClicked -= OnTroopBack;
                    _troopSelectionPopup.MarchClicked -= OnMarch;
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

        private void OnAdvance2h() => AdvanceHours(2);

        private void OnAdvance6h() => AdvanceHours(6);

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

        private void OnCampaignOffered(Campaign campaign)
        {
            if (!_testingArmed || campaign == null || _offerPopup == null)
                return;

            // Clear stale Active panel from the previous Campaign (old Last Battle / Silver).
            _activePanel?.Hide();
            _troopSelectionPopup?.Hide();
            _resultPopup?.Hide();

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
            ShowTroopSelection();
        }

        private void OnDecline()
        {
            if (!_testingArmed)
                return;

            _service?.OwnerDecline();
            _offerPopup?.Hide();
        }

        private void OnCampaignAccepted(Campaign campaign)
        {
        }

        private void OnCampaignDeclined(Campaign campaign)
        {
            if (!_testingArmed)
                return;

            _offerPopup?.Hide();
            _troopSelectionPopup?.Hide();
            _activePanel?.Hide();
        }

        private void ShowTroopSelection()
        {
            if (!_testingArmed || _troopSelectionPopup == null)
                return;

            IReadOnlyList<TroopUnit> available = _roster != null
                ? _roster.GetAvailableUnits()
                : System.Array.Empty<TroopUnit>();

            _troopSelectionPopup.Show(available);
        }

        private void OnTroopBack()
        {
            if (!_testingArmed)
                return;

            _troopSelectionPopup?.Hide();
            Campaign campaign = _service != null ? _service.ActiveCampaign : null;
            if (campaign != null && _offerPopup != null)
            {
                string label = CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData);
                _offerPopup.Show(label, CampaignTerritoryLabel.GetObjectivePlaceholder());
            }
        }

        private void OnMarch()
        {
            if (!_testingArmed || _service == null || _troopSelectionPopup == null)
                return;

            List<TroopUnit> selected = _troopSelectionPopup.GetSelectedUnits();
            if (selected.Count == 0)
                return;

            if (!_service.MarchSelectedOwnerUnits(selected))
                return;

            _troopSelectionPopup.Hide();
            _activePanel?.Show();
            RefreshActivePanel(_service.ActiveCampaign);
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

        private void AdvanceHours(int hours)
        {
            if (!_testingArmed)
                return;
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            _worldTime?.Advance(hours * (double)WorldTime.SecondsPerHour);
        }

        private void OnCompleteNow()
        {
            if (!_testingArmed)
                return;
            _service?.CompleteCampaign();
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
