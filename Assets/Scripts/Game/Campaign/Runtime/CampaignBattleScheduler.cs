using Game.Core.WorldTime;
using UnityEngine;

namespace Game.Campaign
{
    /// <summary>
    /// Schedules BattlesPerDay slots inside the seasonal daylight window from WorldTime.
    /// Does not use a Campaign-specific clock.
    /// </summary>
    public static class CampaignBattleScheduler
    {
        public static bool ShouldResolveBattleNow(
            Campaign campaign,
            CampaignSettings settings,
            WorldTime worldTime,
            out string skipReason)
        {
            skipReason = null;

            if (campaign == null || settings == null || worldTime == null)
            {
                skipReason = "Missing campaign/settings/worldTime.";
                return false;
            }

            if (campaign.LifecycleState != CampaignLifecycleState.Active)
            {
                skipReason = "Campaign not Active.";
                return false;
            }

            if (IsDurationElapsed(campaign, worldTime))
            {
                skipReason = "Campaign duration elapsed.";
                return false;
            }

            if (worldTime.IsNight())
            {
                skipReason = "Night cycle — no Campaign Battles.";
                return false;
            }

            // Prefer pure Day window between sunrise end and sunset start.
            worldTime.GetDaylightWindows(
                out _,
                out double sunriseEnd,
                out double sunsetStart,
                out _);

            double tod = worldTime.GetSecondsIntoDay();
            if (tod < sunriseEnd || tod >= sunsetStart)
            {
                skipReason = "Outside Day combat window (sunriseEnd..sunsetStart).";
                return false;
            }

            int dayIndex = worldTime.GetDayIndex();
            if (campaign.ScheduleDayIndex != dayIndex)
            {
                // Day rolled — caller should reset counter before checking slots.
            }

            if (campaign.ScheduleDayIndex == dayIndex &&
                campaign.BattlesCompletedToday >= settings.BattlesPerDay)
            {
                skipReason = "BattlesPerDay reached for this game day.";
                return false;
            }

            double dayCombatStart = sunriseEnd;
            double dayCombatEnd = sunsetStart;
            double usable = dayCombatEnd - dayCombatStart;
            if (usable <= 0d)
            {
                skipReason = "No usable Day combat window.";
                return false;
            }

            int battlesPerDay = Mathf.Max(1, settings.BattlesPerDay);
            double slotSpan = usable / battlesPerDay;
            int completed = campaign.ScheduleDayIndex == dayIndex ? campaign.BattlesCompletedToday : 0;
            double slotStart = dayCombatStart + completed * slotSpan;
            double slotEnd = slotStart + slotSpan;

            // Fire once when current time is inside the next incomplete slot.
            if (tod < slotStart)
            {
                skipReason = $"Waiting for battle slot {completed} start.";
                return false;
            }

            if (tod >= slotEnd && completed < battlesPerDay)
            {
                // Late in slot or missed — still allow resolve so Alpha doesn't stall if time jumped.
                return true;
            }

            return tod >= slotStart && completed < battlesPerDay;
        }

        public static bool IsDurationElapsed(Campaign campaign, WorldTime worldTime)
        {
            if (campaign == null || worldTime == null)
                return true;
            if (campaign.StartedAtTotalGameSeconds < 0d)
                return false;
            return worldTime.TotalGameSeconds >=
                   campaign.StartedAtTotalGameSeconds + campaign.DurationGameSeconds;
        }

        public static void EnsureDayCounter(Campaign campaign, WorldTime worldTime)
        {
            if (campaign == null || worldTime == null)
                return;

            int dayIndex = worldTime.GetDayIndex();
            if (campaign.ScheduleDayIndex != dayIndex)
                campaign.ResetDailyBattleCount(dayIndex);
        }
    }
}
