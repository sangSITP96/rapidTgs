namespace Game.Campaign
{
    /// <summary>
    /// Individual Campaign Battle outcome (Winning Percentage rule).
    /// Draw is distinct from final CampaignOutcome.Draw.
    /// </summary>
    public enum CampaignBattleSideResult
    {
        None = 0,
        CommanderWin = 1,
        FistWin = 2,
        Draw = 3
    }
}
