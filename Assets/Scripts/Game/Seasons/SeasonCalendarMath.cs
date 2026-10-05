using System;

namespace Game.Seasons
{
    public static class SeasonCalendarMath
    {
        public const int SecondsPerDay = 86400;
        public const int SeasonCount = 4;

        public static void Resolve(
            double totalGameSeconds,
            int daysPerSeason,
            SeasonId startingSeason,
            out SeasonId season,
            out int seasonIndexAbsolute,
            out int dayInSeason,
            out float seasonProgress01,
            out double seasonStartGameSeconds,
            out double seasonEndGameSeconds)
        {
            daysPerSeason = Math.Max(1, daysPerSeason);
            double safeTotal = Math.Max(0d, totalGameSeconds);
            int dayIndex = (int)Math.Floor(safeTotal / SecondsPerDay);
            double secondsIntoDay = safeTotal - dayIndex * (double)SecondsPerDay;

            seasonIndexAbsolute = dayIndex / daysPerSeason;
            int seasonOrdinal = Mod(seasonIndexAbsolute + (int)startingSeason, SeasonCount);
            season = (SeasonId)seasonOrdinal;

            dayInSeason = dayIndex % daysPerSeason;
            double secondsIntoSeason =
                dayInSeason * (double)SecondsPerDay + secondsIntoDay;
            double seasonLengthSeconds = daysPerSeason * (double)SecondsPerDay;
            seasonProgress01 = seasonLengthSeconds > 0d
                ? (float)(secondsIntoSeason / seasonLengthSeconds)
                : 0f;

            int seasonStartDay = seasonIndexAbsolute * daysPerSeason;
            seasonStartGameSeconds = seasonStartDay * (double)SecondsPerDay;
            seasonEndGameSeconds = seasonStartGameSeconds + seasonLengthSeconds;
        }

        public static double GetSeasonStartGameSeconds(
            SeasonId target,
            int daysPerSeason,
            SeasonId startingSeason,
            int fromDayIndex)
        {
            daysPerSeason = Math.Max(1, daysPerSeason);
            int absoluteSeason = fromDayIndex / daysPerSeason;
            SeasonId current = (SeasonId)Mod(absoluteSeason + (int)startingSeason, SeasonCount);

            int stepsForward = Mod((int)target - (int)current, SeasonCount);
            int targetAbsolute = absoluteSeason + stepsForward;
            return targetAbsolute * daysPerSeason * (double)SecondsPerDay;
        }

        private static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }
    }
}
