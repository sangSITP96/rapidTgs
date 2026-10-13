namespace Game.Campaign
{
    /// <summary>
    /// Pure reward math from confirmed client formula.
    /// Does not invent Supporting Commander weighting.
    /// </summary>
    public static class CampaignRewardCalculator
    {
        public static int CountBattleWins(Campaign campaign)
        {
            int wins = 0;
            if (campaign == null)
                return 0;

            for (int i = 0; i < campaign.BattleHistory.Count; i++)
            {
                var record = campaign.BattleHistory[i];
                if (record != null && record.SideResult == CampaignBattleSideResult.CommanderWin)
                    wins++;
            }

            return wins;
        }

        public static int CountBattleLosses(Campaign campaign)
        {
            int losses = 0;
            if (campaign == null)
                return 0;

            for (int i = 0; i < campaign.BattleHistory.Count; i++)
            {
                var record = campaign.BattleHistory[i];
                if (record != null && record.SideResult == CampaignBattleSideResult.FistWin)
                    losses++;
            }

            return losses;
        }

        public static int CountBattleDraws(Campaign campaign)
        {
            int draws = 0;
            if (campaign == null)
                return 0;

            for (int i = 0; i < campaign.BattleHistory.Count; i++)
            {
                var record = campaign.BattleHistory[i];
                if (record != null && record.SideResult == CampaignBattleSideResult.Draw)
                    draws++;
            }

            return draws;
        }

        /// <summary>
        /// Base + (wins * perWin) + (losses * perLoss). Battle Draw = 0 adjustment.
        /// </summary>
        public static int CalculateSilverAmount(Campaign campaign, CampaignSettings settings)
        {
            if (campaign == null || settings == null)
                return 0;

            int wins = CountBattleWins(campaign);
            int losses = CountBattleLosses(campaign);
            // draws intentionally unused — 0 adjustment
            return settings.BaseSilverPayment
                   + wins * settings.SilverPerBattleWin
                   + losses * settings.SilverPerBattleLoss;
        }
    }
}
