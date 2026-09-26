namespace Game.Core.WeatherGate
{
    /// <summary>
    /// Lightweight gate so existing VFX managers can respect Phase 14 season weather
    /// without taking a hard dependency on Season types.
    /// </summary>
    public interface ISeasonWeatherGate
    {
        bool CanSpawnFog { get; }
        bool CanSpawnAmbientClouds { get; }
        bool CanEvolveRain { get; }
        bool CanFormStorm { get; }
        bool ShouldSuppressStormEventLog { get; }
    }

    public static class SeasonWeatherGate
    {
        public static ISeasonWeatherGate Current { get; set; }

        public static bool AllowsFog => Current == null || Current.CanSpawnFog;
        public static bool AllowsAmbientClouds => Current == null || Current.CanSpawnAmbientClouds;
        public static bool AllowsRainEvolution => Current == null || Current.CanEvolveRain;
        public static bool AllowsStormFormation => Current == null || Current.CanFormStorm;
        public static bool SuppressStormEventLog =>
            Current != null && Current.ShouldSuppressStormEventLog;
    }
}
