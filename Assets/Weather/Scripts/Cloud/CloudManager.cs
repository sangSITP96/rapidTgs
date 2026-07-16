using Game.Core.WorldTime;
using Game.Weather.Convergence;
using Game.Weather.Core;
using Game.Weather.Lake;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Weather.Cloud
{
    public class CloudManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private WorldTick _cloudSpawnTick; // 5-10 minutes
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
        [SerializeField] private float _convergenceHoldRadius = 1.2f;
        [SerializeField] private float _convergenceAttractionStrength = 0.5f;

        [Header("Released Cloud Exit")]
        [Tooltip("Optional. If empty, bounds are taken directly from the InfiniteMapStreamer.")]
        [SerializeField] private WeatherWorldBoundsProvider _worldBoundsProvider;
        [SerializeField] private InfiniteMapStreamer _mapStreamer;
        [SerializeField] private float _exitBoundsMargin = 0.5f;

        private static readonly Vector2 EastDriftDirection = Vector2.right;

        [Header("Visual")]
        [SerializeField] private CloudVisualLibrary _cloudVisualLibrary;
        [SerializeField] private Transform _cloudVisualParent;
        [SerializeField] private float _cloudVisualHeight = 5f;
        [SerializeField] private bool _applyCategoryScale = false;
        [SerializeField] private float _smallScale = 1f;
        [SerializeField] private float _mediumScale = 1f;
        [SerializeField] private float _largeScale = 1f;
        [SerializeField] private float _clusterScale = 1f;

        [Header("Event Log")]
        [SerializeField] private bool _logCloudSpawnToEventLog = true;
        [SerializeField] private float _cloudEventLogCooldownSeconds = 3f;

        private float _lastCloudEventLogRealTime = -999f;

        public IReadOnlyList<Cloud> ActiveClouds => _clouds;

        private readonly List<Cloud> _clouds = new();
        private int _nextCloudId = 0;

        private const double SecondsPerMinute = 60.0;
        private const double SecondsPerHour = 3600.0;

        private void OnEnable()
        {
            if (_worldTime == null) _worldTime = FindFirstObjectByType<WorldTime>();
            if (_convergenceManager == null) _convergenceManager = FindFirstObjectByType<ConvergenceManager>();

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

            if (_cloudVisualLibrary == null)
            {
                Debug.LogWarning("[Cloud] CloudVisualLibrary is not assigned.");
                return;
            }

            if (!_cloudVisualLibrary.TryPickPrefabForLake(
                lake.Size,
                out GameObject prefab,
                out CloudVisualCategory category,
                out float simulationRadius))
            {
                Debug.LogWarning($"[Cloud] No VFX prefab configured for lake size {lake.Size}");
                return;
            }

            double lifetimeMinutes = Random.Range(_minLifetimeMinutes, _maxLifetimeMinutes);
            double lifetimeSeconds = lifetimeMinutes * SecondsPerMinute;

            Cloud cloud = new Cloud
            {
                Id = _nextCloudId++,
                Position = lake.Center,// + Random.insideUnitCircle * 0.5f,
                Radius = simulationRadius,
                State = CloudState.Spawning,
                SpawnGameSeconds = now,
                ExpireGameSeconds = now + lifetimeSeconds,
                SourceLakeId = lake.Id,
                SourceLakeSize = lake.Size,
                VisualCategory = category,
                SpawnTimer = 0f
            };

            Vector3 spawnPos = GetCloudWorldPosition(cloud.Position);
            cloud.VisualObject = Instantiate(
                prefab,
                spawnPos,
                Quaternion.identity,
                _cloudVisualParent != null ? _cloudVisualParent : transform);

            if (_applyCategoryScale)
            {
                float scale = GetCategoryScale(category);
                cloud.VisualObject.transform.localScale = Vector3.one * scale;
            }

            _clouds.Add(cloud);

            if (_logCloudSpawnToEventLog
                && category >= CloudVisualCategory.Medium
                && Time.unscaledTime - _lastCloudEventLogRealTime >= _cloudEventLogCooldownSeconds)
            {
                _lastCloudEventLogRealTime = Time.unscaledTime;

                ColonyEventLogService.Instance?.AddSimple(
                    EventCategory.Weather,
                    "Cloud Formed",
                    $"A {category} cloud formed over lake #{lake.Id}" +
                    $"(size {lake.Size:F0}) ");
            }
        }

        private Vector3 GetCloudWorldPosition(Vector2 xzPosition)
        {
            float y = _cloudVisualHeight;

            return new Vector3(xzPosition.x, y, xzPosition.y);
        }

        private float GetCategoryScale(CloudVisualCategory category)
        {
            switch (category)
            {
                case CloudVisualCategory.Small:
                    return _smallScale;

                case CloudVisualCategory.Medium:
                    return _mediumScale;

                case CloudVisualCategory.Large:
                    return _largeScale;

                case CloudVisualCategory.Cluster:
                    return _clusterScale;

                default:
                    return 1f;
            }
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

                if (cloud.State != CloudState.Held &&
                    (cloud.State == CloudState.Drifting || cloud.State == CloudState.InConvergence))
                {
                    MoveCloud(cloud, deltaHours);
                }

                if (cloud.IsReleasedFromConvergence && IsOutsideMapBounds(cloud.Position))
                {
                    DestroyCloud(cloud, i);
                    continue;
                }

                if (cloud.VisualObject != null)
                {
                    cloud.VisualObject.transform.position = GetCloudWorldPosition(
                        cloud.Position);
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
            float stepDistance = _cloudSpeed * deltaHours;

            if (cloud.IsReleasedFromConvergence)
            {
                cloud.State = CloudState.Drifting;
                cloud.Position += EastDriftDirection * stepDistance;
                return;
            }

            ConvergencePoint convergenceInRange = FindConvergenceInAttractionRange(cloud.Position);

            if (convergenceInRange != null)
            {
                cloud.State = CloudState.InConvergence;

                if (!cloud.HasHoldTarget)
                    AssignHoldTarget(cloud, convergenceInRange);

                Vector2 targetPos = convergenceInRange.Position + cloud.HoldTargetOffset;
                Vector2 toTarget = targetPos - cloud.Position;

                float pullStrength = Mathf.Max(
                    _convergenceAttractionStrength,
                    convergenceInRange.AttractionStrength);
                float moveDistance = stepDistance * pullStrength;

                if (toTarget.magnitude <= moveDistance || toTarget.sqrMagnitude <= 0.0001f)
                {
                    cloud.Position = targetPos;
                    cloud.State = CloudState.Held;

                    if (!cloud.IsManagedByConvergence)
                        _convergenceManager?.CaptureHeldCloud(cloud, convergenceInRange);

                    return;
                }

                cloud.Position += toTarget.normalized * moveDistance;
                return;
            }

            if (cloud.State == CloudState.InConvergence)
                cloud.State = CloudState.Drifting;

            cloud.HasHoldTarget = false;

            cloud.Position += EastDriftDirection * stepDistance;
        }

        // Distribute clouds around the convergence point instead of stacking on the center.
        // The cloud settles in the same vertical half (upper/lower) it approached from.
        private void AssignHoldTarget(Cloud cloud, ConvergencePoint point)
        {
            float holdRadius = Mathf.Max(0.01f, _convergenceHoldRadius);

            Vector2 toCloud = cloud.Position - point.Position;

            float halfSign;
            if (Mathf.Abs(toCloud.y) < 0.0001f)
                halfSign = Random.value < 0.5f ? 1f : -1f; // cloud sits exactly on the convergence (e.g. lake at center)
            else
                halfSign = Mathf.Sign(toCloud.y);

            float t = Random.Range(0.12f, 0.88f);
            float angle = halfSign > 0f ? Mathf.PI * t : -Mathf.PI * t;
            float radius = Random.Range(holdRadius * 0.25f, holdRadius * 0.85f);

            cloud.HoldTargetOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            cloud.HasHoldTarget = true;
        }

        private ConvergencePoint FindConvergenceInAttractionRange(Vector2 position)
        {
            if (_convergenceManager == null || _convergenceManager.ActivePoints == null)
                return null;

            ConvergencePoint nearest = null;
            float minDistance = float.MaxValue;
            float attractionRadius = Mathf.Max(_convergenceHoldRadius, _convergenceAttractionRadius);

            foreach (var point in _convergenceManager.ActivePoints)
            {
                float distance = Vector2.Distance(position, point.Position);
                if (distance > attractionRadius || distance >= minDistance)
                    continue;

                minDistance = distance;
                nearest = point;
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

        public bool TryGetCloud(int cloudId, out Cloud cloud)
        {
            foreach (Cloud activeCloud in _clouds)
            {
                if (activeCloud.Id != cloudId)
                    continue;

                cloud = activeCloud;
                return true;
            }

            cloud = null;
            return false;
        }

        public bool ReleaseCloudFromConvergence(int cloudId)
        {
            if (!TryGetCloud(cloudId, out Cloud cloud) || !cloud.IsManagedByConvergence)
                return false;

            cloud.IsManagedByConvergence = false;
            cloud.HeldConvergencePointId = -1;
            cloud.IsReleasedFromConvergence = true;
            cloud.HasHoldTarget = false;
            cloud.State = CloudState.Drifting;
            return true;
        }

        private bool IsOutsideMapBounds(Vector2 position)
        {
            if (!TryGetMapBounds(out Vector2 min, out Vector2 max))
                return false;

            return position.x < min.x - _exitBoundsMargin ||
                   position.x > max.x + _exitBoundsMargin ||
                   position.y < min.y - _exitBoundsMargin ||
                   position.y > max.y + _exitBoundsMargin;
        }

        private bool TryGetMapBounds(out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;

            if (_mapStreamer != null &&
                _mapStreamer.TryGetWorldBounds(
                    out float minX, out float maxX, out float minZ, out float maxZ))
            {
                min = new Vector2(minX, minZ);
                max = new Vector2(maxX, maxZ);
                return true;
            }

            if (_worldBoundsProvider == null)
                _worldBoundsProvider = FindFirstObjectByType<WeatherWorldBoundsProvider>();

            if (_worldBoundsProvider != null)
                return _worldBoundsProvider.TryGetBounds(out min, out max);

            return false;
        }

        public void DissipateCloud(int cloudId)
        {
            if (TryGetCloud(cloudId, out Cloud cloud))
                RemoveCloud(cloud);
        }

        public float ConvergenceAttractionRadius => _convergenceAttractionRadius;
        public float ConvergenceHoldRadius => _convergenceHoldRadius;
    }
}