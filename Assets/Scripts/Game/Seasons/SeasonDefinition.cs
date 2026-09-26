using System.Collections.Generic;
using UnityEngine;

namespace Game.Seasons
{
    [CreateAssetMenu(menuName = "Game/Seasons/Season Definition", fileName = "SeasonDefinition")]
    public sealed class SeasonDefinition : ScriptableObject
    {
        public SeasonId Season = SeasonId.Spring;

        [Header("Duration")]
        [Tooltip("Alpha default: 7 game days. Calendar config can override.")]
        [Min(1)] public int DurationDays = 7;

        [Header("Temperature (°F stored)")]
        public float MinTemperatureF = 50f;
        public float MaxTemperatureF = 79f;

        [Header("Clear / Cloud targets")]
        [Range(0f, 1f)] public float ClearSkiesMin = 0.60f;
        [Range(0f, 1f)] public float ClearSkiesMax = 0.70f;

        [Tooltip("Remaining non-event time after clear target can be partly cloudy.")]
        [Range(0f, 1f)] public float PartlyCloudyFillWeight = 1f;

        [Header("Visual transition into this season")]
        [Min(0f)] public float TransitionMinHours = 1f;
        [Min(0f)] public float TransitionMaxHours = 6f;

        [Header("Mighty Storm (future-ready)")]
        [Tooltip("Alpha: leave disabled. Architecture-ready only.")]
        public bool MightyStormEnabled = false;

        [Header("Weather rules")]
        public List<SeasonWeatherEventRule> WeatherRules = new List<SeasonWeatherEventRule>();

        public bool IsAllowed(SeasonWeatherKind kind)
        {
            SeasonWeatherEventRule rule = FindRule(kind);
            if (rule == null)
                return false;
            if (kind == SeasonWeatherKind.MightyStorm && !MightyStormEnabled)
                return false;
            return rule.Allowed;
        }

        public SeasonWeatherEventRule FindRule(SeasonWeatherKind kind)
        {
            if (WeatherRules == null)
                return null;

            for (int i = 0; i < WeatherRules.Count; i++)
            {
                if (WeatherRules[i] != null && WeatherRules[i].Kind == kind)
                    return WeatherRules[i];
            }

            return null;
        }

        public float EvaluateTemperatureF(float seasonProgress01, float noise01 = 0.5f)
        {
            float t = Mathf.Clamp01(seasonProgress01);
            float n = Mathf.Clamp01(noise01);
            float baseT = Mathf.Lerp(MinTemperatureF, MaxTemperatureF, t);
            float wobble = Mathf.Lerp(-0.15f, 0.15f, n) * (MaxTemperatureF - MinTemperatureF);
            return Mathf.Clamp(baseT + wobble, MinTemperatureF, MaxTemperatureF);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (MaxTemperatureF < MinTemperatureF)
                MaxTemperatureF = MinTemperatureF;
            if (ClearSkiesMax < ClearSkiesMin)
                ClearSkiesMax = ClearSkiesMin;
            if (TransitionMaxHours < TransitionMinHours)
                TransitionMaxHours = TransitionMinHours;

            if (WeatherRules == null)
                return;

            for (int i = 0; i < WeatherRules.Count; i++)
            {
                SeasonWeatherEventRule rule = WeatherRules[i];
                if (rule == null)
                    continue;
                if (rule.MaxCountPerSeason < rule.MinCountPerSeason)
                    rule.MaxCountPerSeason = rule.MinCountPerSeason;
                if (rule.MaxDurationHours < rule.MinDurationHours)
                    rule.MaxDurationHours = rule.MinDurationHours;
                if (rule.PreferProgressMax < rule.PreferProgressMin)
                    rule.PreferProgressMax = rule.PreferProgressMin;
            }
        }
#endif
    }
}
