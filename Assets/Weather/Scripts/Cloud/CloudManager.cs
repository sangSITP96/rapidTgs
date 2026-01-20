using System;
using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Weather.Convergence;
using Game.Weather.Core;
using Game.Weather.Global;
using Game.Weather.Lake;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Weather.Cloud
{
    public class CloudManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private WorldTick _cloudSpawnTick; // 5-10 minutes
        [SerializeField] private GlobalWindSystem _windSystem;
        [SerializeField] private LakeDetector _lakeDetector;

        [SerializeField] private ConvergenceManager _convergenceManager;
        
        [SerializeField] private CoverageTracker _coverageTracker;

        [Header("Config")] [SerializeField] private float _cloudSpawnChance = 0.2f;
        [SerializeField] private float _cloudSpeed = 2f;
        [SerializeField] private float _spawnDuration = 2f;
        [SerializeField] private float _minLifetimeMinutes = 2f;
        [SerializeField] private float _maxLifetimeMinutes = 15f;
        
        [Header("Convergence")]
        [SerializeField] private float _convergenceAttractionRadius = 3f;
        [SerializeField] private float _convergenceSlowdownFactor = 0.3f;
        [SerializeField] private float _convergenceAttractionStrength = 0.5f;
        
        [Header("Visual")]
        [SerializeField] private GameObject _cloudPrefab;

        public IReadOnlyList<Cloud> ActiveClouds => _clouds;

        private readonly List<Cloud> _clouds = new();
        private int _nextCloudId = 0;

        private const double SecondsPerMinute = 60.0;
        private const double SecondsPerHour = 3600.0;

        private float localScaleCloudObj = 0.15f;

        private void OnEnable()
        {
            if (_worldTime == null) _worldTime = FindFirstObjectByType<WorldTime>();
            if (_windSystem == null) _windSystem = FindFirstObjectByType<GlobalWindSystem>();

            if (_cloudSpawnTick != null)
            {
                _cloudSpawnTick.OnTick += HandleSpawnTick;
            }

            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
            }
        }

        private void OnDisable()
        {
            if (_cloudSpawnTick != null)
            {
                _cloudSpawnTick.OnTick -= HandleSpawnTick;
            }

            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
            }
        }
        
        // Spawn clouds from lakes
        private void HandleSpawnTick(long tickIndex, double gameTime)
        {
            if(_lakeDetector == null || _lakeDetector.Lakes == null) return;

            foreach (var lake in _lakeDetector.Lakes)
            {
                float chance = lake.GetCloudSpawnChance(_cloudSpawnChance);
                if (Random.value < chance)
                {
                    SpawnCloudAtLake(lake, gameTime);
                }
            }
        }

        private void SpawnCloudAtLake(Lake.Lake lake, double now)
        {
            if (_coverageTracker != null && !_coverageTracker.CanSpawnNewCloud())
            {
                Debug.Log("[Cloud] Cannot spawn - coverage limit reached");
                return;
            }
            if (float.IsNaN(lake.Center.x) || float.IsNaN(lake.Center.y))
            {
                Debug.LogError($"[Cloud] Invalid lake center: {lake.Center} for lake {lake.Id}");
                return;
            }
    
            if (Mathf.Abs(lake.Center.x) > 1000f || Mathf.Abs(lake.Center.y) > 1000f)
            {
                Debug.LogWarning($"[Cloud] Lake {lake.Id} center {lake.Center} seems out of bounds");
            }
            double lifetimeMinutes = Random.Range(_minLifetimeMinutes, _maxLifetimeMinutes);
            double lifetimeSeconds = lifetimeMinutes * SecondsPerMinute;

            Cloud cloud = new Cloud
            {
                Id = _nextCloudId++,
                Position = lake.Center + Random.insideUnitCircle * 0.5f,
                Radius = Random.Range(0.8f, 1.5f),
                State = CloudState.Spawning,
                SpawnGameSeconds = now,
                ExpireGameSeconds = now + lifetimeSeconds,
                SourceLakeId = lake.Id,
                SpawnTimer = 0f
            };

            if (_cloudPrefab != null)
            {
                cloud.VisualObject = Instantiate(
                    _cloudPrefab,
                    new Vector3(cloud.Position.x, 1f, cloud.Position.y), 
                    Quaternion.identity);
                cloud.VisualObject.transform.localScale = new Vector3(localScaleCloudObj, 0.05f, localScaleCloudObj)*  cloud.Radius;
            }
            
            _clouds.Add(cloud);
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double now)
        {
            float deltaHours = (float)(deltaGameSeconds / SecondsPerHour);

            for (int i = _clouds.Count - 1; i >= 0; i--)
            {
                Cloud cloud = _clouds[i];
                if (cloud.IsExpired(now))
                {
                    DestroyCloud(cloud, i);
                    continue;
                }

                UpdateCloudState(cloud, (float)deltaGameSeconds);

                if (cloud.State == CloudState.Drifting || cloud.State == CloudState.InConvergence)
                {
                    MoveCloud(cloud, deltaHours);
                }

                if (cloud.VisualObject != null)
                {
                    cloud.VisualObject.transform.position = new Vector3(cloud.Position.x, 1f, cloud.Position.y);
                }
            }
        }

        private void UpdateCloudState(Cloud cloud, float deltaGameSeconds)
        {
            if (cloud.State == CloudState.Spawning)
            {
                cloud.SpawnTimer += deltaGameSeconds;
                if (cloud.SpawnTimer >= _spawnDuration)
                {
                    cloud.State = CloudState.Drifting;
                }
            }
        }

        private void MoveCloud(Cloud cloud, float deltaHours)
        {
            Vector2 windDir = _windSystem.CurrentBiasVector.normalized;
            Vector2 movement = windDir * _cloudSpeed * deltaHours;
            
            // Check nearest convergence point
            ConvergencePoint nearestConvergence = FindNearestConvergence(cloud.Position);

            if (nearestConvergence != null)
            {
                float distance = Vector2.Distance(cloud.Position, nearestConvergence.Position);
                if (distance < _convergenceAttractionRadius)
                {
                    cloud.State = CloudState.InConvergence;
                    movement *= _convergenceSlowdownFactor;
                    
                    Vector2 toConvergence = (nearestConvergence.Position - cloud.Position).normalized;
                    movement += toConvergence * (_cloudSpeed * _convergenceAttractionStrength) * deltaHours;
                }
                else if(cloud.State == CloudState.InConvergence)
                {
                    cloud.State = CloudState.Drifting;
                }
            }
            cloud.Position += movement;
        }

        private ConvergencePoint FindNearestConvergence(Vector2 position)
        {
            if (_convergenceManager == null || _convergenceManager.ActivePoints == null)
                return null;
        
            ConvergencePoint nearest = null;
            float minDistance = float.MaxValue;
            foreach (var point in _convergenceManager.ActivePoints)
            {
                float distance = Vector2.Distance(position, point.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = point;
                }
            }
            return nearest;
        }

        private void DestroyCloud(Cloud cloud, int index)
        {
            if (cloud.VisualObject != null)
            {
                Destroy(cloud.VisualObject);
            }
            _clouds.RemoveAt(index);
        }

        public void RemoveCloud(Cloud cloud)
        {
            int index = _clouds.IndexOf(cloud);
            if (index >= 0)
            {
                DestroyCloud(cloud, index);
            }
        }
    } 
}