using Game.Battle;

namespace Game.Campaign
{
    /// <summary>
    /// Campaign-layer battle outcome using client rule:
    /// WinningPercentage = SurvivingTroops / TroopsSent (per side).
    /// Does not modify BattleResolver.
    /// </summary>
    public static class CampaignBattleOutcomeEvaluator
    {
        public static bool TryEvaluateFromLargeResult(
            BattleResult result,
            bool commanderIsAttacker,
            out float commanderWinningPercentage,
            out float fistWinningPercentage,
            out CampaignBattleSideResult sideResult,
            out string error)
        {
            commanderWinningPercentage = 0f;
            fistWinningPercentage = 0f;
            sideResult = CampaignBattleSideResult.None;
            error = null;

            if (result == null)
            {
                error = "BattleResult is null.";
                return false;
            }

            if (result.Mode != BattleResolveMode.LargeCasualties)
            {
                error =
                    "Small Battle mode does not return surviving troop counts required for " +
                    "WinningPercentage = Surviving/Sent. Campaign Alpha requires Large Battle. " +
                    "Limitation deferred — do not change Battle System for this.";
                return false;
            }

            ArmyComposition cmdSent = commanderIsAttacker ? result.AttackerStarting : result.DefenderStarting;
            ArmyComposition fistSent = commanderIsAttacker ? result.DefenderStarting : result.AttackerStarting;
            ArmyComposition cmdSurv = commanderIsAttacker ? result.AttackerRemaining : result.DefenderRemaining;
            ArmyComposition fistSurv = commanderIsAttacker ? result.DefenderRemaining : result.AttackerRemaining;

            if (cmdSent.Total <= 0 || fistSent.Total <= 0)
            {
                error = "TroopsSent must be > 0 for both sides.";
                return false;
            }

            commanderWinningPercentage = cmdSurv.Total / (float)cmdSent.Total;
            fistWinningPercentage = fistSurv.Total / (float)fistSent.Total;

            const float epsilon = 1e-6f;
            if (commanderWinningPercentage > fistWinningPercentage + epsilon)
                sideResult = CampaignBattleSideResult.CommanderWin;
            else if (fistWinningPercentage > commanderWinningPercentage + epsilon)
                sideResult = CampaignBattleSideResult.FistWin;
            else
                sideResult = CampaignBattleSideResult.Draw;

            return true;
        }

        public static float SafeRatio(int surviving, int sent)
        {
            if (sent <= 0)
                return 0f;
            return surviving / (float)sent;
        }
    }
}
