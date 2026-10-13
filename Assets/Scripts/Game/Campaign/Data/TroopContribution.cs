using System;
using Game.Battle;
using Game.Troops;
using UnityEngine;

namespace Game.Campaign
{
    [Serializable]
    public sealed class TroopContribution
    {
        [SerializeField] private string contributionId;
        [SerializeField] private string commanderId;
        [SerializeField] private TroopUnit unit;
        [SerializeField] private TroopContributionStatus status = TroopContributionStatus.Selected;

        public string ContributionId => contributionId;
        public string CommanderId => commanderId;
        public TroopUnit Unit => unit;
        public TroopContributionStatus Status => status;

        public TroopContribution(string contributionId, string commanderId, TroopUnit unit)
        {
            this.contributionId = contributionId;
            this.commanderId = commanderId;
            this.unit = unit;
            status = TroopContributionStatus.Selected;
        }

        public void SetStatus(TroopContributionStatus next) => status = next;

        public ArmyComposition GetComposition()
        {
            return unit != null ? unit.GetAggregateComposition() : default;
        }
    }
}
