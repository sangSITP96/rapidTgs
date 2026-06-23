using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Weather.Cloud;
using Game.Weather.Convergence;
using UnityEngine;

namespace Game.Weather.Storm
{
    public class CloudToStormConverter : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private ConvergenceManager _convergenceManager;
        [SerializeField] private StormLifecycleManager _stormLifecycle;

        [Header("Gathering Rules")]
        [SerializeField] private bool _stormFormationEnabled = false;

        [Tooltip("Radius around convergence point to check for clouds")] [SerializeField]
        private float _gatherRadius = 2.0f;

        [Tooltip("Minimum clouds needed to form a storm")] [SerializeField]
        private int _minCloudsToFormStorm = 3;

        [SerializeField] private float _checkIntervalSeconds = 5f;

        private double _nextCheckAt;

        private void OnEnable()
        {
            if (!_stormFormationEnabled)
            {
                enabled = false;
                return;
            }

            if (_worldTime == null) _worldTime = FindFirstObjectByType<WorldTime>();
            if (_cloudManager == null) _cloudManager = FindFirstObjectByType<CloudManager>();
            if (_convergenceManager == null) _convergenceManager = FindFirstObjectByType<ConvergenceManager>();
            if (_stormLifecycle == null) _stormLifecycle = FindFirstObjectByType<StormLifecycleManager>();

            if (_worldTime == null || _cloudManager == null ||
                _convergenceManager == null || _stormLifecycle == null)
            {
                Debug.LogError("CloudToStormConverter: Missing dependencies");
                enabled = false;
                return;
            }

            _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
            _nextCheckAt = _worldTime.TotalGameSeconds + _checkIntervalSeconds;
        }

        private void OnDisable()
        {
            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
            }
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double now)
        {
            if (now < _nextCheckAt)
                return;

            _nextCheckAt = now + _checkIntervalSeconds;

            CheckAndConvertClouds(now);
        }

        private void CheckAndConvertClouds(double now)
        {
            var convergencePoints = _convergenceManager.ActivePoints;
            if (convergencePoints == null || convergencePoints.Count == 0)
                return;
            foreach (var convergencePoint in convergencePoints)
            {
                TryFormStormAtConvergence(convergencePoint, now);
            }
        }

        private void TryFormStormAtConvergence(ConvergencePoint convergencePoint, double now)
        {
            List<Cloud.Cloud> gatheredClouds = new List<Cloud.Cloud>();
            foreach (var cloud in _cloudManager.ActiveClouds)
            {
                float distance = Vector2.Distance(cloud.Position, convergencePoint.Position);

                if (distance <= _gatherRadius)
                {
                    gatheredClouds.Add(cloud);
                }
            }

            // Check if enough clouds to form storm
            if (gatheredClouds.Count < _minCloudsToFormStorm)
                return;
            
            Debug.Log($"[CloudToStorm] Found {gatheredClouds.Count} clouds near convergence"); 

            // Calculate average position of gathered clouds
            Vector2 avgPosition = Vector2.zero;
            foreach (var cloud in gatheredClouds)
            {
                avgPosition += cloud.Position;
            }

            avgPosition /= gatheredClouds.Count;

            // Check if all clouds are actually clustered together
            // Clouds must be within a reasonable radius from their center
            float maxCloudDistance = _gatherRadius * 0.4f; // 80% of gather radius
            foreach (var cloud in gatheredClouds)
            {
                float distFromCenter = Vector2.Distance(cloud.Position, avgPosition);
                if (distFromCenter > maxCloudDistance)
                {
                    // Clouds are too spread out, not concentrated enough to form storm
                    return;
                }
            }

            // Calculate storm properties from clouds
            float totalRadius = 0f;
            List<double> cloudExpireTimes = new List<double>();

            foreach (var cloud in gatheredClouds)
            {
                totalRadius += cloud.Radius * cloud.Radius; // Sum of areas
                cloudExpireTimes.Add(cloud.ExpireGameSeconds);
            }

            // Radius = sqrt(sum of areas) - conservation of "mass"
            float stormRadius = Mathf.Sqrt(totalRadius);

            // Create storm
            Storm storm = _stormLifecycle.CreateStormFromClouds(
                convergencePoint.Position,
                stormRadius,
                now,
                cloudExpireTimes
            );

            if (storm != null)
            {
                // Remove absorbed clouds
                foreach (var cloud in gatheredClouds)
                {
                    cloud.State = CloudState.Absorbed;
                    _cloudManager.RemoveCloud(cloud);
                }
            }
        }
    }
}