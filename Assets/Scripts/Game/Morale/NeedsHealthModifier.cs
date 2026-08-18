using UnityEngine;

namespace Game.Morale
{
    public sealed class NeedsHealthModifier : IHealthModifier
    {
        private readonly MoraleSettings _settings;

        public NeedsHealthModifier(MoraleSettings settings)
        {
            _settings = settings;
        }

        public float GetHealthChangePerHour(TroopMoraleState state)
        {
            if (state == null || _settings == null)
                return 0f;

            float totalSeverity = 0f;

            totalSeverity += CalculateSeverity(state.Sleep);
            totalSeverity += CalculateSeverity(state.Water);
            totalSeverity += CalculateSeverity(state.Food);

            return -totalSeverity * _settings.MaxHealthLossPerHourPerNeed;
        }

        private float CalculateSeverity(float value)
        {
            float threshold = _settings.HealthDamageThreshold;

            if (value >= threshold)
                return 0f;
            
            if(threshold <= 0f)
                return value <= 0f ? 1f : 0f;
            
            return Mathf.Clamp01((threshold - value) / threshold);
        }
    }
}

