using System;
using System.Collections.Generic;
using Game.Core.WorldTime;
using UnityEngine;

namespace Game.Seasons
{
    [DefaultExecutionOrder(-50)]
    public sealed class SeasonSystem : MonoBehaviour
    {
        public static SeasonSystem Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private SeasonCalendarConfig _calendar;
        [SerializeField] private SeasonDefinition _spring;
        [SerializeField] private SeasonDefinition _summer;
        [SerializeField] private SeasonDefinition _autumn;
        [SerializeField] private SeasonDefinition _winter;

        [Header("Runtime")]
        [SerializeField] private bool _logSeasonChanges = true;

        private readonly Dictionary<SeasonId, SeasonDefinition> _runtimeFallback =
            new Dictionary<SeasonId, SeasonDefinition>();

        private SeasonId _currentSeason = SeasonId.Spring;
        private int _absoluteSeasonIndex = -1;
        private int _dayInSeason;
        private float _seasonProgress01;
        private double _seasonStartGameSeconds;
        private double _seasonEndGameSeconds;
        private float _transitionDurationHours = 6f;
        private bool _hasForcedSeason;
        private SeasonId _forcedSeason;

        public SeasonCalendarConfig Calendar => _calendar;
        public WorldTime WorldTime => _worldTime;

        public SeasonId CurrentSeason => _hasForcedSeason ? _forcedSeason : _currentSeason;
        public int AbsoluteSeasonIndex => _absoluteSeasonIndex;
        public int DayInSeason => _dayInSeason;
        public float SeasonProgress01 => _seasonProgress01;
        public double SeasonStartGameSeconds => _seasonStartGameSeconds;
        public double SeasonEndGameSeconds => _seasonEndGameSeconds;
        public bool IsTransitioning { get; private set; }
        public float TransitionProgress01 { get; private set; }
        public SeasonId PreviousSeason { get; private set; } = SeasonId.Winter;
        public bool HasForcedSeason => _hasForcedSeason;
        public int DaysPerSeason => _calendar != null ? Mathf.Max(1, _calendar.DaysPerSeason) : 7;
        public SeasonId StartingSeason => _calendar != null ? _calendar.StartingSeason : SeasonId.Spring;

        public float CurrentTemperatureF { get; private set; }
        public TemperatureUnit DisplayTemperatureUnit =>
            _calendar != null ? _calendar.DisplayTemperatureUnit : TemperatureUnit.Fahrenheit;

        public event Action<SeasonId, SeasonId> OnSeasonChanged;
        public event Action<float> OnSeasonTransitionProgress;
        public event Action OnSeasonVisualHook;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{nameof(SeasonSystem)}: duplicate instance on {name}.");
            }

            Instance = this;

            if (_worldTime == null)
                Debug.LogError($"{nameof(SeasonSystem)}: assign {nameof(WorldTime)} on the prefab/scene instance.");

            if (_calendar == null)
                Debug.LogError($"{nameof(SeasonSystem)}: assign {nameof(SeasonCalendarConfig)} on the prefab.");

