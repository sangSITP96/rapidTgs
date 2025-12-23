using System;
using System.Collections.Generic;
using Game.Core.WorldTime;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Weather.Convergence
{
    public class ConvergenceManager : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private ConvergenceConfig _config;
        
        [Header("Terrain Bounds (World XZ)")]
        [SerializeField] private Vector2 _terrainMin;
        [SerializeField] private Vector2 _terrainMax;

        public IReadOnlyList<ConvergencePoint> ActivePoints => _points;
        
        private readonly List<ConvergencePoint> _points = new();

        private double _nextDriftRerollAt;
        private const double SecondsPerHour = 3600.0;

        private void OnEnable()
        {
            if (_worldTime == null)
            {
                _worldTime = FindFirstObjectByType<WorldTime>();
            }

            if (_worldTime == null || _config == null)
            {
                Debug.LogError("No WorldTime or Config object found");
                enabled = false;
                return;
            }

            _worldTime.OnTimeAdvanced += HandleTimeAdvance;
            
            InitializePoints();
            ScheduleNextDriftReroll(_worldTime.TotalGameSeconds);
        }

        private void OnDisable()
        {
            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced -= HandleTimeAdvance;
            }
        }

        private void HandleTimeAdvance(double deltaGameSeconds, double now)
        {
            UpdateDrift(deltaGameSeconds);
            UpdateLifetime(now);
        }
        
        //

        private void InitializePoints()
        {
            _points.Clear();

            int targetCount = Random.Range(_config.MinPoints, _config.MaxPoints + 1);

            for (int i = 0; i < targetCount; i++)
            {
                TrySpawnpoint();
            }
        }

        private void TrySpawnpoint()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 pos = RandomPositionInBounds();
                if (IsFarEnough(pos))
                {
                    _points.Add(CreatePoint(pos));
                    return;
                }
            }
        }

        private ConvergencePoint CreatePoint(Vector2 pos)
        {
            double now = _worldTime.TotalGameSeconds;

            double lifetime = Random.Range(
                _config.MinLifeTimehours, 
                _config.MaxLifeTimehours) * SecondsPerHour;

            return new ConvergencePoint
            {
                Position = pos,
                DriftDirection = Random.insideUnitCircle.normalized,
                SpawnGameSeconds = now,
                ExpireGameSeconds = now + lifetime,
                AttractionStrength = _config.attractionStrength
            };
        }

        private bool IsFarEnough(Vector2 pos)
        {
            foreach (var p in _points)
            {
                if (Vector2.Distance(p.Position, pos) < _config.MinSeparation)
                    return false;
            }

            return true;
        }

        private Vector2 RandomPositionInBounds()
        {
            return new Vector2(
                Random.Range(_terrainMin.x, _terrainMax.x),
                Random.Range(_terrainMin.y, _terrainMax.y)
            );
        }
        
        // Drift
        private void UpdateDrift(double deltaGameSeconds)
        {
            float deltaHours = (float)(deltaGameSeconds / SecondsPerHour);

            foreach (var p in _points)
            {
                p.Position += p.DriftDirection*_config.DriftSpeed*deltaHours;
            }

            ClampToBounds();
        }

        private void ScheduleNextDriftReroll(double now)
        {
            _nextDriftRerollAt = now + _config.DriftRerollHours * SecondsPerHour;
        }

        private void RerollDriftDirections()
        {
            foreach (var p in _points)
            {
                p.DriftDirection = Random.insideUnitCircle.normalized;
            }
        }
        
        // Lifetime
        private void UpdateLifetime(double now)
        {
            bool removed = false;
            for (int i = _points.Count - 1; i >= 0; i--)
            {
                if (_points[i].IsExpired(now))
                {
                    _points.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                while (_points.Count < _config.MinPoints)
                {
                    TrySpawnpoint();
                }
            }

            if (now >= _nextDriftRerollAt)
            {
                RerollDriftDirections();
                ScheduleNextDriftReroll(now);
            }
        }

        // Bounds
        private void ClampToBounds()
        {
            foreach (var p in _points)
            {
                p.Position = new Vector2(
                    Mathf.Clamp(p.Position.x, _terrainMin.x, _terrainMax.x),
                    Mathf.Clamp(p.Position.y, _terrainMin.y, _terrainMax.y)
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (_points == null) return;

            Gizmos.color = Color.cyan;

            foreach (var p in _points)
            {
                Vector3 pos = new Vector3(p.Position.x, 0.1f, p.Position.y);
                
                Gizmos.DrawSphere(pos, 1.5f);
                Vector3 dir = new Vector3(p.DriftDirection.x,0,p.DriftDirection.y);
                Gizmos.DrawLine(pos, pos + dir*5f);
            }
        }
    }
}

