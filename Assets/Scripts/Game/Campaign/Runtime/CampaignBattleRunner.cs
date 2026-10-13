using Game.Battle;
using Game.Core.WorldTime;
using UnityEngine;

namespace Game.Campaign
{
    public static class CampaignBattleRunner
    {
        public const bool CommanderIsAttacker = true;

        public static bool TryRunBattle(
            Campaign campaign,
            CampaignSettings campaignSettings,
            BattleSettings battleSettings,
            WorldTime worldTime,
            IFistForceSelector fistSelector,
            out CampaignBattleRecord record,
            out string error)
        {
            record = null;
            error = null;

            if (campaign == null || campaignSettings == null || battleSettings == null || worldTime == null)
            {
                error = "Missing required references.";
                return false;
            }

            ArmyComposition commanderForce = CampaignForceAggregator.AggregateInCampCommanderForce(campaign);
            if (commanderForce.Total <= 0)
            {
                error = "No InCamp Commander-side troops to fight.";
                return false;
            }

            if (fistSelector == null)
                fistSelector = new RandomEqualFistForceSelector();

            // Alpha default: equal totals when EqualArmySizeForAlpha.
            // If false, F.I.S.T. sizing rule is not yet defined by client — do not invent; keep equal for now.
            int fistTotal = commanderForce.Total;

            int seed = unchecked(
                (int)worldTime.TotalGameSeconds ^
                (campaign.NextBattleIndex * 397) ^
                commanderForce.Total);

            // Snapshot troops sent BEFORE resolve (also available on BattleResult.Starting after).
            ArmyComposition fistForce = fistSelector.CreateForce(fistTotal, seed ^ 0x5F3759DF);

            if (campaignSettings.RequireLargeBattleMode)
            {
                int threshold = battleSettings.SmallBattleThreshold;
                if (commanderForce.Total < threshold || fistForce.Total < threshold)
                {
                    error =
                        $"Campaign Alpha requires Large Battle (both sides >= {threshold}). " +
                        $"Commander={commanderForce.Total}, F.I.S.T.={fistForce.Total}. " +
                        "Small Battle has no Surviving troops for WinningPercentage — limitation deferred.";
                    return false;
                }
            }

            var input = new BattleInput(commanderForce, fistForce);
            var context = new BattleContext(applyDefenderAdvantage: true, seed: seed);
            BattleResult result = BattleResolver.Resolve(input, context, battleSettings);

            if (!CampaignBattleOutcomeEvaluator.TryEvaluateFromLargeResult(
                    result,
                    CommanderIsAttacker,
                    out float cmdWp,
                    out float fistWp,
                    out CampaignBattleSideResult sideResult,
                    out error))
            {
                return false;
            }

            record = CampaignBattleRecord.CreateResolved(
                campaign.NextBattleIndex,
                worldTime.GetDayIndex(),
                worldTime.TotalGameSeconds,
                result,
                CommanderIsAttacker,
                cmdWp,
                fistWp,
                sideResult);

            campaign.AddBattleRecord(record);
            campaign.IncrementBattlesCompletedToday();
            CampaignTerritoryControlTracker.ApplyBattleResult(campaign, sideResult);

            Debug.Log(
                $"[Campaign] Battle #{record.BattleIndex} {sideResult} " +
                $"CmdWP={cmdWp:P1} ({record.CommanderSurviving.Total}/{record.CommanderTroopsSent.Total}) " +
                $"FistWP={fistWp:P1} ({record.FistSurviving.Total}/{record.FistTroopsSent.Total}) " +
                $"Controller={campaign.CurrentController}");

            return true;
        }
    }
}
