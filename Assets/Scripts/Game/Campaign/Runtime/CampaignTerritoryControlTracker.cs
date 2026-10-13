namespace Game.Campaign
{
    /// <summary>
    /// Temporary Alpha rule: battle winner flips CurrentController; Draw leaves it unchanged.
    /// Kept in Campaign layer so it can be replaced later without touching BattleResolver.
    /// </summary>
    public static class CampaignTerritoryControlTracker
    {
        public static void ApplyBattleResult(Campaign campaign, CampaignBattleSideResult battleResult)
        {
            if (campaign == null || campaign.LifecycleState != CampaignLifecycleState.Active)
                return;

            switch (battleResult)
            {
                case CampaignBattleSideResult.CommanderWin:
                    campaign.SetCurrentController(CampaignControllerSide.Commander);
                    break;
                case CampaignBattleSideResult.FistWin:
                    campaign.SetCurrentController(CampaignControllerSide.Fist);
                    break;
                case CampaignBattleSideResult.Draw:
                    // Individual Battle Draw: CurrentController does not change.
                    break;
            }
        }

        public static void AccumulateControlTime(Campaign campaign, double deltaGameSeconds)
        {
            if (campaign == null || campaign.LifecycleState != CampaignLifecycleState.Active)
                return;
            if (deltaGameSeconds <= 0d)
                return;

            campaign.AddControlDuration(campaign.CurrentController, deltaGameSeconds);
        }

        /// <summary>
        /// Final Campaign Winner by Territory Control Duration.
        /// Equal durations → CampaignOutcome.Draw (Unresolved). No tie-breaker.
        /// </summary>
        public static CampaignOutcome EvaluateCampaignOutcome(Campaign campaign)
        {
            if (campaign == null)
                return CampaignOutcome.None;

            double cmd = campaign.CommanderControlDurationGameSeconds;
            double fist = campaign.FistControlDurationGameSeconds;

            if (cmd > fist)
                return CampaignOutcome.Won;
            if (fist > cmd)
                return CampaignOutcome.Lost;
            return CampaignOutcome.Draw;
        }
    }
}
