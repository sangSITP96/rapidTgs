using Game.Battle;

namespace Game.Campaign
{
    public static class CampaignForceAggregator
    {
        /// <summary>
        /// Aggregates InCamp contributions for the Commander side without discarding ownership links.
        /// Ownership remains on each TroopContribution; this only builds a battle force snapshot.
        /// </summary>
        public static ArmyComposition AggregateInCampCommanderForce(Campaign campaign)
        {
            var total = new ArmyComposition(0, 0, 0);
            if (campaign == null)
                return total;

            for (int i = 0; i < campaign.OwnerContributions.Count; i++)
            {
                var c = campaign.OwnerContributions[i];
                if (c == null || c.Status != TroopContributionStatus.InCamp)
                    continue;
                total += c.GetComposition();
            }

            for (int s = 0; s < campaign.Supports.Count; s++)
            {
                var support = campaign.Supports[s];
                if (support == null || support.Response != CampaignResponse.Accepted)
                    continue;

                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var c = support.Contributions[i];
                    if (c == null || c.Status != TroopContributionStatus.InCamp)
                        continue;
                    total += c.GetComposition();
                }
            }

            return total;
        }

        public static int CountInCampContributions(Campaign campaign)
        {
            int count = 0;
            if (campaign == null)
                return 0;

            for (int i = 0; i < campaign.OwnerContributions.Count; i++)
            {
                if (campaign.OwnerContributions[i] != null &&
                    campaign.OwnerContributions[i].Status == TroopContributionStatus.InCamp)
                    count++;
            }

            for (int s = 0; s < campaign.Supports.Count; s++)
            {
                var support = campaign.Supports[s];
                if (support == null)
                    continue;
                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    if (support.Contributions[i] != null &&
                        support.Contributions[i].Status == TroopContributionStatus.InCamp)
                        count++;
                }
            }

            return count;
        }
    }
}
