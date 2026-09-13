using System;
using UnityEngine;
using Game.Core.WorldTime;

namespace Game.Travel
{
    /// <summary>
    /// Phase 12 travel lifecycle bones:
    /// March → Make Camp → Rest → Resume March → Arrival.
    /// Night does not auto-stop marching. No route/auto-camp (Phase 13).
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

        public TravelSettings Settings => _settings;
        public TravelRuntimeState RuntimeState => _state;
        public TravelStatus Status => _state.Status;

        /// <summary>
        /// True while this controller is writing the mover transform.
        /// Used to avoid fighting MarbleMovement click-to-move.
        /// </summary>
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
            if (_settings == null)
            {
                Debug.LogWarning($"{nameof(TroopTravelController)}: TravelSettings missing.");
                return;
            }

            Vector3 start = _mover != null ? _mover.position : _state.CurrentPosition;
            _state.Origin = start;
            _state.Destination = destination;
            _state.CurrentPosition = start;
            _state.HasDestination = true;
            _state.RemainingDistance = HorizontalDistance(start, destination);

            SetStatus(TravelStatus.Marching);
            MarchControlStarted?.Invoke();
            RefreshSurfaceSample(start);
            Publish();
        }

        public void StopAndMakeCamp()
        {
            // Manual camp is always allowed in Phase 12 (including from Resting → Camped).
            SetStatus(TravelStatus.Camped);
            Publish();
        }

        public void StartRest()
        {
            if (_state.Status != TravelStatus.Camped &&
                _state.Status != TravelStatus.Idle &&
                _state.Status != TravelStatus.Resting)
            {
                // Manual camp first is preferred, but allow rest from idle for prototype.
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

            SetStatus(TravelStatus.Marching);
            MarchControlStarted?.Invoke();
            Publish();
        }

        public void CancelTravel()
        {
            _state.HasDestination = false;
            _state.RemainingDistance = 0f;
            _state.CurrentSpeed = 0f;
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
                // Blocked — stay put, keep Marching so player can camp/reroute later.
                _state.CurrentSpeed = 0f;
                return;
            }

            float hours = (float)(deltaGameSeconds / WorldTime.SecondsPerHour);
            float speed = _settings.BaseMarchSpeedUnitsPerGameHour *
                          _state.EffectiveMovementModifier *
                          _state.WeatherMovementMultiplier;
            _state.CurrentSpeed = speed;

            float step = speed * hours;
            Vector3 toDest = Flat(_state.Destination) - Flat(current);
            float remaining = toDest.magnitude;
            _state.RemainingDistance = remaining;

            if (remaining <= _settings.ArrivalDistanceThreshold || step >= remaining)
            {
                ApplyPosition(_state.Destination);
                _state.RemainingDistance = 0f;
                _state.CurrentSpeed = 0f;
                SetStatus(TravelStatus.Arrived);
                Arrived?.Invoke();
                return;
            }

            Vector3 next = current + toDest.normalized * step;
            next.y = current.y;

            // Look-ahead block check
            TravelSurfaceSample lookAhead = SampleSurface(next);
            if (!lookAhead.IsPassable)
            {
                _state.CurrentSpeed = 0f;
                return;
            }

            ApplyPosition(next);
            _state.RemainingDistance = HorizontalDistance(next, _state.Destination);
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
            // Intentionally do NOT auto-stop march at night.
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
