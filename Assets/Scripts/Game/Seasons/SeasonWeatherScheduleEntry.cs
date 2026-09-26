using System;

namespace Game.Seasons
{
    [Serializable]
    public sealed class SeasonWeatherScheduleEntry
    {
        public SeasonWeatherKind Kind;
        public double StartGameSeconds;
        public double EndGameSeconds;

        public bool Contains(double totalGameSeconds)
        {
            return totalGameSeconds >= StartGameSeconds && totalGameSeconds < EndGameSeconds;
        }

        public double DurationGameSeconds => Math.Max(0d, EndGameSeconds - StartGameSeconds);
    }
}