            SyncTimeConfigYearLength();
            EnsureDefinitions();
            RefreshFromWorldTime(forceEvents: false);
        }

        private void OnEnable()
        {
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
        }

        private void OnDisable()
        {
            if (_worldTime != null)
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            foreach (var pair in _runtimeFallback)
            {
                if (pair.Value != null)
                    Destroy(pair.Value);
            }

            _runtimeFallback.Clear();
        }

        private void HandleTimeAdvanced(double delta, double total)
        {
            RefreshFromWorldTime(forceEvents: true);
        }

        public void RefreshFromWorldTime(bool forceEvents)
        {
            if (_worldTime == null)
                return;

            double total = _worldTime.TotalGameSeconds;
            SeasonCalendarMath.Resolve(
                total,
                DaysPerSeason,
                StartingSeason,
                out SeasonId resolvedSeason,
                out int absoluteIndex,
                out int dayInSeason,
                out float progress,
                out double seasonStart,
                out double seasonEnd);

            if (_hasForcedSeason)
                resolvedSeason = _forcedSeason;

            _dayInSeason = dayInSeason;
            _seasonProgress01 = progress;
            _seasonStartGameSeconds = seasonStart;
            _seasonEndGameSeconds = seasonEnd;

            bool seasonChanged = absoluteIndex != _absoluteSeasonIndex ||
                                 (!_hasForcedSeason && resolvedSeason != _currentSeason);

            if (seasonChanged)
            {
                PreviousSeason = _currentSeason;
                _currentSeason = resolvedSeason;
                _absoluteSeasonIndex = absoluteIndex;
                RollTransitionDuration();
                UpdateTransition(total);

                if (forceEvents)
                {
                    OnSeasonChanged?.Invoke(PreviousSeason, CurrentSeason);
                    OnSeasonVisualHook?.Invoke();
                    if (_logSeasonChanges)
                    {
                        ColonyEventLogService.Instance?.AddSimple(
                            EventCategory.System,
                            "Season Changed",
                            $"{PreviousSeason} → {CurrentSeason}");
                    }
                }
            }
            else
            {
                UpdateTransition(total);
            }

            SeasonDefinition def = GetDefinition(CurrentSeason);
            float noise = Mathf.PerlinNoise((float)(total * 0.00001d), (int)CurrentSeason * 12.3f);
            CurrentTemperatureF = def != null
                ? def.EvaluateTemperatureF(_seasonProgress01, noise)
                : 70f;
        }

        private void UpdateTransition(double totalGameSeconds)
        {
            double elapsed = totalGameSeconds - _seasonStartGameSeconds;
            double transitionSeconds = Math.Max(0d, _transitionDurationHours * 3600d);
            if (transitionSeconds <= 0d)
            {
                IsTransitioning = false;
                TransitionProgress01 = 1f;
                return;
            }

            if (elapsed < transitionSeconds)
            {
                IsTransitioning = true;
                TransitionProgress01 = (float)(elapsed / transitionSeconds);
                OnSeasonTransitionProgress?.Invoke(TransitionProgress01);
            }
            else
            {
                IsTransitioning = false;
                TransitionProgress01 = 1f;
            }
        }

        private void RollTransitionDuration()
        {
            SeasonDefinition def = GetDefinition(CurrentSeason);
            if (def == null)
            {
                _transitionDurationHours = 6f;
                return;
            }

            _transitionDurationHours = UnityEngine.Random.Range(
                def.TransitionMinHours,
                def.TransitionMaxHours);
        }

        public SeasonDefinition GetDefinition(SeasonId season)
        {
            SeasonDefinition assigned = season switch
            {
                SeasonId.Spring => _spring,
                SeasonId.Summer => _summer,
                SeasonId.Autumn => _autumn,
                _ => _winter
            };

            if (assigned != null)
                return assigned;

            if (!_runtimeFallback.TryGetValue(season, out SeasonDefinition created) || created == null)
            {
                created = SeasonDefinitionFactory.CreateRuntime(season);
                _runtimeFallback[season] = created;
            }

            return created;
        }

        public SeasonDefinition GetCurrentDefinition() => GetDefinition(CurrentSeason);

        public bool IsWeatherAllowed(SeasonWeatherKind kind)
        {
            SeasonDefinition def = GetCurrentDefinition();
            return def != null && def.IsAllowed(kind);
        }

        public string GetCurrentWeekdayName()
        {
            if (_worldTime == null || _calendar == null)
                return "Monday";
            return _calendar.GetWeekdayName(_worldTime.GetDayIndex());
        }

        public float GetDisplayTemperature()
        {
            return SeasonTemperatureUtility.Convert(
                CurrentTemperatureF,
                TemperatureUnit.Fahrenheit,
                DisplayTemperatureUnit);
        }

        public string GetDisplayTemperatureText()
        {
            return SeasonTemperatureUtility.Format(CurrentTemperatureF, DisplayTemperatureUnit, "0.0");
        }

        public void ForceSeason(SeasonId season)
        {
            JumpToSeasonStart(season);
        }

        public void ClearForcedSeason()
        {
            _hasForcedSeason = false;
            RefreshFromWorldTime(forceEvents: true);
        }

        public void JumpToSeasonStart(SeasonId season)
        {
            if (_worldTime == null)
                return;

            _hasForcedSeason = false;
            PreviousSeason = _currentSeason;
            double target = SeasonCalendarMath.GetSeasonStartGameSeconds(
                season,
                DaysPerSeason,
                StartingSeason,
                _worldTime.GetDayIndex());
            _worldTime.SetTotalGameSeconds(target);
            RefreshFromWorldTime(forceEvents: false);
            OnSeasonChanged?.Invoke(PreviousSeason, CurrentSeason);
            OnSeasonVisualHook?.Invoke();
            if (_logSeasonChanges)
            {
                ColonyEventLogService.Instance?.AddSimple(
                    EventCategory.System,
                    "Season Changed",
                    $"{PreviousSeason} → {CurrentSeason} (jump)");
            }
        }

        public void SetCalendar(SeasonCalendarConfig calendar)
        {
            if (calendar == null)
                return;
            _calendar = calendar;
            SyncTimeConfigYearLength();
            RefreshFromWorldTime(forceEvents: false);
        }

        public void AdvanceGameHours(float hours)
        {
            if (_worldTime == null || hours <= 0f)
                return;
            _worldTime.Advance(hours * 3600d);
        }

        public void AdvanceGameDays(float days)
        {
            if (_worldTime == null || days <= 0f)
                return;
            _worldTime.Advance(days * SeasonCalendarMath.SecondsPerDay);
        }

        private void EnsureDefinitions()
        {
        }

        private void SyncTimeConfigYearLength()
        {
            if (_worldTime == null || _worldTime.Config == null || _calendar == null)
                return;

            int yearDays = _calendar.EffectiveDaysPerYear;
            if (_worldTime.Config.GameDaysPerYear != yearDays)
                _worldTime.Config.GameDaysPerYear = yearDays;

            _worldTime.Config.StartingSeasonIndex = (int)_calendar.StartingSeason;
        }

        public bool TryGetDaylightSeason(out int seasonIndex, out float blendToNext01)
        {
            seasonIndex = (int)CurrentSeason;
            if (IsTransitioning)
            {
                blendToNext01 = 0f;
                return true;
            }

            float t = Mathf.InverseLerp(0.85f, 1f, _seasonProgress01);
            blendToNext01 = Mathf.Clamp01(t);
            return true;
        }
    }
}
