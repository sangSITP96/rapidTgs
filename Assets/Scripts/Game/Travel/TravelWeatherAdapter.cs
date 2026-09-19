using UnityEngine;

namespace Game.Travel
{
    public sealed class TravelWeatherAdapter : MonoBehaviour
    {
        [SerializeField] private WeatherManager _weatherManager;

        [Header("Fallback when WeatherManager is missing")]
        [SerializeField, Min(0f)] private float _fallbackMovementMultiplier = 1f;
        [SerializeField, Min(0f)] private float _fallbackNeedsMultiplier = 1f;

        private void Awake()
        {
            if (_weatherManager == null)
                _weatherManager = FindFirstObjectByType<WeatherManager>();
        }

        public float GetMovementMultiplier()
        {
            if (_weatherManager == null)
                return _fallbackMovementMultiplier;

            return Mathf.Max(0f, _weatherManager.GetTravelMovementMultiplier());
        }

        public float GetNeedsMultiplier()
        {
            if (_weatherManager == null)
                return _fallbackNeedsMultiplier;

            return Mathf.Max(0f, _weatherManager.GetTravelNeedsMultiplier());
        }
    }
}
