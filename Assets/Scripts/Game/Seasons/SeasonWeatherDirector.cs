using System;
using System.Collections.Generic;
using Game.Core.WeatherGate;
using Game.Core.WorldTime;
using Game.Weather.Cloud;
using Game.Weather.Fog;
using Game.Weather.Rain;
using Game.Weather.Storm;
using UnityEngine;

namespace Game.Seasons
{
    [DefaultExecutionOrder(-40)]
    public sealed class SeasonWeatherDirector : MonoBehaviour, ISeasonWeatherGate
    {
        public static SeasonWeatherDirector Instance { get; private set; }

        [SerializeField] private SeasonSystem _seasonSystem;
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private WeatherManager _weatherManager;
        [SerializeField] private SeasonWeatherMapping _mapping;

        [Header("VFX (optional — reused existing managers)")]
        [SerializeField] private FogManager _fogManager;
        [SerializeField] private CloudManager _cloudManager;
        [SerializeField] private StormLifecycleManager _stormLifecycle;
        [SerializeField] private CloudEvolutionManager _cloudEvolution;

        [Header("Thunderstorm VFX")]
        [SerializeField] private GameObject _thunderstormVfxPrefab;
        [SerializeField] private Transform _thunderstormVisualParent;
        [SerializeField] private float _thunderstormVisualHeight = 5f;

        [Header("Logging")]
        [SerializeField] private bool _logWeatherChanges = true;

        private readonly SeasonWeatherScheduler _scheduler = new SeasonWeatherScheduler();
        private readonly List<SeasonWeatherScheduleEntry> _schedule = new List<SeasonWeatherScheduleEntry>();

        private SeasonWeatherKind _currentKind = SeasonWeatherKind.Clear;
        private SeasonWeatherScheduleEntry _activeEntry;
        private SeasonWeatherScheduleEntry _debugOverride;
        private int _builtForAbsoluteSeason = int.MinValue;
        private bool _suppressStormEventLog;
        private GameObject _activeThunderstormVfx;

        public SeasonWeatherKind CurrentWeatherKind => _currentKind;
        public SeasonWeatherScheduleEntry ActiveEntry => _activeEntry;
        public IReadOnlyList<SeasonWeatherScheduleEntry> Schedule => _schedule;

        public bool CanSpawnFog =>
            _currentKind == SeasonWeatherKind.Fog &&
            _seasonSystem != null &&
            _seasonSystem.IsWeatherAllowed(SeasonWeatherKind.Fog);

        public bool CanSpawnAmbientClouds =>
            _currentKind == SeasonWeatherKind.PartlyCloudy ||
            _currentKind == SeasonWeatherKind.Clear ||
            _currentKind == SeasonWeatherKind.Snow ||
            _currentKind == SeasonWeatherKind.SteadyRain ||
            _currentKind == SeasonWeatherKind.Thunderstorm ||
            _currentKind == SeasonWeatherKind.Snowstorm;

        public bool CanEvolveRain =>
            _currentKind == SeasonWeatherKind.SteadyRain ||
            _currentKind == SeasonWeatherKind.Thunderstorm ||
            _currentKind == SeasonWeatherKind.HeavyRain;

        public bool CanFormStorm =>
            _currentKind == SeasonWeatherKind.Thunderstorm &&
            _seasonSystem != null &&
            _seasonSystem.IsWeatherAllowed(SeasonWeatherKind.Thunderstorm);

        public bool CanShowSnowVfx =>
            _currentKind == SeasonWeatherKind.Snow ||
            _currentKind == SeasonWeatherKind.Snowstorm;

        public bool ShouldSuppressStormEventLog => _suppressStormEventLog;

        public event Action<SeasonWeatherKind, SeasonWeatherKind> OnWeatherKindChanged;

        private void Awake()
        {
            Instance = this;
            SeasonWeatherGate.Current = this;

            if (_seasonSystem == null || _worldTime == null || _weatherManager == null)
            {
                Debug.LogError(
                    $"{nameof(SeasonWeatherDirector)}: assign SeasonSystem, WorldTime, and WeatherManager on the prefab/scene instance.");
            }
        }

        private void OnEnable()
        {
            if (_seasonSystem != null)
                _seasonSystem.OnSeasonChanged += HandleSeasonChanged;

            if (_worldTime != null)
                _worldTime.OnTimeAdvanced += HandleTimeAdvanced;

            RebuildScheduleIfNeeded(force: true);
            ApplyAt(_worldTime != null ? _worldTime.TotalGameSeconds : 0d, force: true);
        }

        private void OnDisable()
        {
            if (_seasonSystem != null)
                _seasonSystem.OnSeasonChanged -= HandleSeasonChanged;

            if (_worldTime != null)
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;

            ClearThunderstormVfx();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (SeasonWeatherGate.Current == (ISeasonWeatherGate)this)
                SeasonWeatherGate.Current = null;
        }

        private void HandleSeasonChanged(SeasonId previous, SeasonId next)
        {
            RebuildScheduleIfNeeded(force: true);
            ApplyAt(_worldTime != null ? _worldTime.TotalGameSeconds : 0d, force: true);
        }

