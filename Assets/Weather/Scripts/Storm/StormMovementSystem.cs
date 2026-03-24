using System;
using Game.Core.WorldTime;
using Game.Weather.Convergence;
using Game.Weather.Global;
using UnityEngine;

namespace Game.Weather.Storm
{
    public class StormMovementSystem : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private GlobalWindSystem _globalWind;
        [SerializeField] private ConvergenceManager _convergenceManager;
        [SerializeField] private StormLifecycleManager _stormLifecycle;
        [SerializeField] private StormMovementConfig _stormMovementConfig;

        [Header("Bounds Source")]
        [SerializeField] private WeatherWorldBoundsProvider _worldBoundsProvider;

        [Header("Terrain Bounds (World XZ)")] 
        [SerializeField] private Vector2 _terrainMin;
        [SerializeField] private Vector2 _terrainMax;

        private const double SecondsPerHour = 3600.0;

        private void OnEnable()
        {
            if (_worldTime == null)
            {
                _worldTime = FindAnyObjectByType<WorldTime>();
            }

            if (_worldTime == null ||
                _globalWind == null ||
                _convergenceManager == null ||
                _stormLifecycle == null ||
                _stormMovementConfig == null)
            {
                Debug.LogError("Missing References.");
                enabled = false;
                return;
            }

            RefreshTerrainBounds();
            _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
        }

        private void OnDisable()
        {
            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
            }
        }

        private void RefreshTerrainBounds()
        {
            if (_worldBoundsProvider == null)
            {
                return;
            }

            if(_worldBoundsProvider.TryGetBounds(out var min, out var max))
            {
                _terrainMin = min;
                _terrainMax = max;
            }
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double now)
        {
            float deltaHours = (float) (deltaGameSeconds / SecondsPerHour);

            foreach (var storm in _stormLifecycle.ActiveStorm)
            {
                // if(!storm.IsActive)
                //     continue;
                float speedMultiplier = storm.IsActive ? 1.0f : 0.3f; // Forming = 30% speed
                Vector2 velocity = ComputeVelocity(storm);
                storm.Position += velocity * deltaHours * speedMultiplier;

                if (storm.view != null)
                {
                    storm.view.transform.position = new Vector3(storm.Position.x, 0.5f, storm.Position.y);
                }

                HandleBounds(storm);
            }
            if (_stormMovementConfig.EnableMerging)
            {
                CheckAndMergeStorms();
            }
        }

        private Vector2 ComputeVelocity(Storm storm)
        {
            Vector2 windDir = _globalWind.CurrentBiasVector.normalized;

            Vector2 convergenceDir = Vector2.zero;
            var nearest = FindNearestConvergence(storm.Position);
            if (nearest != null)
            {
                convergenceDir = (nearest.Position - storm.Position).normalized;
            }

            Vector2 combined = windDir * _stormMovementConfig.WindWeight +
                               convergenceDir * _stormMovementConfig.ConvergenceWeight;

            if (combined.sqrMagnitude < 0.001f)
            {
                combined = windDir;
            }
            
            combined.Normalize();

            return combined * _stormMovementConfig.BaseSpeed;
        }

        private ConvergencePoint FindNearestConvergence(Vector2 pos)
        {
            ConvergencePoint closest = null;
            float bestDist = float.MaxValue;

            foreach (var p in _convergenceManager.ActivePoints)
            {
                float distance = Vector2.SqrMagnitude(p.Position - pos);
                if (distance < bestDist)
                {
                    bestDist = distance;
                    closest = p;
                }
            }
            
            return closest;
        }

        private void HandleBounds(Storm storm)
        {
            bool outside = storm.Position.x < _terrainMin.x ||
                           storm.Position.x > _terrainMax.x ||
                           storm.Position.y < _terrainMin.y ||
                           storm.Position.y > _terrainMax.y;

            if (!outside)
                return;
            if (_stormMovementConfig.DestroyIfOutsideBounds)
            {
                storm.State = StormState.Exited; 
                
                if(storm.view != null)
                    Destroy(storm.view.gameObject);
            }
        }

        // Check all storm pairs and merge if overlap
        private void CheckAndMergeStorms()
        {
            var storms = _stormLifecycle.GetStormList();

            for (int i = storms.Count - 1; i >= 0; i--)
            {
                //Only merge Active Storms
                // if (!storms[i].IsActive)
                //     continue;

                for (int j = i - 1; j >= 0; j--)
                {
                    //Only merge Active Storms
                    // if (!storms[j].IsActive)
                    //     continue;
                    
                    Storm stormA = storms[i];
                    Storm stormB = storms[j];

                    if (ShouldMerge(stormA, stormB))
                    {
                        MergeStorms(stormA, stormB, i, j);
                        return;
                    }
                }
            }
        }

        private bool ShouldMerge(Storm stormA, Storm stormB)
        {
            float distance = Vector2.Distance(stormA.Position, stormB.Position);
            
            float mergeThreshold = (stormA.Radius + stormB.Radius)*_stormMovementConfig.MergeOverlapThreshold;
            
            return distance < mergeThreshold;
        }
        
        // Execute Merge 2 Storms => 1
        private void MergeStorms(Storm stormA, Storm stormB, int indexA, int indexB)
        {
            Storm absorber, absorbed;
            int absorbedIndex;

            if (stormA.Radius >= stormB.Radius)
            {
                absorber = stormA;
                absorbed = stormB;
                absorbedIndex = indexB;
            }
            else
            {
                absorber = stormB;
                absorbed = stormA;
                absorbedIndex = indexA;
            }
            
            Debug.Log($"$[Storm Merge] Absorber radius: {absorber.Radius:F2}, Absorbed radius: {absorbed.Radius:F2}");
            
            float radiusSquaredSum = absorber.Radius * absorber.Radius + absorbed.Radius * absorbed.Radius;
            float newRadius = Mathf.Sqrt(radiusSquaredSum);
            
            // Cap maximum radius
            newRadius = Mathf.Min(newRadius, _stormMovementConfig.MaxMergedRadius);
            
            // Calculate new pos
            float weight1 = absorber.Radius;
            float weight2 = absorbed.Radius;
            float totalWeight = weight1 + weight2;

            Vector2 newPos = (absorber.Position * weight1 + absorbed.Position * weight2) / totalWeight;
            
            absorber.Radius = newRadius;
            absorber.Position = newPos;
            if (stormA.IsActive || stormB.IsActive)
            {
                absorber.IsActive = true;
                absorber.State = StormState.Active;
    
                Debug.Log($"[Storm Merge] Merged storm is now Active (was: A={stormA.State}, B={stormB.State})");
            }
            else
            {
                // Both are Forming → merged storm remains Forming
                absorber.IsActive = false;
                absorber.State = StormState.Forming;
    
                Debug.Log($"[Storm Merge] Merged storm remains Forming");
            }
            if (absorber.ExpireGameSeconds < absorbed.ExpireGameSeconds)
            {
                absorber.ExpireGameSeconds = absorbed.ExpireGameSeconds;
            }

            if (absorber.view != null)
            {
                absorber.view.UpdateState(absorber.State);
                // update circle radius
                absorber.view.Initialize(absorber.Radius);
                
                // update pos
                absorber.view.transform.position = new Vector3(
                    absorber.Position.x,
                    0.5f,
                    absorber.Position.y
                );
            }
            
            _stormLifecycle.RemoveStormAt(absorbedIndex);
            
            Debug.Log($"[Storm Merge] Complete!");
        }
    }
}