using System.Collections.Generic;
using UnityEngine;

namespace Game.Seasons
{
    /// <summary>
    /// Builds Alpha season definitions in code when ScriptableObject assets are not assigned.
    /// </summary>
    public static class SeasonDefinitionFactory
    {
        public static SeasonDefinition CreateRuntime(SeasonId season)
        {
            SeasonDefinition def = ScriptableObject.CreateInstance<SeasonDefinition>();
            def.hideFlags = HideFlags.HideAndDontSave;
            def.Season = season;
            def.DurationDays = 7;
            def.MightyStormEnabled = false;

            switch (season)
            {
                case SeasonId.Spring:
                    def.MinTemperatureF = 50f;
                    def.MaxTemperatureF = 79f;
                    def.ClearSkiesMin = 0.60f;
                    def.ClearSkiesMax = 0.70f;
                    def.TransitionMinHours = 1f;
                    def.TransitionMaxHours = 6f;
                    def.WeatherRules = new List<SeasonWeatherEventRule>
                    {
                        Event(SeasonWeatherKind.Fog, true, true, 3, 5, 1f, 4f),
                        Event(SeasonWeatherKind.SteadyRain, true, true, 5, 7, 1f, 5f),
                        Event(SeasonWeatherKind.Clear, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.PartlyCloudy, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Thunderstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.HeavyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snow, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snowstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.MightyStorm, false, false, 0, 0, 0f, 0f)
                    };
                    break;

                case SeasonId.Summer:
                    def.MinTemperatureF = 80f;
                    def.MaxTemperatureF = 102f;
                    def.ClearSkiesMin = 0.35f;
                    def.ClearSkiesMax = 0.59f;
                    def.TransitionMinHours = 6f;
                    def.TransitionMaxHours = 12f;
                    def.WeatherRules = new List<SeasonWeatherEventRule>
                    {
                        Event(SeasonWeatherKind.Thunderstorm, true, true, 2, 5, 1f, 4f),
                        Event(SeasonWeatherKind.Clear, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.PartlyCloudy, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Fog, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.SteadyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.HeavyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snow, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snowstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.MightyStorm, false, false, 0, 0, 0f, 0f)
                    };
                    break;

                case SeasonId.Autumn:
                    def.MinTemperatureF = 50f;
                    def.MaxTemperatureF = 79f;
                    def.ClearSkiesMin = 0.60f;
                    def.ClearSkiesMax = 0.70f;
                    def.TransitionMinHours = 6f;
                    def.TransitionMaxHours = 12f;
                    def.WeatherRules = new List<SeasonWeatherEventRule>
                    {
                        Event(SeasonWeatherKind.SteadyRain, true, true, 3, 5, 1f, 3f),
                        Event(SeasonWeatherKind.Clear, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.PartlyCloudy, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Fog, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Thunderstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.HeavyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snow, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Snowstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.MightyStorm, false, false, 0, 0, 0f, 0f)
                    };
                    break;

                default: // Winter
                    def.MinTemperatureF = -15f;
                    def.MaxTemperatureF = 49f;
                    def.ClearSkiesMin = 0.25f;
                    def.ClearSkiesMax = 0.49f;
                    def.TransitionMinHours = 6f;
                    def.TransitionMaxHours = 12f;
                    def.MightyStormEnabled = false;
                    var snowstorm = Event(SeasonWeatherKind.Snowstorm, true, true, 3, 5, 1f, 4f);
                    snowstorm.UseWeekdayWindow = true;
                    snowstorm.WindowStartWeekday = 1; // Tuesday
                    snowstorm.WindowStartHour = 18;
                    snowstorm.WindowEndWeekday = 4;   // Friday
                    snowstorm.WindowEndHour = 12;
                    snowstorm.MinGapHours = 8f;

                    var snow = Event(SeasonWeatherKind.Snow, true, true, 2, 4, 1f, 4f);
                    snow.PreferProgressMin = 0f;
                    snow.PreferProgressMax = 0.35f;
                    // Second preference handled by splitting counts in scheduler via another rule-like placement:
                    // we place half near start and half near end via PreferProgressMax mid override in scheduler.

                    def.WeatherRules = new List<SeasonWeatherEventRule>
                    {
                        snowstorm,
                        snow,
                        Event(SeasonWeatherKind.Clear, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.PartlyCloudy, true, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Fog, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.SteadyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.HeavyRain, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.Thunderstorm, false, false, 0, 0, 0f, 0f),
                        Event(SeasonWeatherKind.MightyStorm, false, false, 0, 0, 0f, 0f)
                    };
                    break;
            }

            return def;
        }

        private static SeasonWeatherEventRule Event(
            SeasonWeatherKind kind,
            bool allowed,
            bool schedule,
            int minCount,
            int maxCount,
            float minHours,
            float maxHours)
        {
            return new SeasonWeatherEventRule
            {
                Kind = kind,
                Allowed = allowed,
                ScheduleAsEvents = schedule,
                MinCountPerSeason = minCount,
                MaxCountPerSeason = maxCount,
                MinDurationHours = minHours,
                MaxDurationHours = maxHours,
                PreferProgressMin = 0f,
                PreferProgressMax = 1f,
                MinGapHours = 6f
            };
        }
    }
}
