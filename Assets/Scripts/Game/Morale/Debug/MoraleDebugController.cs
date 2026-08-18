using UnityEngine;

namespace  Game.Morale
{
    public sealed class MoraleDebugController : MonoBehaviour
    {
        [SerializeField]
        private TroopMoraleSystem moraleSystem;

        [Header("Debug Values")]
        [SerializeField, Range(0f, 100f)]
        private float health = 100f;

        [SerializeField, Range(0f, 100f)]
        private float sleep = 100f;

        [SerializeField, Range(0f, 100f)]
        private float water = 100f;

        [SerializeField, Range(0f, 100f)]
        private float food = 100f;

        [Header("Debug Consumption Per Hour")]
        [SerializeField, Min(0f)]
        private float sleepConsumptionPerHour = 5f;

        [SerializeField, Min(0f)]
        private float waterConsumptionPerHour = 5f;

        [SerializeField, Min(0f)]
        private float foodConsumptionPerHour = 5f;

        [ContextMenu("Apply Debug Values")]
        private void ApplyDebugValues()
        {
            if (moraleSystem == null)
                return;

            moraleSystem.SetHealth(health);
            moraleSystem.SetSleep(sleep);
            moraleSystem.SetWater(water);
            moraleSystem.SetFood(food);
        }

        [ContextMenu("Simulate 1 Hour")]
        private void SimulateOneHour()
        {
            Simulate(1f);
        }

        [ContextMenu("Simulate 6 Hours")]
        private void SimulateSixHours()
        {
            Simulate(6f);
        }

        private void Simulate(float hours)
        {
            if (moraleSystem == null || hours <= 0f)
                return;

            moraleSystem.ConsumeSleep(
                sleepConsumptionPerHour * hours);

            moraleSystem.ConsumeWater(
                waterConsumptionPerHour * hours);

            moraleSystem.ConsumeFood(
                foodConsumptionPerHour * hours);

            moraleSystem.SimulateHours(hours);
        }

        [ContextMenu("Reset Morale")]
        private void ResetMorale()
        {
            moraleSystem?.ResetState();
        }
    }
}