using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Weather.Cloud;
using UnityEngine;

namespace Game.Weather.Storm
{
    public class StormCloudAbsorber : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private StormLifecycleManager _stormLifecycle;
        
        [Header("Absorption Settings")]
        [Tooltip("How often to check for clouds to absorb (seconds)")]
        [SerializeField] private float _checkIntervalSeconds = 2f;
        
        [Tooltip("Multiplier for absorption radius (storm radius * multiplier)")]
        [SerializeField] private float _absorptionRadiusMultiplier = 1.2f;
        
        [Tooltip("Maximum radius a storm can grow to")]
        [SerializeField] private float _maxStormRadius = 3f;
        
        [Tooltip("Minimum time added per cloud absorbed (game seconds)")]
        [SerializeField] private float _minLifetimeExtensionSeconds = 600f; // 10 minutes
        
        private double _nextCheckAt;
        private const double SecondsPerHour = 3600.0;
        
        private void OnEnable()
        {
            if (_worldTime == null) _worldTime = FindFirstObjectByType<WorldTime>();
            if (_cloudManager == null) _cloudManager = FindFirstObjectByType<CloudManager>();
            if (_stormLifecycle == null) _stormLifecycle = FindFirstObjectByType<StormLifecycleManager>();
            
            if (_worldTime == null || _cloudManager == null || _stormLifecycle == null)
            {
                Debug.LogError("StormCloudAbsorber: Missing dependencies");
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
            
            CheckAndAbsorbClouds(now);
        }

        private void CheckAndAbsorbClouds(double now)
        {
            var storms = _stormLifecycle.ActiveStorm;
            if (storms == null || storms.Count == 0)
                return;

            // Collect clouds to absorb (to avoid modifying collection during iteration)
            List<(Storm storm, Cloud.Cloud cloud)> toAbsorb = new List<(Storm, Cloud.Cloud)>();

            foreach (var storm in storms)
            {
                // Only active storms can absorb clouds
                if (!storm.IsActive)
                    continue;
                
                float absorptionRadius = storm.Radius * _absorptionRadiusMultiplier;
                
                foreach (var cloud in _cloudManager.ActiveClouds)
                {
                    // Skip clouds that are spawning or already absorbed
                    if (cloud.State != CloudState.Drifting && cloud.State != CloudState.InConvergence)
                        continue;
                    
                    float distance = Vector2.Distance(storm.Position, cloud.Position);
                    
                    if (distance <= absorptionRadius)
                    {
                        toAbsorb.Add((storm, cloud));
                    }
                }
            }

            // Execute absorption
            foreach (var (storm, cloud) in toAbsorb)
            {
                AbsorbCloudIntoStorm(storm, cloud, now);
            }
        }

        private void AbsorbCloudIntoStorm(Storm storm, Cloud.Cloud cloud, double now)
        {
            // Calculate new radius using conservation of area
            float currentArea = storm.Radius * storm.Radius;
            float cloudArea = cloud.Radius * cloud.Radius;
            float newArea = currentArea + cloudArea;
            float newRadius = Mathf.Sqrt(newArea);
            
            // Cap maximum radius
            newRadius = Mathf.Min(newRadius, _maxStormRadius);
            
            storm.Radius = newRadius;
            
            // Extend storm lifetime
            double cloudRemainingLifetime = cloud.ExpireGameSeconds - now;
            double lifetimeExtension = Mathf.Max((float)cloudRemainingLifetime, _minLifetimeExtensionSeconds);
            storm.ExpireGameSeconds += lifetimeExtension;
            
            // Update visual
            if (storm.view != null)
            {
                storm.view.Initialize(storm.Radius);
            }
            
            // Remove absorbed cloud
            cloud.State = CloudState.Absorbed;
            _cloudManager.RemoveCloud(cloud);
        }
        
    }
}

