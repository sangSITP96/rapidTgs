using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Campaign
{
    /// <summary>
    /// Supporting Commander on a Campaign owned by someone else.
    /// Contribution/participation data is retained for future reward rules.
    /// </summary>
    [Serializable]
    public sealed class CampaignSupport
    {
        [SerializeField] private string commanderId;
        [SerializeField] private CampaignResponse response = CampaignResponse.Pending;
        [SerializeField] private List<TroopContribution> contributions = new List<TroopContribution>();

        public string CommanderId => commanderId;
        public CampaignResponse Response => response;
        public IReadOnlyList<TroopContribution> Contributions => contributions;

        public CampaignSupport(string commanderId)
        {
            this.commanderId = commanderId;
            response = CampaignResponse.Pending;
            contributions = new List<TroopContribution>();
        }

        public void SetResponse(CampaignResponse next) => response = next;

        public void AddContribution(TroopContribution contribution)
        {
            if (contribution == null)
                return;
            contributions.Add(contribution);
        }

        public bool TryGetContribution(string contributionId, out TroopContribution contribution)
        {
            contribution = null;
            if (string.IsNullOrEmpty(contributionId) || contributions == null)
                return false;

            for (int i = 0; i < contributions.Count; i++)
            {
                if (contributions[i] != null && contributions[i].ContributionId == contributionId)
                {
                    contribution = contributions[i];
                    return true;
                }
            }

            return false;
        }
    }
}
