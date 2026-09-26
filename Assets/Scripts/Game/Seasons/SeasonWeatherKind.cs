namespace Game.Seasons
{
    /// <summary>
    /// Seasonal weather categories used by the Phase 14 scheduler.
    /// Mapped to gameplay <see cref="WeatherType"/> and VFX gates separately.
    /// </summary>
    public enum SeasonWeatherKind
    {
        Clear = 0,
        PartlyCloudy = 1,
        Fog = 2,
        SteadyRain = 3,
        HeavyRain = 4,
        Thunderstorm = 5,
        Snow = 6,
        Snowstorm = 7,
        /// <summary>Future-ready only. Not scheduled in Alpha.</summary>
        MightyStorm = 8
    }
}
