using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Weather.Cloud;
using UnityEngine;
using Random = UnityEngine.Random;
using CloudEntity = Game.Weather.Cloud.Cloud;

namespace Game.Weather.Convergence
{
    [DefaultExecutionOrder(20)]
    public class ConvergenceManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private ConvergenceConfig _config;

        [Header("Cloud Management")]
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private CloudVisualLibrary _cloudVisualLibrary;

        [Header("Visual")]
        [SerializeField] private ConvergenceDebugView _convergenceVisualPrefab;
        [SerializeField] private float _visualRadius = 1.5f;

        [Header("Spawn")]
        [SerializeField] private InfiniteMapStreamer _mapStreamer;
        [SerializeField] private Vector2Int _spawnChunkCoord = Vector2Int.zero;
        [SerializeField] private int _initialPointCount = 1;

        [Header("Terrain Bounds Fallback (World XZ)")]
        [SerializeField] private Vector2 _terrainMin;
        [SerializeField] private Vector2 _terrainMax;

        [Header("Bounds Source")]
        [SerializeField] private WeatherWorldBoundsProvider _worldBoundsProvider;

        public IReadOnlyList<ConvergencePoint> ActivePoints => _points;

        private readonly List<ConvergencePoint> _points = new();
        private readonly Dictionary<int, HeldCloudRecord> _heldByCloudId = new();
        private readonly Dictionary<int, List<HeldCloudRecord>> _heldByPointId = new();

        private int _nextPointId;
        private double _heldCloudCheckAccumulator;

        private const double SecondsPerHour = 3600.0;

        private class HeldCloudRecord
        {
            public int CloudId;
            public int PointId;
            public float Intensity;
        }

        private void OnEnable()
        {
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();

            if (_cloudManager == null)
                _cloudManager = FindFirstObjectByType<CloudManager>();

            if (_worldTime == null || _config == null)
            {
                Debug.LogError("No WorldTime or Config object found");
                enabled = false;
                return;
            }

            RefreshTerrainBounds();
            _worldTime.OnTimeAdvanced += HandleTimeAdvance;
        }

        private void Start()
        {
            ResolveMapStreamer();
            InitializePoints();
        }

        private void ResolveMapStreamer()
        {
            if (_mapStreamer == null)
                _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();
        }

        private void RefreshTerrainBounds()
        {
            if (_worldBoundsProvider == null)
                return;

            if (_worldBoundsProvider.TryGetBounds(out var min, out var max))
            {
                _terrainMin = min;
                _terrainMax = max;
            }
        }

        private void OnDisable()
        {
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced -= HandleTimeAdvance;

            foreach (var point in _points)
            {
                if (point.VisualObject != null)
                    Destroy(point.VisualObject.gameObject);
            }
        }

        private void HandleTimeAdvance(double deltaGameSeconds, double now)
        {
            UpdateLifetime(now);

            _heldCloudCheckAccumulator += deltaGameSeconds;
            if (_heldCloudCheckAccumulator < _config.HeldCloudCheckIntervalSeconds)
                return;

            _heldCloudCheckAccumulator = 0;
            ProcessHeldClouds();
        }

        public void CaptureHeldCloud(CloudEntity cloud, ConvergencePoint point)
        {
            if (cloud == null || point == null || cloud.IsManagedByConvergence)
                return;

            float intensity = _cloudVisualLibrary != null
                ? _cloudVisualLibrary.GetConvergenceIntensity(cloud.VisualCategory)
                : 1f;

            cloud.IsManagedByConvergence = true;
            cloud.HeldConvergencePointId = point.PointId;
            cloud.ConvergenceIntensity = intensity;

            point.CurrentIntensity += intensity;
            point.HeldCloudCount++;

            var record = new HeldCloudRecord
            {
                CloudId = cloud.Id,
                PointId = point.PointId,
                Intensity = intensity
            };

            _heldByCloudId[cloud.Id] = record;

            if (!_heldByPointId.TryGetValue(point.PointId, out List<HeldCloudRecord> list))
            {
                list = new List<HeldCloudRecord>();
                _heldByPointId[point.PointId] = list;
            }

            list.Add(record);

            Debug.Log(
                $"[Convergence] Captured cloud #{cloud.Id} ({cloud.VisualCategory}) " +
                $"+{intensity:F1} intensity -> point #{point.PointId} total={point.CurrentIntensity:F1}");

            LogIntensityThresholdCheck(point);
        }

        public float GetPointIntensity(int pointId)
        {
            ConvergencePoint point = FindPointById(pointId);
            return point != null ? point.CurrentIntensity : 0f;
        }

        private void ProcessHeldClouds()
        {
            foreach (var point in _points)
            {
                LogIntensityThresholdCheck(point);

                if (!_heldByPointId.TryGetValue(point.PointId, out List<HeldCloudRecord> heldList)
                    || heldList.Count == 0)
                {
                    continue;
                }

                var snapshot = new List<HeldCloudRecord>(heldList);

                foreach (HeldCloudRecord record in snapshot)
                {
                    if (!_heldByCloudId.ContainsKey(record.CloudId))
                        continue;

                    if (_cloudManager == null
                        || !_cloudManager.TryGetCloud(record.CloudId, out CloudEntity cloud)
                        || cloud.State != CloudState.Held)
                    {
                        UnregisterHeldCloud(record);
                        continue;
                    }

                    if (Random.value < _config.DissipateChancePerCheck)
                    {
                        DissipateHeldCloud(record);
                        continue;
                    }

                    if (Random.value < _config.ReleaseChancePerCheck)
                        ReleaseHeldCloud(record);
                }
            }
        }

