namespace Game.Campaign
{
    /// <summary>
    /// Final Campaign result. Draw means Unresolved — no client tie-breaker.
    /// </summary>
    public enum CampaignOutcome
    {
        None = 0,
        Won = 1,
        Lost = 2,
        Draw = 3
    }
}
