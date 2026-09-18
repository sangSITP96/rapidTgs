using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.WorldTime;
using Game.Navigation;

namespace Game.Travel
{
    /// <summary>
    /// Phase 12 travel lifecycle + Phase 13 route consumption:
    /// March → Make Camp → Rest → Resume March → Arrival.
    /// Route march follows arc-length along the polyline (no look-ahead steering drift).
    /// </summary>
    [DefaultExecutionOrder(0)]
    public sealed class TroopTravelController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private TravelSettings _settings;

        [Header("Refs")]
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private TravelSurfaceQuery _surfaceQuery;
        [SerializeField] private TravelWeatherAdapter _weatherAdapter;
        [SerializeField] private Transform _mover;

        [Header("Runtime")]
        [SerializeField] private TravelRuntimeState _state = new TravelRuntimeState();

        private readonly List<Vector3> _routeWaypoints = new List<Vector3>();
        private readonly List<Vector3> _resampleBuffer = new List<Vector3>(128);
        private readonly List<float> _cumulativeLength = new List<float>(128);
        private float _pathDistance;
        private float _totalPathLength;

        public TravelSettings Settings => _settings;
        public TravelRuntimeState RuntimeState => _state;
        public TravelStatus Status => _state.Status;
        public bool HasActiveRoute => _state.HasRoute && _routeWaypoints.Count > 0;

        public bool IsDrivingMover => _state != null && _state.Status == TravelStatus.Marching;

        public event Action<TravelStatus, TravelStatus> StatusChanged;
        public event Action Arrived;
        public event Action StateUpdated;
        public event Action MarchControlStarted;