        private void HandleTimeAdvanced(double delta, double total)
        {
            RebuildScheduleIfNeeded(force: false);
            ApplyAt(total, force: false);
        }

        public void RebuildScheduleIfNeeded(bool force)
        {
            if (_seasonSystem == null)
                return;

            int absolute = _seasonSystem.AbsoluteSeasonIndex;
            if (!force && absolute == _builtForAbsoluteSeason && _schedule.Count > 0)
                return;

            SeasonDefinition def = _seasonSystem.GetCurrentDefinition();
            _schedule.Clear();
            _schedule.AddRange(_scheduler.BuildSchedule(
                def,
                _seasonSystem.Calendar,
                _seasonSystem.SeasonStartGameSeconds,
                _seasonSystem.SeasonEndGameSeconds,
                absolute));
            _builtForAbsoluteSeason = absolute;
        }

        public void ApplyAt(double totalGameSeconds, bool force)
        {
            SeasonWeatherScheduleEntry entry;
            if (_debugOverride != null && _debugOverride.Contains(totalGameSeconds))
            {
                entry = _debugOverride;
            }
            else
            {
                _debugOverride = null;
                entry = FindEntryAt(totalGameSeconds);
            }

            SeasonWeatherKind kind = entry != null ? entry.Kind : SeasonWeatherKind.Clear;

            if (kind == SeasonWeatherKind.MightyStorm)
                kind = SeasonWeatherKind.PartlyCloudy;

            if (_seasonSystem != null && !_seasonSystem.IsWeatherAllowed(kind) &&
                kind != SeasonWeatherKind.Clear && kind != SeasonWeatherKind.PartlyCloudy)
            {
                kind = SeasonWeatherKind.Clear;
                entry = null;
            }

            if (!force && kind == _currentKind && entry == _activeEntry)
                return;

            SeasonWeatherKind previous = _currentKind;
            bool kindChanged = kind != _currentKind;

            if (kindChanged && _activeEntry != null && _logWeatherChanges)
            {
                ColonyEventLogService.Instance?.AddSimple(
                    EventCategory.Weather,
                    "Weather Ended",
                    $"{previous} ended");
            }

            _currentKind = kind;
            _activeEntry = entry;

            ApplyGameplayWeather(kind);
            ApplyVfx(kind, previous, kindChanged);

            if (kindChanged)
            {
                OnWeatherKindChanged?.Invoke(previous, kind);
                if (_logWeatherChanges && kind != SeasonWeatherKind.Clear && kind != SeasonWeatherKind.PartlyCloudy)
                {
                    string duration = entry != null
                        ? $" for {entry.DurationGameSeconds / 3600d:0.0}h"
                        : "";
                    ColonyEventLogService.Instance?.AddSimple(
                        EventCategory.Weather,
                        "Weather Started",
                        $"{kind}{duration}");
                }
            }
        }

        private SeasonWeatherScheduleEntry FindEntryAt(double total)
        {
            SeasonWeatherScheduleEntry ambient = null;
            for (int i = 0; i < _schedule.Count; i++)
            {
                SeasonWeatherScheduleEntry e = _schedule[i];
                if (e == null || !e.Contains(total))
                    continue;

                if (e.Kind == SeasonWeatherKind.Clear || e.Kind == SeasonWeatherKind.PartlyCloudy)
                {
                    ambient = e;
                    continue;
                }

                return e;
            }

            return ambient;
        }

        private void ApplyGameplayWeather(SeasonWeatherKind kind)
        {
            if (_weatherManager == null)
                return;

            _weatherManager.ClearAllWeather();

            IReadOnlyList<WeatherType> types = _mapping != null
                ? _mapping.GetGameplayTypes(kind)
                : SeasonWeatherMapping.GetDefaultGameplayTypes(kind);

            for (int i = 0; i < types.Count; i++)
                _weatherManager.SetWeatherActive(types[i], true);
        }

        private void ApplyVfx(SeasonWeatherKind kind, SeasonWeatherKind previous, bool kindChanged)
        {
            if (kindChanged)
            {
                if (previous == SeasonWeatherKind.Fog && kind != SeasonWeatherKind.Fog)
                    _fogManager?.ClearAllFog();

                if (IsStormKind(previous) && !IsStormKind(kind))
                {
                    ClearStormsQuietly();
                    ClearThunderstormVfx();
                }

                if (IsRainKind(kind) && _cloudManager != null && _cloudManager.ActiveClouds != null)
                {
                    for (int i = 0; i < _cloudManager.ActiveClouds.Count; i++)
                    {
                        var cloud = _cloudManager.ActiveClouds[i];
                        if (cloud != null && cloud.VisualCategory == CloudVisualCategory.Cluster)
                        {
                            _cloudManager.RequestEvolveToRain(cloud.Id);
                            break;
                        }
                    }
                }
            }

            if (kind == SeasonWeatherKind.Thunderstorm && kindChanged)
            {
                SpawnThunderstormVfx();
                TrySpawnThunderstormSim();
            }
        }

