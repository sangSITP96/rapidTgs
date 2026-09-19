using System;
using UnityEngine;
using Game.Core.WorldTime;
using Game.Morale;

namespace Game.Travel
{
    [DefaultExecutionOrder(50)]
    public sealed class TravelNeedsSimulator : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private TroopTravelController _travelController;
        [SerializeField] private TroopMoraleSystem _moraleSystem;
        [SerializeField] private TravelSettings _settings;
        [SerializeField] private TravelWeatherAdapter _weatherAdapter;

        public event Action NeedsSimulated;

        private void OnEnable()
        {
            ResolveRefs();

            if (_worldTime != null)
                _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
        }

        private void OnDisable()
        {
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
        }

        private void ResolveRefs()
        {
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            if (_travelController == null)
                _travelController = GetComponent<TroopTravelController>();
            if (_moraleSystem == null)
                _moraleSystem = FindFirstObjectByType<TroopMoraleSystem>();
            if (_weatherAdapter == null)
                _weatherAdapter = GetComponent<TravelWeatherAdapter>();
            if (_settings == null && _travelController != null)
                _settings = _travelController.Settings;
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double totalGameSeconds)
        {
            SimulateGameSeconds(deltaGameSeconds);
        }

        public void SimulateGameSeconds(double deltaGameSeconds)
        {
            if (deltaGameSeconds <= 0d || _moraleSystem == null || _settings == null)
                return;

            if (_travelController == null)
                return;

            TravelRuntimeState state = _travelController.RuntimeState;
            float hours = (float)(deltaGameSeconds / WorldTime.SecondsPerHour);
            if (hours <= 0f)
                return;

            float weatherNeeds = _weatherAdapter != null
                ? _weatherAdapter.GetNeedsMultiplier()
                : 1f;

            float surfaceNeeds = Mathf.Max(0.01f, state.CurrentSurfaceNeedsModifier);
            float needsScale = weatherNeeds * surfaceNeeds;

            switch (state.Status)
            {
                case TravelStatus.Marching:
                    _moraleSystem.ConsumeSleep(_settings.MarchSleepConsumptionPerHour * hours * needsScale);
                    _moraleSystem.ConsumeWater(_settings.MarchWaterConsumptionPerHour * hours * needsScale);
                    _moraleSystem.ConsumeFood(_settings.MarchFoodConsumptionPerHour * hours * needsScale);
                    break;

                case TravelStatus.Resting:
                    _moraleSystem.RestoreSleep(_settings.RestSleepRestorePerHour * hours);
                    _moraleSystem.ConsumeWater(_settings.RestWaterConsumptionPerHour * hours * needsScale);
                    _moraleSystem.ConsumeFood(_settings.RestFoodConsumptionPerHour * hours * needsScale);
                    break;

                case TravelStatus.Camped:
                case TravelStatus.Idle:
                case TravelStatus.Arrived:
                default:
                    _moraleSystem.ConsumeSleep(_settings.IdleSleepConsumptionPerHour * hours * needsScale);
                    _moraleSystem.ConsumeWater(_settings.IdleWaterConsumptionPerHour * hours * needsScale);
                    _moraleSystem.ConsumeFood(_settings.IdleFoodConsumptionPerHour * hours * needsScale);
                    break;
            }

            _moraleSystem.SimulateHours(hours);
            NeedsSimulated?.Invoke();
        }
    }
}