        private void Awake()
        {
            ResolveRefs();

            if (_mover == null)
                _mover = transform;

            _state.CurrentPosition = _mover.position;
            _state.Origin = _mover.position;
        }

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
            if (_surfaceQuery == null)
                _surfaceQuery = GetComponent<TravelSurfaceQuery>() ?? FindFirstObjectByType<TravelSurfaceQuery>();
            if (_weatherAdapter == null)
                _weatherAdapter = GetComponent<TravelWeatherAdapter>() ?? FindFirstObjectByType<TravelWeatherAdapter>();
        }

        public void BeginTravel(Vector3 destination)
        {
            var route = new NavigationRoute
            {
                IsValid = true,
                Origin = _mover != null ? _mover.position : _state.CurrentPosition,
                Destination = destination
            };
            route.Waypoints.Add(route.Origin);
            route.Waypoints.Add(destination);
            BeginTravel(route);
        }

        public void BeginTravel(NavigationRoute route)
        {
            if (_settings == null)
            {
                Debug.LogWarning($"{nameof(TroopTravelController)}: TravelSettings missing.");
                return;
            }

            if (route == null || !route.IsValid || route.Waypoints == null || route.Waypoints.Count == 0)
            {
                Debug.LogWarning($"{nameof(TroopTravelController)}: Invalid NavigationRoute.");
                return;
            }

            Vector3 start = _mover != null ? _mover.position : _state.CurrentPosition;

            _routeWaypoints.Clear();
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                Vector3 wp = route.Waypoints[i];
                wp.y = start.y;
                _routeWaypoints.Add(wp);
            }

            Vector3 finalDest = route.Destination;
            finalDest.y = start.y;
            if (_routeWaypoints.Count == 0 ||
                HorizontalDistance(_routeWaypoints[_routeWaypoints.Count - 1], finalDest) > 0.001f)
            {
                _routeWaypoints.Add(finalDest);
            }

            // Optional curve smoothing for stride corners. Uniform Catmull-Rom can overshoot;
            // keep samples dense so arc-length follow stays faithful to the approved path.
            if (_settings.ResampleRouteWithCatmullRom && _routeWaypoints.Count >= 3)
            {
                ResampleRouteCatmullRom(_routeWaypoints, _resampleBuffer, _settings.SmoothPathSampleSpacing);
                _routeWaypoints.Clear();
                _routeWaypoints.AddRange(_resampleBuffer);
            }

            RebuildCumulativeLengths();
            FindClosestPathDistance(start, out _pathDistance, out int startIndex);
            _pathDistance = Mathf.Clamp(_pathDistance, 0f, _totalPathLength);

            // Snap onto the path immediately so the first frames don't lerp from off-path.
            Vector3 onPath = SamplePathAtDistance(_pathDistance, out startIndex);
            onPath.y = start.y;
            ApplyPosition(onPath);

            _state.Origin = onPath;
            _state.Destination = _routeWaypoints[_routeWaypoints.Count - 1];
            _state.CurrentPosition = onPath;
            _state.HasDestination = true;
            _state.HasRoute = true;
            _state.RouteWaypointIndex = startIndex;
            _state.RouteWaypointCount = _routeWaypoints.Count;
            _state.RemainingDistance = Mathf.Max(0f, _totalPathLength - _pathDistance);

            SetStatus(TravelStatus.Marching);
            MarchControlStarted?.Invoke();
            RefreshSurfaceSample(onPath);
            Publish();
        }

        public void StopAndMakeCamp()
        {
            SetStatus(TravelStatus.Camped);
            Publish();
        }

        public void StartRest()
        {
            if (_state.Status != TravelStatus.Camped &&
                _state.Status != TravelStatus.Idle &&
                _state.Status != TravelStatus.Resting)
            {
                SetStatus(TravelStatus.Camped);
            }

            SetStatus(TravelStatus.Resting);
            Publish();
        }

        public void StopRest()
        {
            if (_state.Status == TravelStatus.Resting)
                SetStatus(TravelStatus.Camped);

            Publish();
        }

        public void ResumeMarch()
        {
            if (!_state.HasDestination)
            {
                Debug.LogWarning($"{nameof(TroopTravelController)}: No destination to resume.");
                return;
            }

            if (_state.Status == TravelStatus.Arrived)
            {
                Debug.Log($"{nameof(TroopTravelController)}: Already arrived.");
                return;
            }

            // Re-sync path distance from current world position after camping.
            if (_state.HasRoute && _routeWaypoints.Count > 1)
            {
                FindClosestPathDistance(
                    _mover != null ? _mover.position : _state.CurrentPosition,
                    out _pathDistance,
                    out int idx);
                _pathDistance = Mathf.Clamp(_pathDistance, 0f, _totalPathLength);
                _state.RouteWaypointIndex = idx;
                _state.RemainingDistance = Mathf.Max(0f, _totalPathLength - _pathDistance);
            }

            SetStatus(TravelStatus.Marching);
            MarchControlStarted?.Invoke();
            Publish();
        }

        public void CancelTravel()
        {
            _state.HasDestination = false;
            _state.HasRoute = false;
            _state.RouteWaypointIndex = 0;
            _state.RouteWaypointCount = 0;
            _state.RemainingDistance = 0f;
            _state.CurrentSpeed = 0f;
            _pathDistance = 0f;
            _totalPathLength = 0f;
            _routeWaypoints.Clear();
            _cumulativeLength.Clear();
            SetStatus(TravelStatus.Idle);
            Publish();
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double totalGameSeconds)
        {
            if (deltaGameSeconds <= 0d)
                return;

            UpdateDayPhase();

            switch (_state.Status)
            {
                case TravelStatus.Marching:
                    _state.MarchElapsedGameSeconds += deltaGameSeconds;
                    SimulateMarch(deltaGameSeconds);
                    break;

                case TravelStatus.Resting:
                    _state.RestElapsedGameSeconds += deltaGameSeconds;
                    _state.CampElapsedGameSeconds += deltaGameSeconds;
                    break;

                case TravelStatus.Camped:
                    _state.CampElapsedGameSeconds += deltaGameSeconds;
                    break;
            }

            Publish();
        }

        private void SimulateMarch(double deltaGameSeconds)
        {
            if (_settings == null || !_state.HasDestination)
                return;

            Vector3 current = _mover != null ? _mover.position : _state.CurrentPosition;
            RefreshSurfaceSample(current);

            if (!_state.IsPassable || _state.EffectiveMovementModifier <= 0f)
            {
                _state.CurrentSpeed = 0f;
                return;
            }

            float hours = (float)(deltaGameSeconds / WorldTime.SecondsPerHour);
            float speed = _settings.BaseMarchSpeedUnitsPerGameHour *
                          _state.EffectiveMovementModifier *
                          _state.WeatherMovementMultiplier;
            _state.CurrentSpeed = speed;

            float stepBudget = speed * hours;
            if (stepBudget <= 0f)
                return;

            if (_state.HasRoute && _routeWaypoints.Count > 0)
                SimulateMarchAlongRoute(current, stepBudget);
            else
                SimulateMarchStraightLine(current, stepBudget);
        }

        private void SimulateMarchAlongRoute(Vector3 current, float stepBudget)
        {
            if (_routeWaypoints.Count == 0 || _cumulativeLength.Count == 0)
                return;

            float remaining = Mathf.Max(0f, _totalPathLength - _pathDistance);

            // Finish only when this step actually reaches the end — no long-range teleport snap.
            if (remaining <= 0.001f || stepBudget >= remaining)
            {
                _pathDistance = _totalPathLength;
                Vector3 end = _state.Destination;
                end.y = current.y;
                ApplyPosition(end);
                _state.RouteWaypointIndex = Mathf.Max(0, _routeWaypoints.Count - 1);
                _state.RemainingDistance = 0f;
                CompleteArrival();
                return;
            }

            float previousDistance = _pathDistance;
            _pathDistance += stepBudget;
            if (_pathDistance > _totalPathLength)
                _pathDistance = _totalPathLength;

            Vector3 next = SamplePathAtDistance(_pathDistance, out int segmentIndex);
            next.y = current.y;

            TravelSurfaceSample surface = SampleSurface(next);
            if (!surface.IsPassable)
            {
                // Lake pocket on an otherwise valid route: skip forward along the path to the
                // next passable sample instead of freezing forever on a mixed-biome cell edge.
                if (TrySkipImpassablePathSegment(current.y, previousDistance, stepBudget, out Vector3 skipTo, out int skipSegment, out float skipDistance))
                {
                    ApplyPosition(skipTo);
                    _pathDistance = skipDistance;
                    _state.RouteWaypointIndex = skipSegment;
                    _state.RemainingDistance = Mathf.Max(0f, _totalPathLength - _pathDistance);
                    return;
                }

                _pathDistance = previousDistance;
                _state.CurrentSpeed = 0f;
                _state.RemainingDistance = remaining;
                return;
            }

            ApplyPosition(next);
            _state.RouteWaypointIndex = segmentIndex;
            _state.RemainingDistance = Mathf.Max(0f, _totalPathLength - _pathDistance);
        }

        private bool TrySkipImpassablePathSegment(
            float y,
            float fromDistance,
            float minSkip,
            out Vector3 landPos,
            out int segmentIndex,
            out float landDistance)
        {
            landPos = default;
            segmentIndex = 0;
            landDistance = fromDistance;

            float probe = fromDistance + Mathf.Max(minSkip, 0.05f);
            float maxProbe = Mathf.Min(_totalPathLength, fromDistance + 1.5f);

            while (probe <= maxProbe + 0.0001f)
            {
                Vector3 sample = SamplePathAtDistance(probe, out segmentIndex);
                sample.y = y;
                TravelSurfaceSample surface = SampleSurface(sample);
                if (surface.IsPassable)
                {
                    landPos = sample;
                    landDistance = probe;
                    return true;
                }

                probe += 0.05f;
            }

            return false;
        }

        private void SimulateMarchStraightLine(Vector3 current, float step)
        {
            Vector3 toDest = Flat(_state.Destination) - Flat(current);
            float remaining = toDest.magnitude;
            _state.RemainingDistance = remaining;

            if (remaining <= 0.001f || step >= remaining)
            {
                ApplyPosition(_state.Destination);
                CompleteArrival();
                return;
            }

            Vector3 next = current + toDest.normalized * step;
            next.y = current.y;

            TravelSurfaceSample lookAhead = SampleSurface(next);
            if (!lookAhead.IsPassable)
            {
                _state.CurrentSpeed = 0f;
                return;
            }

            ApplyPosition(next);
            _state.RemainingDistance = HorizontalDistance(next, _state.Destination);
        }

        private void CompleteArrival()
        {
            _state.RemainingDistance = 0f;
            _state.CurrentSpeed = 0f;
            _state.RouteWaypointIndex = Mathf.Max(0, _routeWaypoints.Count - 1);
            SetStatus(TravelStatus.Arrived);
            Arrived?.Invoke();
        }

        private void RebuildCumulativeLengths()
        {
            _cumulativeLength.Clear();
            _totalPathLength = 0f;
            _pathDistance = 0f;

            if (_routeWaypoints.Count == 0)
                return;

            _cumulativeLength.Add(0f);
            for (int i = 1; i < _routeWaypoints.Count; i++)
            {
                _totalPathLength += HorizontalDistance(_routeWaypoints[i - 1], _routeWaypoints[i]);
                _cumulativeLength.Add(_totalPathLength);
            }
        }

        private void FindClosestPathDistance(Vector3 worldPos, out float distanceAlongPath, out int segmentIndex)
        {
            distanceAlongPath = 0f;
            segmentIndex = 0;

            if (_routeWaypoints.Count <= 1 || _cumulativeLength.Count < 2)
                return;

            Vector3 p = Flat(worldPos);
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < _routeWaypoints.Count - 1; i++)
            {
                Vector3 a = Flat(_routeWaypoints[i]);
                Vector3 b = Flat(_routeWaypoints[i + 1]);
                Vector3 ab = b - a;
                float abLenSq = ab.sqrMagnitude;
                float t = abLenSq > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / abLenSq) : 0f;
                Vector3 closest = a + ab * t;
                float dSq = (p - closest).sqrMagnitude;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    segmentIndex = i;
                    float segStart = _cumulativeLength[i];
                    float segLen = _cumulativeLength[i + 1] - segStart;
                    distanceAlongPath = segStart + segLen * t;
                }
            }
        }

        private Vector3 SamplePathAtDistance(float distance, out int segmentIndex)
        {
            segmentIndex = 0;
            if (_routeWaypoints.Count == 0)
                return Vector3.zero;

            if (_routeWaypoints.Count == 1 || distance <= 0f)
                return _routeWaypoints[0];

            if (distance >= _totalPathLength)
            {
                segmentIndex = _routeWaypoints.Count - 1;
                return _routeWaypoints[segmentIndex];
            }

            int lo = 0;
            int hi = _cumulativeLength.Count - 1;
            while (lo < hi - 1)
            {
                int mid = (lo + hi) >> 1;
                if (_cumulativeLength[mid] <= distance)
                    lo = mid;
                else
                    hi = mid;
            }

            segmentIndex = lo;
            float start = _cumulativeLength[lo];
            float end = _cumulativeLength[lo + 1];
            float segLen = end - start;
            float t = segLen > 1e-8f ? (distance - start) / segLen : 0f;
            return Vector3.Lerp(_routeWaypoints[lo], _routeWaypoints[lo + 1], t);
        }

        private static void ResampleRouteCatmullRom(List<Vector3> source, List<Vector3> destination, float spacing)
        {
            destination.Clear();
            if (source == null || source.Count == 0)
                return;

            if (source.Count == 1)
            {
                destination.Add(source[0]);
                return;
            }

            spacing = Mathf.Max(0.02f, spacing);
            destination.Add(source[0]);

            for (int i = 0; i < source.Count - 1; i++)
            {
                Vector3 p0 = source[Mathf.Max(i - 1, 0)];
                Vector3 p1 = source[i];
                Vector3 p2 = source[i + 1];
                Vector3 p3 = source[Mathf.Min(i + 2, source.Count - 1)];

                float segLen = HorizontalDistance(p1, p2);
                int steps = Mathf.Max(1, Mathf.CeilToInt(segLen / spacing));

                for (int s = 1; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                    point.y = p1.y;

                    if (destination.Count > 0 &&
                        HorizontalDistance(destination[destination.Count - 1], point) < spacing * 0.35f &&
                        s < steps)
                    {
                        continue;
                    }

                    destination.Add(point);
                }
            }

            Vector3 last = source[source.Count - 1];
            if (destination.Count == 0 || HorizontalDistance(destination[destination.Count - 1], last) > 0.001f)
                destination.Add(last);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private void RefreshSurfaceSample(Vector3 worldPos)
        {
            TravelSurfaceSample sample = SampleSurface(worldPos);
            _state.CurrentSurface = sample.SurfaceType;
            _state.IsPassable = sample.IsPassable;
            _state.IsOnTrail = sample.IsTrail;
            _state.EffectiveMovementModifier = sample.MovementModifier;
            _state.CurrentSurfaceNeedsModifier = sample.NeedsModifier;

            _state.WeatherMovementMultiplier = _weatherAdapter != null
                ? _weatherAdapter.GetMovementMultiplier()
                : 1f;
            _state.WeatherNeedsMultiplier = _weatherAdapter != null
                ? _weatherAdapter.GetNeedsMultiplier()
                : 1f;
        }

        private TravelSurfaceSample SampleSurface(Vector3 worldPos)
        {
            if (_surfaceQuery != null)
                return _surfaceQuery.Sample(worldPos);

            return new TravelSurfaceSample
            {
                SurfaceType = TravelSurfaceType.Grassland,
                IsPassable = true,
                MovementModifier = 1f,
                NeedsModifier = 1f,
                IsTrail = false,
                WorldPosition = worldPos
            };
        }

        private void UpdateDayPhase()
        {
            if (_worldTime == null)
                return;

            _state.CurrentDayPhase = _worldTime.GetDayPhase();
            _state.IsNight = _worldTime.IsNight();
        }

        private void ApplyPosition(Vector3 worldPos)
        {
            _state.CurrentPosition = worldPos;
            if (_mover != null)
                _mover.position = worldPos;
        }

        private void SetStatus(TravelStatus next)
        {
            if (_state.Status == next)
                return;

            TravelStatus previous = _state.Status;
            _state.Status = next;
            StatusChanged?.Invoke(previous, next);
        }

        private void Publish()
        {
            if (_mover != null)
                _state.CurrentPosition = _mover.position;

            StateUpdated?.Invoke();
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(Flat(a), Flat(b));
        }
    }
}