        private void SpawnThunderstormVfx()
        {
            if (_thunderstormVfxPrefab == null)
            {
                Debug.LogWarning(
                    $"{nameof(SeasonWeatherDirector)}: Thunderstorm VFX prefab is not assigned.");
                return;
            }

            ClearThunderstormVfx();

            Transform parent = _thunderstormVisualParent;
            _activeThunderstormVfx = Instantiate(
                _thunderstormVfxPrefab,
                ResolveThunderstormVfxPosition(),
                Quaternion.identity,
                parent);
        }

        private Vector3 ResolveThunderstormVfxPosition()
        {
            if (_cloudManager != null && _cloudManager.ActiveClouds != null)
            {
                Vector2 sum = Vector2.zero;
                int used = 0;
                for (int i = 0; i < _cloudManager.ActiveClouds.Count && used < 3; i++)
                {
                    var cloud = _cloudManager.ActiveClouds[i];
                    if (cloud == null)
                        continue;
                    sum += cloud.Position;
                    used++;
                }

                if (used > 0)
                {
                    sum /= used;
                    return new Vector3(sum.x, _thunderstormVisualHeight, sum.y);
                }
            }

            if (_thunderstormVisualParent != null)
            {
                Vector3 parentPos = _thunderstormVisualParent.position;
                parentPos.y = _thunderstormVisualHeight;
                return parentPos;
            }

            return new Vector3(0f, _thunderstormVisualHeight, 0f);
        }

        private void ClearThunderstormVfx()
        {
            if (_activeThunderstormVfx == null)
                return;

            Destroy(_activeThunderstormVfx);
            _activeThunderstormVfx = null;
        }

        private void TrySpawnThunderstormSim()
        {
            if (_stormLifecycle == null || _worldTime == null)
                return;

            if (_cloudManager == null || _cloudManager.ActiveClouds == null || _cloudManager.ActiveClouds.Count == 0)
                return;

            var lifetimes = new List<double>();
            Vector2 pos = Vector2.zero;
            float radius = 2f;
            int used = 0;

            for (int i = 0; i < _cloudManager.ActiveClouds.Count && used < 3; i++)
            {
                var cloud = _cloudManager.ActiveClouds[i];
                if (cloud == null)
                    continue;
                pos += cloud.Position;
                lifetimes.Add(cloud.ExpireGameSeconds);
                used++;
            }

            if (used == 0)
                return;

            pos /= used;
            _suppressStormEventLog = true;
            try
            {
                _stormLifecycle.CreateStormFromClouds(pos, radius, _worldTime.TotalGameSeconds, lifetimes);
            }
            finally
            {
                _suppressStormEventLog = false;
            }
        }

        private void ClearStormsQuietly()
        {
            if (_stormLifecycle == null)
                return;

            var storms = _stormLifecycle.GetStormList();
            if (storms == null)
                return;

            for (int i = storms.Count - 1; i >= 0; i--)
                _stormLifecycle.RemoveStormAt(i);
        }

        public IEnumerable<SeasonWeatherScheduleEntry> GetUpcoming(int maxCount = 8)
        {
            if (_worldTime == null)
                yield break;

            double now = _worldTime.TotalGameSeconds;
            int yielded = 0;
            for (int i = 0; i < _schedule.Count; i++)
            {
                SeasonWeatherScheduleEntry e = _schedule[i];
                if (e == null || e.EndGameSeconds <= now)
                    continue;
                if (e.Kind == SeasonWeatherKind.Clear || e.Kind == SeasonWeatherKind.PartlyCloudy)
                    continue;

                yield return e;
                yielded++;
                if (yielded >= maxCount)
                    yield break;
            }
        }

        public void DebugForceWeather(SeasonWeatherKind kind, float durationHours = 2f)
        {
            if (_seasonSystem != null &&
                kind != SeasonWeatherKind.Clear &&
                kind != SeasonWeatherKind.PartlyCloudy &&
                !_seasonSystem.IsWeatherAllowed(kind))
            {
                Debug.LogWarning($"[SeasonWeather] {kind} is disallowed in {_seasonSystem.CurrentSeason}.");
                return;
            }

            if (kind == SeasonWeatherKind.MightyStorm)
            {
                Debug.LogWarning("[SeasonWeather] Mighty Storm is future-ready only and remains disabled.");
                return;
            }

            double now = _worldTime != null ? _worldTime.TotalGameSeconds : 0d;
            _debugOverride = new SeasonWeatherScheduleEntry
            {
                Kind = kind,
                StartGameSeconds = now,
                EndGameSeconds = now + Math.Max(0.25d, durationHours) * 3600d
            };
            _currentKind = SeasonWeatherKind.Clear;
            ApplyAt(now, force: true);
        }

        private static bool IsStormKind(SeasonWeatherKind kind) =>
            kind == SeasonWeatherKind.Thunderstorm || kind == SeasonWeatherKind.MightyStorm;

        private static bool IsRainKind(SeasonWeatherKind kind) =>
            kind == SeasonWeatherKind.SteadyRain ||
            kind == SeasonWeatherKind.HeavyRain ||
            kind == SeasonWeatherKind.Thunderstorm;
    }
}
