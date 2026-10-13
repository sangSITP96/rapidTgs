using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Core.WorldTime;
using Game.Currency;
using Game.Navigation;
using Game.Travel;
using Game.Troops;
using TGS;
using UnityEngine;

namespace Game.Campaign
{
    /// <summary>
    /// Orchestrates Campaign lifecycle. Delegates battle, control, reward, and force aggregation.
    /// CreateCampaign / OfferAtTerritory = Offered only (not Active). Active starts on first Owner InCamp.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class CampaignService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private CampaignSettings _campaignSettings;
        [SerializeField] private BattleSettings _battleSettings;

        [Header("Refs")]
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private CurrencySystem _currency;
        [SerializeField] private NavigationController _navigation;
        [SerializeField] private TgsNavigationPathfinder _pathfinder;
        [SerializeField] private TroopTravelController _travel;
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private TemporaryAlphaTroopRoster _ownerRoster;

        [Header("Local / Debug Identity")]
        [Tooltip("CommanderId that maps to the local CurrencySystem wallet for TEMPORARY Alpha Owner payout.")]
        [SerializeField] private string _localCommanderId = "local-commander";

        [Header("Runtime")]
        [SerializeField] private Campaign _activeCampaign;

        private IFistForceSelector _fistForceSelector = new RandomEqualFistForceSelector();
        private readonly List<string> _activeMarchGroupIds = new List<string>();

        private bool _autoResolveBattles = true;
        private string _lastScheduleSkipReason;
        private int _lastFailedBattleDay = -1;
        private int _lastFailedBattleSlot = -1;

        public CampaignSettings Settings => _campaignSettings;
        public Campaign ActiveCampaign => _activeCampaign;
        public string LocalCommanderId => _localCommanderId;
        public ITroopUnitRoster OwnerRoster => _ownerRoster;
        public bool AutoResolveBattles
        {
            get => _autoResolveBattles;
            set => _autoResolveBattles = value;
        }

