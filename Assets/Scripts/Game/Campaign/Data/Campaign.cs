using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Campaign
{
    [Serializable]
    public sealed class Campaign
    {
        [SerializeField] private string campaignId;
        [SerializeField] private int territoryIndex = -1;
        [SerializeField] private Vector3 marchDestinationWorld;
        [SerializeField] private bool hasMarchDestination;
        [SerializeField] private string ownerCommanderId;
        [SerializeField] private CampaignObjectiveType objective = CampaignObjectiveType.CaptureTerritory;

        [SerializeField] private CampaignLifecycleState lifecycleState = CampaignLifecycleState.None;
        [SerializeField] private CampaignOutcome outcome = CampaignOutcome.None;
        [SerializeField] private CampaignResponse ownerResponse = CampaignResponse.Pending;

        [SerializeField] private List<TroopContribution> ownerContributions = new List<TroopContribution>();
        [SerializeField] private List<CampaignSupport> supports = new List<CampaignSupport>();
        [SerializeField] private List<CampaignBattleRecord> battleHistory = new List<CampaignBattleRecord>();

        [SerializeField] private CampaignControllerSide currentController = CampaignControllerSide.Fist;
        [SerializeField] private double commanderControlDurationGameSeconds;
        [SerializeField] private double fistControlDurationGameSeconds;

        [SerializeField] private double startedAtTotalGameSeconds = -1d;
        [SerializeField] private double durationGameSeconds;
        [SerializeField] private int battlesCompletedToday;
        [SerializeField] private int scheduleDayIndex = -1;
        [SerializeField] private int nextBattleIndex;
        [SerializeField] private bool rewardPaid;

        /// <summary>
        /// Soft record only — no TGS ownership system. Set when Outcome == Won.
        /// </summary>
        [SerializeField] private bool recordedOwnerTerritoryWin;

        public string CampaignId => campaignId;
        public int TerritoryIndex => territoryIndex;

        /// <summary>
        /// Exact map tap / offer location used for March. Prefer this over territory centroid.
        /// </summary>
        public Vector3 MarchDestinationWorld => marchDestinationWorld;
        public bool HasMarchDestination => hasMarchDestination;

        public string OwnerCommanderId => ownerCommanderId;
        public CampaignObjectiveType Objective => objective;

        public CampaignLifecycleState LifecycleState => lifecycleState;
        public CampaignOutcome Outcome => outcome;
        public CampaignResponse OwnerResponse => ownerResponse;

        public IReadOnlyList<TroopContribution> OwnerContributions => ownerContributions;
        public IReadOnlyList<CampaignSupport> Supports => supports;
        public IReadOnlyList<CampaignBattleRecord> BattleHistory => battleHistory;

        public CampaignControllerSide CurrentController => currentController;
        public double CommanderControlDurationGameSeconds => commanderControlDurationGameSeconds;
        public double FistControlDurationGameSeconds => fistControlDurationGameSeconds;

        public double StartedAtTotalGameSeconds => startedAtTotalGameSeconds;
        public double DurationGameSeconds => durationGameSeconds;
        public int BattlesCompletedToday => battlesCompletedToday;
        public int ScheduleDayIndex => scheduleDayIndex;
        public int NextBattleIndex => nextBattleIndex;
        public bool RewardPaid => rewardPaid;
        public bool RecordedOwnerTerritoryWin => recordedOwnerTerritoryWin;

        public static Campaign CreateOffered(
            string campaignId,
            int territoryIndex,
            string ownerCommanderId,
            double durationGameSeconds,
            Vector3? marchDestinationWorld = null)
        {
            bool hasDestination = marchDestinationWorld.HasValue;
            return new Campaign
            {
                campaignId = campaignId,
                territoryIndex = territoryIndex,
                marchDestinationWorld = hasDestination ? marchDestinationWorld.Value : Vector3.zero,
                hasMarchDestination = hasDestination,
                ownerCommanderId = ownerCommanderId,
                objective = CampaignObjectiveType.CaptureTerritory,
                lifecycleState = CampaignLifecycleState.Offered,
                outcome = CampaignOutcome.None,
                ownerResponse = CampaignResponse.Pending,
                ownerContributions = new List<TroopContribution>(),
                supports = new List<CampaignSupport>(),
                battleHistory = new List<CampaignBattleRecord>(),
                currentController = CampaignControllerSide.Fist,
                durationGameSeconds = durationGameSeconds
            };
        }

        public void SetMarchDestination(Vector3 worldPos)
        {
            marchDestinationWorld = worldPos;
            hasMarchDestination = true;
        }

        public void SetOwnerResponse(CampaignResponse response) => ownerResponse = response;

        public void SetLifecycleState(CampaignLifecycleState state) => lifecycleState = state;

        public void SetOutcome(CampaignOutcome next) => outcome = next;

        public void MarkStarted(double totalGameSeconds)
        {
            startedAtTotalGameSeconds = totalGameSeconds;
            lifecycleState = CampaignLifecycleState.Active;
            currentController = CampaignControllerSide.Fist;
            commanderControlDurationGameSeconds = 0d;
            fistControlDurationGameSeconds = 0d;
        }

        public void AddOwnerContribution(TroopContribution contribution)
        {
            if (contribution == null)
                return;
            ownerContributions.Add(contribution);
        }

        public CampaignSupport GetOrAddSupport(string commanderId)
        {
            for (int i = 0; i < supports.Count; i++)
            {
                if (supports[i] != null && supports[i].CommanderId == commanderId)
                    return supports[i];
            }

            var support = new CampaignSupport(commanderId);
            supports.Add(support);
            return support;
        }

        public bool TryGetSupport(string commanderId, out CampaignSupport support)
        {
            support = null;
            for (int i = 0; i < supports.Count; i++)
            {
                if (supports[i] != null && supports[i].CommanderId == commanderId)
                {
                    support = supports[i];
                    return true;
                }
            }

            return false;
        }

        public void AddBattleRecord(CampaignBattleRecord record)
        {
            if (record == null)
                return;
            battleHistory.Add(record);
            nextBattleIndex++;
        }

        public void SetCurrentController(CampaignControllerSide side) => currentController = side;

        public void AddControlDuration(CampaignControllerSide side, double deltaGameSeconds)
        {
            if (deltaGameSeconds <= 0d)
                return;

            if (side == CampaignControllerSide.Commander)
                commanderControlDurationGameSeconds += deltaGameSeconds;
            else
                fistControlDurationGameSeconds += deltaGameSeconds;
        }

        public void ResetDailyBattleCount(int dayIndex)
        {
            scheduleDayIndex = dayIndex;
            battlesCompletedToday = 0;
        }

        public void IncrementBattlesCompletedToday() => battlesCompletedToday++;

        public void MarkRewardPaid() => rewardPaid = true;

        public void MarkRecordedOwnerTerritoryWin() => recordedOwnerTerritoryWin = true;

        public bool TryFindContribution(string contributionId, out TroopContribution contribution, out string commanderId)
        {
            contribution = null;
            commanderId = null;

            for (int i = 0; i < ownerContributions.Count; i++)
            {
                var c = ownerContributions[i];
                if (c != null && c.ContributionId == contributionId)
                {
                    contribution = c;
                    commanderId = ownerCommanderId;
                    return true;
                }
            }

            for (int s = 0; s < supports.Count; s++)
            {
                var support = supports[s];
                if (support == null)
                    continue;
                if (support.TryGetContribution(contributionId, out contribution))
                {
                    commanderId = support.CommanderId;
                    return true;
                }
            }

            return false;
        }
    }
}