        private void ReleaseHeldCloud(HeldCloudRecord record)
        {
            UnregisterHeldCloud(record);

            if (_cloudManager != null)
                _cloudManager.ReleaseCloudFromConvergence(record.CloudId);

            Debug.Log($"[Convergence] Released cloud #{record.CloudId} from point #{record.PointId}");
        }

        private void DissipateHeldCloud(HeldCloudRecord record)
        {
            UnregisterHeldCloud(record);

            if (_cloudManager != null)
                _cloudManager.DissipateCloud(record.CloudId);

            Debug.Log($"[Convergence] Dissipated cloud #{record.CloudId} from point #{record.PointId}");
        }

        private void UnregisterHeldCloud(HeldCloudRecord record)
        {
            _heldByCloudId.Remove(record.CloudId);

            if (_heldByPointId.TryGetValue(record.PointId, out List<HeldCloudRecord> list))
            {
                list.Remove(record);
                if (list.Count == 0)
                    _heldByPointId.Remove(record.PointId);
            }

            ConvergencePoint point = FindPointById(record.PointId);
            if (point == null)
                return;

            point.CurrentIntensity = Mathf.Max(0f, point.CurrentIntensity - record.Intensity);
            point.HeldCloudCount = Mathf.Max(0, point.HeldCloudCount - 1);
        }

        private void ReleaseAllHeldCloudsForPoint(int pointId)
        {
            if (!_heldByPointId.TryGetValue(pointId, out List<HeldCloudRecord> list))
                return;

            var snapshot = new List<HeldCloudRecord>(list);
            foreach (HeldCloudRecord record in snapshot)
                ReleaseHeldCloud(record);
        }

        private void LogIntensityThresholdCheck(ConvergencePoint point)
        {
            float intensity = point.CurrentIntensity;

            Debug.Log(
                $"[Convergence] Point #{point.PointId} intensity={intensity:F1} " +
                $"held={point.HeldCloudCount} — TODO: evaluate release / storm / snow");

            if (intensity >= _config.StormIntensityThreshold)
            {
                Debug.Log(
                    $"[Convergence] TODO: intensity {intensity:F1} >= storm threshold " +
                    $"{_config.StormIntensityThreshold} — future storm formation check");
                return;
            }

            if (intensity >= _config.SnowIntensityThreshold)
            {
                Debug.Log(
                    $"[Convergence] TODO: intensity {intensity:F1} >= snow threshold " +
                    $"{_config.SnowIntensityThreshold} — future snow formation check");
                return;
            }

            if (intensity >= _config.ReleaseIntensityThreshold)
            {
                Debug.Log(
                    $"[Convergence] TODO: intensity {intensity:F1} >= release threshold " +
                    $"{_config.ReleaseIntensityThreshold} — future bulk release check");
            }
        }

        private ConvergencePoint FindPointById(int pointId)
        {
            foreach (ConvergencePoint point in _points)
            {
                if (point.PointId == pointId)
                    return point;
            }

            return null;
        }

        private void InitializePoints()
        {
            _points.Clear();

            int count = Mathf.Max(1, _initialPointCount);
            for (int i = 0; i < count; i++)
                SpawnPointAtChunkCenter();
        }

        private void SpawnPointAtChunkCenter()
        {
            Vector2 pos = GetSpawnPosition();
            _points.Add(CreatePoint(pos));
        }

        private Vector2 GetSpawnPosition()
        {
            ResolveMapStreamer();

            if (_mapStreamer != null &&
                _mapStreamer.TryGetChunkCenterXZ(_spawnChunkCoord, out Vector2 center))
            {
                return center;
            }

            return new Vector2(
                (_terrainMin.x + _terrainMax.x) * 0.5f,
                (_terrainMin.y + _terrainMax.y) * 0.5f);
        }

        private ConvergencePoint CreatePoint(Vector2 pos)
        {
            double now = _worldTime.TotalGameSeconds;

            double lifetime = Random.Range(
                _config.MinLifeTimehours,
                _config.MaxLifeTimehours) * SecondsPerHour;

            var point = new ConvergencePoint
            {
                PointId = _nextPointId++,
                Position = pos,
                SpawnGameSeconds = now,
                ExpireGameSeconds = now + lifetime,
                AttractionStrength = _config.attractionStrength
            };

            if (_convergenceVisualPrefab != null)
            {
                point.VisualObject = Instantiate(
                    _convergenceVisualPrefab,
                    new Vector3(pos.x, 0.1f, pos.y),
                    Quaternion.identity);
                point.VisualObject.Initialize(_visualRadius);
            }

            return point;
        }

        private void UpdateLifetime(double now)
        {
            bool removed = false;

            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (!_points[i].IsExpired(now))
                    continue;

                ReleaseAllHeldCloudsForPoint(_points[i].PointId);

                if (_points[i].VisualObject != null)
                    Destroy(_points[i].VisualObject.gameObject);

                _points.RemoveAt(i);
                removed = true;
            }

            if (!removed)
                return;

            int targetCount = Mathf.Max(1, _initialPointCount);
            while (_points.Count < targetCount)
                SpawnPointAtChunkCenter();
        }

        private void OnDrawGizmosSelected()
        {
            if (_points == null)
                return;

            Gizmos.color = Color.cyan;

            foreach (var p in _points)
            {
                Vector3 pos = new Vector3(p.Position.x, 0.1f, p.Position.y);
                Gizmos.DrawSphere(pos, _visualRadius);
            }
        }
    }
}
