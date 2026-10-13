using Game.Currency;
using UnityEngine;

namespace Game.Campaign
{
    public static class CampaignRewardPayout
    {
        /// <summary>
        /// Pays Silver via existing Currency API.
        /// TEMPORARY Alpha: when settings.TemporaryAlphaRewardOwnerOnly, only Owner is paid.
        /// Supporting Commander Silver is NOT a confirmed client rule — contribution data is retained, no invent weighting.
        /// </summary>
        public static bool TryPay(
            Campaign campaign,
            CampaignSettings settings,
            CurrencySystem currency,
            string localCommanderIdForCurrency)
        {
            if (campaign == null || settings == null)
                return false;

            if (campaign.RewardPaid)
                return false;

            if (campaign.LifecycleState != CampaignLifecycleState.Completed)
                return false;

            int amount = CampaignRewardCalculator.CalculateSilverAmount(campaign, settings);
            if (amount <= 0)
            {
                campaign.MarkRewardPaid();
                Debug.Log(
                    $"[Campaign] Reward amount {amount} for {campaign.CampaignId} — nothing to award " +
                    $"(wins={CampaignRewardCalculator.CountBattleWins(campaign)}, " +
                    $"losses={CampaignRewardCalculator.CountBattleLosses(campaign)}).");
                return true;
            }

            if (settings.TemporaryAlphaRewardOwnerOnly)
            {
                // TEMPORARY ASSUMPTION — not client-confirmed permanent rule.
                if (currency == null)
                {
                    Debug.LogWarning("[Campaign] CurrencySystem missing; cannot pay Owner Silver.");
                    return false;
                }

                if (!string.IsNullOrEmpty(localCommanderIdForCurrency) &&
                    campaign.OwnerCommanderId != localCommanderIdForCurrency)
                {
                    Debug.Log(
                        $"[Campaign] TEMPORARY Alpha Owner-only payout skipped: Owner={campaign.OwnerCommanderId} " +
                        $"is not local currency owner={localCommanderIdForCurrency}. " +
                        $"Computed Silver={amount}. Support rewards unresolved.");
                    campaign.MarkRewardPaid();
                    return true;
                }

                string reason =
                    $"Campaign:{campaign.CampaignId} OwnerReward " +
                    $"(TEMPORARY Alpha Owner-only; Support Silver unresolved)";
                currency.AwardSilver(amount, reason);
                campaign.MarkRewardPaid();
                Debug.Log($"[Campaign] Awarded {amount} Silver to Owner {campaign.OwnerCommanderId}. {reason}");
                return true;
            }

            // Supporting / multi-commander payout rules are unresolved — do not invent.
            Debug.LogWarning(
                "[Campaign] TemporaryAlphaRewardOwnerOnly is false, but multi-commander Silver rules " +
                "are not defined by client. No payout performed. Contribution data retained.");
            return false;
        }
    }
}
