using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Seasons
{
    /// <summary>
    /// Pre-generates a season weather timeline from quota/range rules.
    /// </summary>
    public sealed class SeasonWeatherScheduler
    {
        private const double SecondsPerHour = 3600d;

        public List<SeasonWeatherScheduleEntry> BuildSchedule(
            SeasonDefinition definition,
            SeasonCalendarConfig calendar,
            double seasonStartGameSeconds,
            double seasonEndGameSeconds,
            int absoluteSeasonIndex)
        {
            var entries = new List<SeasonWeatherScheduleEntry>();
            if (definition == null)
                return entries;

            int seed = (calendar != null ? calendar.ScheduleSeed : 14)
                       ^ (absoluteSeasonIndex * 397)
                       ^ ((int)definition.Season * 31);
            var rng = new System.Random(seed);

            double seasonLength = Math.Max(1d, seasonEndGameSeconds - seasonStartGameSeconds);
            var occupied = new List<(double start, double end)>();

            if (definition.WeatherRules != null)
            {
                // Schedule severe / discrete events first.
                for (int i = 0; i < definition.WeatherRules.Count; i++)
                {
                    SeasonWeatherEventRule rule = definition.WeatherRules[i];
                    if (rule == null || !rule.Allowed || !rule.ScheduleAsEvents)
                        continue;
                    if (rule.Kind == SeasonWeatherKind.MightyStorm && !definition.MightyStormEnabled)
                        continue;

                    PlaceEventsForRule(
                        definition,
                        calendar,
                        rule,
                        seasonStartGameSeconds,
                        seasonEndGameSeconds,
                        seasonLength,
                        rng,
                        entries,
                        occupied);
                }
            }

            FillAmbient(definition, seasonStartGameSeconds, seasonEndGameSeconds, rng, entries, occupied);
            entries.Sort((a, b) => a.StartGameSeconds.CompareTo(b.StartGameSeconds));
            return entries;
        }

        private static void PlaceEventsForRule(
            SeasonDefinition definition,
            SeasonCalendarConfig calendar,
            SeasonWeatherEventRule rule,
            double seasonStart,
            double seasonEnd,
            double seasonLength,
            System.Random rng,
            List<SeasonWeatherScheduleEntry> entries,
            List<(double start, double end)> occupied)
        {
            int count = RandomInclusive(rng, rule.MinCountPerSeason, rule.MaxCountPerSeason);
            if (count <= 0)
                return;

            float preferMin = Mathf.Clamp01(rule.PreferProgressMin);
            float preferMax = Mathf.Clamp01(rule.PreferProgressMax);
            if (preferMax < preferMin)
                preferMax = preferMin;

            // Winter snow: bias half toward season start, half toward end.
            bool splitEnds = rule.Kind == SeasonWeatherKind.Snow && definition.Season == SeasonId.Winter;

            for (int i = 0; i < count; i++)
            {
                float localPreferMin = preferMin;
                float localPreferMax = preferMax;
                if (splitEnds)
                {
                    if (i < (count + 1) / 2)
                    {
                        localPreferMin = 0f;
                        localPreferMax = 0.35f;
                    }
                    else
                    {
                        localPreferMin = 0.65f;
                        localPreferMax = 1f;
                    }
                }

                bool placed = false;
                for (int attempt = 0; attempt < 48 && !placed; attempt++)
                {
                    float progress = Mathf.Lerp(localPreferMin, localPreferMax, (float)rng.NextDouble());
                    // Keep events from clustering at the extreme start of the season.
                    progress = Mathf.Clamp(progress, 0.02f, 0.98f);

                    double durationHours = Lerp(rule.MinDurationHours, rule.MaxDurationHours, rng.NextDouble());
                    double durationSeconds = Math.Max(SecondsPerHour * 0.25d, durationHours * SecondsPerHour);
                    double start = seasonStart + progress * seasonLength;
                    double end = start + durationSeconds;
                    if (end > seasonEnd)
                    {
                        end = seasonEnd;
                        start = Math.Max(seasonStart, end - durationSeconds);
                    }

                    if (rule.UseWeekdayWindow && calendar != null &&
                        !IsInsideWeekdayWindow(calendar, start, end, rule))
                        continue;

                    if (OverlapsWithGap(occupied, start, end, rule.MinGapHours * SecondsPerHour))
                        continue;

                    entries.Add(new SeasonWeatherScheduleEntry
                    {
                        Kind = rule.Kind,
                        StartGameSeconds = start,
                        EndGameSeconds = end
                    });
                    occupied.Add((start, end));
                    placed = true;
                }
            }
        }

        private static void FillAmbient(
            SeasonDefinition definition,
            double seasonStart,
            double seasonEnd,
            System.Random rng,
            List<SeasonWeatherScheduleEntry> entries,
            List<(double start, double end)> occupied)
        {
            occupied.Sort((a, b) => a.start.CompareTo(b.start));

            float clearTarget = Mathf.Lerp(
                definition.ClearSkiesMin,
                definition.ClearSkiesMax,
                (float)rng.NextDouble());

            double cursor = seasonStart;
            var gaps = new List<(double start, double end)>();

            for (int i = 0; i < occupied.Count; i++)
            {
                if (occupied[i].start > cursor)
                    gaps.Add((cursor, occupied[i].start));
                cursor = Math.Max(cursor, occupied[i].end);
            }

            if (cursor < seasonEnd)
                gaps.Add((cursor, seasonEnd));

            double totalGap = 0d;
            for (int i = 0; i < gaps.Count; i++)
                totalGap += Math.Max(0d, gaps[i].end - gaps[i].start);

            double clearBudget = totalGap * clearTarget;
            bool allowPartly = definition.IsAllowed(SeasonWeatherKind.PartlyCloudy);
            bool allowClear = definition.IsAllowed(SeasonWeatherKind.Clear);

            for (int i = 0; i < gaps.Count; i++)
            {
                double gapStart = gaps[i].start;
                double gapEnd = gaps[i].end;
                double gapLen = gapEnd - gapStart;
                if (gapLen <= 1d)
                    continue;

                double clearLen = 0d;
                if (allowClear && clearBudget > 0d)
                {
                    clearLen = Math.Min(gapLen, clearBudget);
                    clearBudget -= clearLen;
                }

                if (clearLen > 1d)
                {
                    entries.Add(new SeasonWeatherScheduleEntry
                    {
                        Kind = SeasonWeatherKind.Clear,
                        StartGameSeconds = gapStart,
                        EndGameSeconds = gapStart + clearLen
                    });
                }

                double remainStart = gapStart + clearLen;
                if (remainStart < gapEnd - 1d)
                {
                    entries.Add(new SeasonWeatherScheduleEntry
                    {
                        Kind = allowPartly ? SeasonWeatherKind.PartlyCloudy : SeasonWeatherKind.Clear,
                        StartGameSeconds = remainStart,
                        EndGameSeconds = gapEnd
                    });
                }
            }
        }

        private static bool IsInsideWeekdayWindow(
            SeasonCalendarConfig calendar,
            double start,
            double end,
            SeasonWeatherEventRule rule)
        {
            // Require the midpoint of the event to sit inside the window.
            double mid = (start + end) * 0.5d;
            int dayIndex = (int)Math.Floor(mid / SeasonCalendarMath.SecondsPerDay);
            double secondsIntoDay = mid - dayIndex * (double)SeasonCalendarMath.SecondsPerDay;
            int hour = (int)Math.Floor(secondsIntoDay / SecondsPerHour);
            int weekday = calendar.GetWeekday(dayIndex);

            int startMinutes = rule.WindowStartWeekday * 24 * 60 + rule.WindowStartHour * 60;
            int endMinutes = rule.WindowEndWeekday * 24 * 60 + rule.WindowEndHour * 60;
            int midMinutes = weekday * 24 * 60 + hour * 60;

            if (endMinutes >= startMinutes)
                return midMinutes >= startMinutes && midMinutes < endMinutes;

            // Window wraps week (not expected for Alpha winter window).
            return midMinutes >= startMinutes || midMinutes < endMinutes;
        }

        private static bool OverlapsWithGap(
            List<(double start, double end)> occupied,
            double start,
            double end,
            double minGapSeconds)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                double oStart = occupied[i].start - minGapSeconds;
                double oEnd = occupied[i].end + minGapSeconds;
                if (start < oEnd && end > oStart)
                    return true;
            }

            return false;
        }

        private static int RandomInclusive(System.Random rng, int min, int max)
        {
            if (max < min)
                max = min;
            return rng.Next(min, max + 1);
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
