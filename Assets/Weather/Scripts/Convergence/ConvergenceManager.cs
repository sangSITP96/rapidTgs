using System.Collections.Generic;
using Game.Core.WorldTime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Weather.Convergence
{
    [DefaultExecutionOrder(20)]
    public class ConvergenceManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private ConvergenceConfig _config;

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

        private const double SecondsPerHour = 3600.0;

        private void OnEnable()
        {
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();

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
