using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Seasons
{
    [Serializable]
    public sealed class SeasonWeatherTypeMap
    {
        public SeasonWeatherKind Kind;
        public List<WeatherType> GameplayTypes = new List<WeatherType>();
    }

    [CreateAssetMenu(menuName = "Game/Seasons/Season Weather Mapping", fileName = "SeasonWeatherMapping")]
    public sealed class SeasonWeatherMapping : ScriptableObject
    {
        public List<SeasonWeatherTypeMap> Maps = new List<SeasonWeatherTypeMap>();

        public IReadOnlyList<WeatherType> GetGameplayTypes(SeasonWeatherKind kind)
        {
            if (Maps != null)
            {
                for (int i = 0; i < Maps.Count; i++)
                {
                    if (Maps[i] != null && Maps[i].Kind == kind)
                    {
                        if (Maps[i].GameplayTypes != null)
                            return Maps[i].GameplayTypes;
                        return s_Empty;
                    }
                }
            }

            return GetDefaultGameplayTypes(kind);
        }

        public static IReadOnlyList<WeatherType> GetDefaultGameplayTypes(SeasonWeatherKind kind)
        {
            switch (kind)
            {
                case SeasonWeatherKind.SteadyRain:
                    return s_SteadyRain;
                case SeasonWeatherKind.HeavyRain:
                    return s_HeavyRain;
                case SeasonWeatherKind.Thunderstorm:
                    return s_Thunderstorm;
                case SeasonWeatherKind.Snow:
                    return s_Snow;
                case SeasonWeatherKind.Snowstorm:
                    return s_Snowstorm;
                case SeasonWeatherKind.MightyStorm:
                    return s_MightyStorm;
                default:
                    return s_Empty;
            }
        }

        private static readonly WeatherType[] s_Empty = Array.Empty<WeatherType>();
        private static readonly WeatherType[] s_SteadyRain = { WeatherType.LightRain, WeatherType.ModerateRain };
        private static readonly WeatherType[] s_HeavyRain = { WeatherType.HeavyRain, WeatherType.BrutalRain };
        private static readonly WeatherType[] s_Thunderstorm = { WeatherType.Thunderstorm, WeatherType.HeavyRain };
        private static readonly WeatherType[] s_Snow = { WeatherType.LightSnow, WeatherType.ModerateSnow };
        private static readonly WeatherType[] s_Snowstorm = { WeatherType.HeavySnow, WeatherType.Blizzard };
        private static readonly WeatherType[] s_MightyStorm = { WeatherType.Thunderstorm, WeatherType.BrutalRain, WeatherType.Blizzard };
    }
}