        public event Action<Campaign> CampaignChanged;
        public event Action<Campaign> CampaignOffered;
        public event Action<Campaign> CampaignAccepted;
        public event Action<Campaign> CampaignDeclined;
        public event Action<Campaign> CampaignActivated;
        public event Action<Campaign, CampaignBattleRecord> BattleResolved;
        public event Action<Campaign> CampaignCompleted;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            ResolveRefs();
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
            if (_travel != null)
                _travel.Arrived += HandleTravelArrived;
        }

        private void OnDisable()
        {
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
            if (_travel != null)
                _travel.Arrived -= HandleTravelArrived;
        }

        public void ResolveRefs()
        {
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            if (_currency == null)
                _currency = FindFirstObjectByType<CurrencySystem>();
            if (_navigation == null)
                _navigation = FindFirstObjectByType<NavigationController>();
            if (_pathfinder == null)
            {
                if (_navigation != null)
                    _pathfinder = _navigation.GetComponent<TgsNavigationPathfinder>();
                if (_pathfinder == null)
                    _pathfinder = FindFirstObjectByType<TgsNavigationPathfinder>();
            }
            if (_travel == null)
                _travel = FindFirstObjectByType<TroopTravelController>();
            if (_tgs == null)
                _tgs = FindFirstObjectByType<TerrainGridSystem>();
            if (_ownerRoster == null)
                _ownerRoster = FindFirstObjectByType<TemporaryAlphaTroopRoster>();
            if (_ownerRoster != null)
                _ownerRoster.SetCommanderId(_localCommanderId);
        }

        public void SetFistForceSelector(IFistForceSelector selector)
        {
            _fistForceSelector = selector ?? new RandomEqualFistForceSelector();
        }

        /// <summary>
        /// Creates a Campaign in Offered state only — does not Accept, March, or Activate.
        /// Safe for map tap → Offer popup flow.
        /// </summary>
        public Campaign OfferAtTerritory(
            int territoryIndex,
            Vector3? marchDestinationWorld = null,
            string ownerCommanderId = null)
        {
            return CreateCampaign(territoryIndex, ownerCommanderId, marchDestinationWorld);
        }

        /// <summary>
        /// Alias for OfferAtTerritory. Lifecycle = Offered until Owner Accept + first Owner InCamp.
        /// </summary>
        public Campaign CreateCampaign(
            int territoryIndex,
            string ownerCommanderId,
            Vector3? marchDestinationWorld = null)
        {
            ResolveRefs();
            if (_campaignSettings == null)
            {
                Debug.LogError($"{nameof(CampaignService)}: CampaignSettings missing.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(ownerCommanderId))
                ownerCommanderId = _localCommanderId;

            // Replace pending offer if player taps another territory before Accept.
            if (_activeCampaign != null &&
                _activeCampaign.LifecycleState == CampaignLifecycleState.Offered &&
                _activeCampaign.OwnerResponse == CampaignResponse.Pending)
            {
                // Discard previous pending offer.
            }
            else if (_activeCampaign != null &&
                     _activeCampaign.LifecycleState != CampaignLifecycleState.Completed &&
                     _activeCampaign.OwnerResponse == CampaignResponse.Accepted)
            {
                Debug.LogWarning("[Campaign] A Campaign is already in progress — cannot offer a new one.");
                return null;
            }

            // Alpha seed roster: free units again so the next Campaign can select/march.
            // Production roster would return units after campaign discharge instead.
            _ownerRoster?.ResetDeployedForTesting();

            _activeMarchGroupIds.Clear();
            _lastFailedBattleDay = -1;
            _lastFailedBattleSlot = -1;
            _lastScheduleSkipReason = null;

            // Reset travel so the next March is not stuck on a prior Arrived state.
            if (_travel != null &&
                (_travel.Status == TravelStatus.Arrived || _travel.Status == TravelStatus.Marching))
            {
                _travel.CancelTravel();
            }

            string id = "campaign-" + Guid.NewGuid().ToString("N").Substring(0, 16);
            _activeCampaign = Campaign.CreateOffered(
                id,
                territoryIndex,
                ownerCommanderId,
                _campaignSettings.CampaignDurationGameSeconds,
                marchDestinationWorld);

            NotifyChanged();
            CampaignOffered?.Invoke(_activeCampaign);
            Debug.Log(
                $"[Campaign] Offered {id} territory={territoryIndex} owner={ownerCommanderId} " +
                $"dest={(marchDestinationWorld.HasValue ? marchDestinationWorld.Value.ToString() : "centroid-fallback")} " +
                $"(not Active — waiting Accept → March → InCamp)");
            return _activeCampaign;
        }

        public bool OwnerAccept(string commanderId = null)
        {
            if (_activeCampaign == null ||
                _activeCampaign.LifecycleState != CampaignLifecycleState.Offered)
                return false;

            commanderId = ResolveCommanderId(commanderId);
            if (_activeCampaign.OwnerCommanderId != commanderId)
            {
                Debug.LogWarning(
                    $"[Campaign] OwnerAccept: {commanderId} is not Owner {_activeCampaign.OwnerCommanderId}.");
                return false;
            }

            _activeCampaign.SetOwnerResponse(CampaignResponse.Accepted);
            NotifyChanged();
            CampaignAccepted?.Invoke(_activeCampaign);
            return true;
        }

        public bool OwnerDecline(string commanderId = null)
        {
            if (_activeCampaign == null)
                return false;

            if (_activeCampaign.LifecycleState != CampaignLifecycleState.Offered)
                return false;

            // Allow Decline after Accept only if no troops have been contributed/marched yet.
            if (_activeCampaign.OwnerResponse == CampaignResponse.Accepted &&
                _activeCampaign.OwnerContributions.Count > 0)
            {
                Debug.LogWarning("[Campaign] Cannot Decline after troops were contributed.");
                return false;
            }

            commanderId = ResolveCommanderId(commanderId);
            if (_activeCampaign.OwnerCommanderId != commanderId)
                return false;

            _activeCampaign.SetOwnerResponse(CampaignResponse.Declined);
            Campaign declined = _activeCampaign;
            _activeCampaign = null;
            _activeMarchGroupIds.Clear();
            NotifyChanged();
            CampaignDeclined?.Invoke(declined);
            return true;
        }

        public bool SupportAccept(string commanderId)
        {
            if (_activeCampaign == null || string.IsNullOrWhiteSpace(commanderId))
                return false;

            if (commanderId == _activeCampaign.OwnerCommanderId)
                return false;

            if (_activeCampaign.LifecycleState == CampaignLifecycleState.Completed)
                return false;

            var support = _activeCampaign.GetOrAddSupport(commanderId);
            support.SetResponse(CampaignResponse.Accepted);
            NotifyChanged();
            return true;
        }

        public bool SupportDecline(string commanderId)
        {
            if (_activeCampaign == null || string.IsNullOrWhiteSpace(commanderId))
                return false;

            if (commanderId == _activeCampaign.OwnerCommanderId)
                return false;

            var support = _activeCampaign.GetOrAddSupport(commanderId);
            support.SetResponse(CampaignResponse.Declined);
            NotifyChanged();
            return true;
        }

        public TroopContribution AddOwnerContribution(TroopUnit unit, string commanderId = null)
        {
            if (_activeCampaign == null || unit == null)
                return null;

            commanderId = ResolveCommanderId(commanderId);
            if (_activeCampaign.OwnerCommanderId != commanderId)
                return null;

            if (_activeCampaign.OwnerResponse == CampaignResponse.Declined)
                return null;

            var contribution = new TroopContribution(NewContributionId(), commanderId, unit);
            _activeCampaign.AddOwnerContribution(contribution);
            _ownerRoster?.TryMarkDeployed(unit);
            NotifyChanged();
            return contribution;
        }

        public TroopContribution AddSupportContribution(string commanderId, TroopUnit unit)
        {
            if (_activeCampaign == null || unit == null || string.IsNullOrWhiteSpace(commanderId))
                return null;

            if (_activeCampaign.LifecycleState == CampaignLifecycleState.Completed)
                return null;

            if (commanderId == _activeCampaign.OwnerCommanderId)
                return AddOwnerContribution(unit, commanderId);

            var support = _activeCampaign.GetOrAddSupport(commanderId);
            if (support.Response == CampaignResponse.Declined)
                return null;

            if (support.Response == CampaignResponse.Pending)
                support.SetResponse(CampaignResponse.Accepted);

            var contribution = new TroopContribution(NewContributionId(), commanderId, unit);
            support.AddContribution(contribution);
            NotifyChanged();
            return contribution;
        }

        /// <summary>
        /// Contribute selected roster units and begin one shared March to Campaign territory.
        /// Units become InCamp only on Travel Arrived — not immediately Campaign force.
        /// </summary>
        public bool MarchSelectedOwnerUnits(IReadOnlyList<TroopUnit> selectedUnits)
        {
            ResolveRefs();
            if (_activeCampaign == null || _travel == null || selectedUnits == null || selectedUnits.Count == 0)
                return false;

            if (_activeCampaign.OwnerResponse != CampaignResponse.Accepted)
            {
                Debug.LogWarning("[Campaign] Owner must Accept before March.");
                return false;
            }

            if (!TryGetCampaignDestination(out Vector3 destination))
            {
                Debug.LogWarning("[Campaign] Cannot resolve territory destination.");
                return false;
            }

            _activeMarchGroupIds.Clear();
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                TroopUnit unit = selectedUnits[i];
                if (unit == null)
                    continue;

                TroopContribution contribution = AddOwnerContribution(unit, _activeCampaign.OwnerCommanderId);
                if (contribution == null)
                    continue;

                contribution.SetStatus(TroopContributionStatus.Marching);
                _activeMarchGroupIds.Add(contribution.ContributionId);
            }

            if (_activeMarchGroupIds.Count == 0)
                return false;

            if (!TryBeginPathedTravel(destination, out string travelError))
            {
                Debug.LogWarning($"[Campaign] March failed: {travelError}");
                // Roll contributions back to Selected so player can retry.
                RevertMarchGroupToSelected();
                return false;
            }

            NotifyChanged();
            Debug.Log(
                $"[Campaign] March group started: {_activeMarchGroupIds.Count} unit(s) → " +
                $"territory {_activeCampaign.TerritoryIndex} dest={destination}");
            return true;
        }

        /// <summary>
        /// Support/reinforcement: contribute units and join the same shared March/Arrive path.
        /// If already travelling, marks contributions Marching and they join camp on next Arrived
        /// or immediately if travel already Arrived.
        /// </summary>
        public bool MarchSelectedSupportUnits(string commanderId, IReadOnlyList<TroopUnit> selectedUnits)
        {
            ResolveRefs();
            if (_activeCampaign == null || selectedUnits == null || selectedUnits.Count == 0)
                return false;

            var newIds = new List<string>();
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                TroopUnit unit = selectedUnits[i];
                if (unit == null)
                    continue;

                TroopContribution contribution = AddSupportContribution(commanderId, unit);
                if (contribution == null)
                    continue;

                contribution.SetStatus(TroopContributionStatus.Marching);
                newIds.Add(contribution.ContributionId);
                _activeMarchGroupIds.Add(contribution.ContributionId);
            }

            if (newIds.Count == 0)
                return false;

            if (_travel != null && _travel.Status == TravelStatus.Arrived)
            {
                for (int i = 0; i < newIds.Count; i++)
                    RegisterArrival(newIds[i]);
                NotifyChanged();
                return true;
            }

            if (_travel != null &&
                (_travel.Status == TravelStatus.Idle || _travel.Status == TravelStatus.Camped))
            {
                if (!TryGetCampaignDestination(out Vector3 destination))
                    return false;

                if (!TryBeginPathedTravel(destination, out string travelError))
                {
                    Debug.LogWarning($"[Campaign] Support march failed: {travelError}");
                    return false;
                }
            }

            NotifyChanged();
            return true;
        }

        public bool RegisterArrival(string contributionId)
        {
            if (_activeCampaign == null)
                return false;

            if (!_activeCampaign.TryFindContribution(contributionId, out var contribution, out string commanderId))
                return false;

            contribution.SetStatus(TroopContributionStatus.InCamp);
            Debug.Log($"[Campaign] Contribution {contributionId} joined Campaign Camp.");

            TryActivateWhenOwnerInCamp(commanderId);
            NotifyChanged();
            return true;
        }

        public bool JoinCampImmediate(string contributionId) => RegisterArrival(contributionId);

        /// <summary>
        /// March existing Selected contributions as one shared travel group (debug / late path).
        /// Prefer <see cref="MarchSelectedOwnerUnits"/> for the main UX flow.
        /// </summary>
        public bool BeginMarchExistingSelectedContributions()
        {
            ResolveRefs();
            if (_activeCampaign == null || _travel == null)
                return false;

            if (!TryGetCampaignDestination(out Vector3 destination))
                return false;

            _activeMarchGroupIds.Clear();
            CollectSelectedAsMarching(_activeCampaign.OwnerContributions);

            for (int s = 0; s < _activeCampaign.Supports.Count; s++)
            {
                var support = _activeCampaign.Supports[s];
                if (support != null)
                    CollectSelectedAsMarching(support.Contributions);
            }

            if (_activeMarchGroupIds.Count == 0)
                return false;

            if (!TryBeginPathedTravel(destination, out string travelError))
            {
                Debug.LogWarning($"[Campaign] March failed: {travelError}");
                RevertMarchGroupToSelected();
                return false;
            }

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Reuses TgsNavigationPathfinder (same Lake-blocking rules as Plan March),
        /// then starts Travel on that route — no Plan March approval UI.
        /// Lake is impassable; straight-line BeginTravel(destination) is never used.
        /// </summary>
        private bool TryBeginPathedTravel(Vector3 destination, out string error)
        {
            error = null;
            ResolveRefs();

            if (_travel == null)
            {
                error = "TroopTravelController missing.";
                return false;
            }

            if (_pathfinder == null)
            {
                error = "TgsNavigationPathfinder missing — cannot route around Lake.";
                return false;
            }

            Vector3 origin = _travel.transform.position;
            NavigationRoute route = _pathfinder.FindRouteToWorld(origin, destination);
            if (route == null || !route.IsValid)
            {
                error = route != null
                    ? route.FailureReason
                    : "No route (null). Destination may be Lake/blocked or unreachable.";
                return false;
            }

            _travel.BeginTravel(route);
            return true;
        }

        private void RevertMarchGroupToSelected()
        {
            if (_activeCampaign == null)
                return;

            for (int i = 0; i < _activeMarchGroupIds.Count; i++)
            {
                if (_activeCampaign.TryFindContribution(
                        _activeMarchGroupIds[i],
                        out TroopContribution contribution,
                        out _))
                {
                    contribution.SetStatus(TroopContributionStatus.Selected);
                }
            }

            _activeMarchGroupIds.Clear();
        }

        private void CollectSelectedAsMarching(IReadOnlyList<TroopContribution> contributions)
        {
            if (contributions == null)
                return;

            for (int i = 0; i < contributions.Count; i++)
            {
                TroopContribution c = contributions[i];
                if (c == null || c.Status != TroopContributionStatus.Selected)
                    continue;

                c.SetStatus(TroopContributionStatus.Marching);
                _activeMarchGroupIds.Add(c.ContributionId);
            }
        }

        /// <summary>
        /// Preferred Alpha activation is automatic via first Owner InCamp.
        /// Kept for debug / test controls only.
        /// </summary>
        public bool StartCampaign()
        {
            return TryStartActiveInternal("manual StartCampaign");
        }

        public bool TryResolveScheduledBattle()
        {
            ResolveRefs();
            if (_activeCampaign == null || _campaignSettings == null || _battleSettings == null || _worldTime == null)
                return false;

            CampaignBattleScheduler.EnsureDayCounter(_activeCampaign, _worldTime);

            if (!CampaignBattleScheduler.ShouldResolveBattleNow(
                    _activeCampaign, _campaignSettings, _worldTime, out string skip))
            {
                if (!string.IsNullOrEmpty(skip) && skip != _lastScheduleSkipReason)
                    _lastScheduleSkipReason = skip;
                return false;
            }

            if (!CampaignBattleRunner.TryRunBattle(
                    _activeCampaign,
                    _campaignSettings,
                    _battleSettings,
                    _worldTime,
                    _fistForceSelector,
                    out CampaignBattleRecord record,
                    out string error))
            {
                int day = _worldTime.GetDayIndex();
                int slot = _activeCampaign.BattlesCompletedToday;
                if (!string.IsNullOrEmpty(error) &&
                    (day != _lastFailedBattleDay || slot != _lastFailedBattleSlot))
                {
                    _lastFailedBattleDay = day;
                    _lastFailedBattleSlot = slot;
                    Debug.LogWarning($"[Campaign] Battle skipped: {error}");
                }

                return false;
            }

            _lastFailedBattleDay = -1;
            _lastFailedBattleSlot = -1;
            BattleResolved?.Invoke(_activeCampaign, record);
            NotifyChanged();
            return true;
        }

        public bool DebugForceResolveBattle()
        {
            ResolveRefs();
            if (_activeCampaign == null || _activeCampaign.LifecycleState != CampaignLifecycleState.Active)
                return false;

            CampaignBattleScheduler.EnsureDayCounter(_activeCampaign, _worldTime);

            if (!CampaignBattleRunner.TryRunBattle(
                    _activeCampaign,
                    _campaignSettings,
                    _battleSettings,
                    _worldTime,
                    _fistForceSelector,
                    out CampaignBattleRecord record,
                    out string error))
            {
                Debug.LogWarning($"[Campaign] Force battle failed: {error}");
                return false;
            }

            BattleResolved?.Invoke(_activeCampaign, record);
            NotifyChanged();
            return true;
        }

        public void CompleteCampaign()
        {
            if (_activeCampaign == null)
                return;

            if (_activeCampaign.LifecycleState == CampaignLifecycleState.Completed)
                return;

            CampaignOutcome outcome = CampaignTerritoryControlTracker.EvaluateCampaignOutcome(_activeCampaign);
            _activeCampaign.SetOutcome(outcome);
            _activeCampaign.SetLifecycleState(CampaignLifecycleState.Completed);

            if (outcome == CampaignOutcome.Won)
                _activeCampaign.MarkRecordedOwnerTerritoryWin();

            Debug.Log(
                $"[Campaign] Completed {_activeCampaign.CampaignId} outcome={outcome} " +
                $"CmdControlHours={_activeCampaign.CommanderControlDurationGameSeconds / 3600d:F2} " +
                $"FistControlHours={_activeCampaign.FistControlDurationGameSeconds / 3600d:F2}");

            TryPayRewards();
            _activeMarchGroupIds.Clear();
            _lastFailedBattleDay = -1;
            _lastFailedBattleSlot = -1;

            // Allow seed units to be selected again on the next Campaign (Alpha only).
            _ownerRoster?.ResetDeployedForTesting();

            CampaignCompleted?.Invoke(_activeCampaign);
            NotifyChanged();
        }

        public bool TryPayRewards()
        {
            ResolveRefs();
            return CampaignRewardPayout.TryPay(
                _activeCampaign,
                _campaignSettings,
                _currency,
                _localCommanderId);
        }

        public ArmyComposition GetAggregatedInCampForce()
        {
            return CampaignForceAggregator.AggregateInCampCommanderForce(_activeCampaign);
        }

        public int CountOwnerInCampTroops()
        {
            if (_activeCampaign == null)
                return 0;

            int total = 0;
            for (int i = 0; i < _activeCampaign.OwnerContributions.Count; i++)
            {
                var c = _activeCampaign.OwnerContributions[i];
                if (c != null && c.Status == TroopContributionStatus.InCamp)
                    total += c.GetComposition().Total;
            }

            return total;
        }

        public int CountOwnerMarchingTroops()
        {
            if (_activeCampaign == null)
                return 0;

            int total = 0;
            for (int i = 0; i < _activeCampaign.OwnerContributions.Count; i++)
            {
                var c = _activeCampaign.OwnerContributions[i];
                if (c != null && c.Status == TroopContributionStatus.Marching)
                    total += c.GetComposition().Total;
            }

            return total;
        }

        /// <summary>Owner + Accepted Support troops currently InCamp (battle force headcount).</summary>
        public int CountCommanderSideInCampTroops()
        {
            return GetAggregatedInCampForce().Total;
        }

        /// <summary>Owner + Accepted Support troops currently Marching.</summary>
        public int CountCommanderSideMarchingTroops()
        {
            if (_activeCampaign == null)
                return 0;

            int total = CountOwnerMarchingTroops();
            for (int s = 0; s < _activeCampaign.Supports.Count; s++)
            {
                var support = _activeCampaign.Supports[s];
                if (support == null || support.Response != CampaignResponse.Accepted)
                    continue;

                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var c = support.Contributions[i];
                    if (c != null && c.Status == TroopContributionStatus.Marching)
                        total += c.GetComposition().Total;
                }
            }

            return total;
        }

        public int CountSupportInCampTroops()
        {
            if (_activeCampaign == null)
                return 0;

            int total = 0;
            for (int s = 0; s < _activeCampaign.Supports.Count; s++)
            {
                var support = _activeCampaign.Supports[s];
                if (support == null || support.Response != CampaignResponse.Accepted)
                    continue;

                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var c = support.Contributions[i];
                    if (c != null && c.Status == TroopContributionStatus.InCamp)
                        total += c.GetComposition().Total;
                }
            }

            return total;
        }

        public bool TryGetCampaignDestination(out Vector3 worldPos)
        {
            worldPos = default;
            ResolveRefs();
            if (_activeCampaign == null)
                return false;

            // Prefer the exact map-tap location stored at Offer time.
            // Territory centroid is often far from the clicked cell on large territories.
            if (_activeCampaign.HasMarchDestination)
            {
                worldPos = _activeCampaign.MarchDestinationWorld;
                return true;
            }

            if (_tgs == null)
                return false;

            int index = _activeCampaign.TerritoryIndex;
            if (index < 0 || _tgs.territories == null || index >= _tgs.territories.Count)
                return false;

            worldPos = _tgs.TerritoryGetPosition(index);
            return true;
        }

        private void TryActivateWhenOwnerInCamp(string arrivedCommanderId)
        {
            if (_activeCampaign == null)
                return;

            if (_activeCampaign.LifecycleState != CampaignLifecycleState.Offered)
                return;

            if (_activeCampaign.OwnerResponse != CampaignResponse.Accepted)
                return;

            if (arrivedCommanderId != _activeCampaign.OwnerCommanderId)
                return;

            if (!HasOwnerInCamp())
                return;

            TryStartActiveInternal("first Owner contribution InCamp");
        }

        private bool HasOwnerInCamp()
        {
            if (_activeCampaign == null)
                return false;

            for (int i = 0; i < _activeCampaign.OwnerContributions.Count; i++)
            {
                var c = _activeCampaign.OwnerContributions[i];
                if (c != null && c.Status == TroopContributionStatus.InCamp)
                    return true;
            }

            return false;
        }

        private bool TryStartActiveInternal(string reason)
        {
            ResolveRefs();
            if (_activeCampaign == null || _campaignSettings == null || _worldTime == null)
                return false;

            if (_activeCampaign.LifecycleState == CampaignLifecycleState.Active ||
                _activeCampaign.LifecycleState == CampaignLifecycleState.Completed)
                return false;

            if (_activeCampaign.OwnerResponse != CampaignResponse.Accepted)
                return false;

            _activeCampaign.MarkStarted(_worldTime.TotalGameSeconds);
            _activeCampaign.ResetDailyBattleCount(_worldTime.GetDayIndex());
            NotifyChanged();
            CampaignActivated?.Invoke(_activeCampaign);
            Debug.Log(
                $"[Campaign] Active {_activeCampaign.CampaignId} ({reason}) at t={_worldTime.TotalGameSeconds:F0}");
            return true;
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double totalGameSeconds)
        {
            if (_activeCampaign == null || _activeCampaign.LifecycleState != CampaignLifecycleState.Active)
                return;

            CampaignTerritoryControlTracker.AccumulateControlTime(_activeCampaign, deltaGameSeconds);

            if (CampaignBattleScheduler.IsDurationElapsed(_activeCampaign, _worldTime))
            {
                CompleteCampaign();
                return;
            }

            if (_autoResolveBattles)
                TryResolveScheduledBattle();
        }

        private void HandleTravelArrived()
        {
            if (_activeCampaign == null)
                return;

            // One shared March Group: all currently Marching contributions join Camp together.
            RegisterAllMarchingArrivals();
            _activeMarchGroupIds.Clear();
        }

        private void RegisterAllMarchingArrivals()
        {
            if (_activeCampaign == null)
                return;

            for (int i = 0; i < _activeCampaign.OwnerContributions.Count; i++)
            {
                var c = _activeCampaign.OwnerContributions[i];
                if (c != null && c.Status == TroopContributionStatus.Marching)
                    RegisterArrival(c.ContributionId);
            }

            for (int s = 0; s < _activeCampaign.Supports.Count; s++)
            {
                var support = _activeCampaign.Supports[s];
                if (support == null)
                    continue;
                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var c = support.Contributions[i];
                    if (c != null && c.Status == TroopContributionStatus.Marching)
                        RegisterArrival(c.ContributionId);
                }
            }
        }

        private string ResolveCommanderId(string commanderId)
        {
            return string.IsNullOrWhiteSpace(commanderId) ? _localCommanderId : commanderId;
        }

        private static string NewContributionId()
        {
            return "contrib-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        private void NotifyChanged()
        {
            CampaignChanged?.Invoke(_activeCampaign);
        }
    }
}
