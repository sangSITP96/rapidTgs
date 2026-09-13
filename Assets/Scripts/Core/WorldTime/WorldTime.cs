using System;
using UnityEngine;

namespace Game.Core.WorldTime
{
    public class WorldTime : MonoBehaviour
    {
        [SerializeField] private TimeConfig _config;

        public TimeConfig Config => _config;

        public double TotalGameSeconds { get; private set; }

        public float GameSecondsPerRealSecond => _config != null ? _config.GameSecondsPerRealSecond : 0f;
        public bool IsPaused { get; private set; }

        public event Action<double, double> OnTimeAdvanced;
        public event Action<int, int, int> OnClockChanged;
        public event Action<DayPhase, DayPhase> OnDayPhaseChanged;

        private int _lastClockH, _lastClockM, _lastClockS;
        private DayPhase _lastDayPhase = DayPhase.Day;

        public const int SecondsPerMinute = 60;
        public const int SecondsPerHour = 3600;
        public const int SecondsPerDay = 86400;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning($"{nameof(WorldTime)}: No TimeConfig has been assigned. Time will not advance");
            }

            SetTotalGameSeconds(
                _config != null
                    ? (_config.StartHour * SecondsPerHour) +
                      (_config.StartMinute * SecondsPerMinute) +
                      _config.StartSecond
                    : 0);
        }

        private void FixedUpdate()
        {
            if (IsPaused) return;
            if (_config == null) return;
            if (_config.GameSecondsPerRealSecond <= 0f) return;

            double deltaGameSeconds = Time.deltaTime * _config.GameSecondsPerRealSecond;
            Advance(deltaGameSeconds);
        }

        public void Pause(bool paused) => IsPaused = paused;

        public void SetTotalGameSeconds(double totalSeconds)
        {
            TotalGameSeconds = Math.Max(0d, totalSeconds);

            if (_config != null && _config.Wrap24Hours)
                TotalGameSeconds = Mod(TotalGameSeconds, SecondsPerDay);

            PushClockChangedIfNeeded(force: true);
            PushDayPhaseChangedIfNeeded(force: true);
        }

        public void SetTime(double timeOfDaySeconds)
        {
            timeOfDaySeconds = Math.Max(0d, timeOfDaySeconds);

            if (_config != null && _config.Wrap24Hours)
                timeOfDaySeconds = Mod(timeOfDaySeconds, SecondsPerDay);

            // Preserve completed days when setting clock within the current day.
            int dayIndex = GetDayIndex();
            TotalGameSeconds = (dayIndex * (double)SecondsPerDay) + Mod(timeOfDaySeconds, SecondsPerDay);

            if (_config != null && _config.Wrap24Hours)
                TotalGameSeconds = Mod(TotalGameSeconds, SecondsPerDay);

            PushClockChangedIfNeeded(force: true);
            PushDayPhaseChangedIfNeeded(force: true);
        }

        public void Advance(double deltaGameSeconds)
        {
            if (deltaGameSeconds <= 0d) return;

            double newTotal = TotalGameSeconds + deltaGameSeconds;

            if (_config != null && !_config.AllowBeyond24Hours)
                newTotal = Math.Min(newTotal, SecondsPerDay);

            if (_config != null && _config.Wrap24Hours)
                newTotal = Mod(newTotal, SecondsPerDay);

            TotalGameSeconds = newTotal;
            OnTimeAdvanced?.Invoke(deltaGameSeconds, TotalGameSeconds);
            PushClockChangedIfNeeded(force: false);
            PushDayPhaseChangedIfNeeded(force: false);
        }

        /// <summary>Absolute completed game days (0-based).</summary>
        public int GetDayIndex()
        {
            return (int)Math.Floor(TotalGameSeconds / SecondsPerDay);
        }

        /// <summary>Seconds into the current game day [0, 86400).</summary>
        public double GetSecondsIntoDay()
        {
            return Mod(TotalGameSeconds, SecondsPerDay);
        }

        /// <summary>0..1 progress through the current game day.</summary>
        public float GetNormalizedDayProgress()
        {
            return (float)(GetSecondsIntoDay() / SecondsPerDay);
        }

        /// <summary>Time-of-day seconds for clock / day-night (always within one day).</summary>
        public int GetTimeOfDaySeconds()
        {
            return (int)Math.Floor(GetSecondsIntoDay());
        }

        public void GetClock(out int hour, out int minute, out int second)
        {
            int t = GetTimeOfDaySeconds();
            hour = t / SecondsPerHour;
            t -= hour * SecondsPerHour;
            minute = t / SecondsPerMinute;
            second = t - minute * SecondsPerMinute;
        }

        public DayPhase GetDayPhase()
        {
            GetDaylightWindows(
                out double sunriseStart,
                out double sunriseEnd,
                out double sunsetStart,
                out double sunsetEnd);

            double t = GetSecondsIntoDay();

            if (t >= sunriseStart && t < sunriseEnd)
                return DayPhase.Sunrise;
            if (t >= sunriseEnd && t < sunsetStart)
                return DayPhase.Day;
            if (t >= sunsetStart && t < sunsetEnd)
                return DayPhase.Sunset;

            return DayPhase.Night;
        }

        public bool IsNight()
        {
            DayPhase phase = GetDayPhase();
            return phase == DayPhase.Night;
        }

        public bool IsDaylight()
        {
            DayPhase phase = GetDayPhase();
            return phase == DayPhase.Day || phase == DayPhase.Sunrise || phase == DayPhase.Sunset;
        }

        /// <summary>
        /// 0 at start of sunrise, 1 at end of sunrise. Outside sunrise returns 0 or 1.
        /// Useful later for lighting visuals.
        /// </summary>
        public float GetSunriseProgress()
        {
            GetDaylightWindows(out double sunriseStart, out double sunriseEnd, out _, out _);
            double t = GetSecondsIntoDay();
            if (t <= sunriseStart) return 0f;
            if (t >= sunriseEnd) return 1f;
            if (sunriseEnd <= sunriseStart) return 1f;
            return (float)((t - sunriseStart) / (sunriseEnd - sunriseStart));
        }

        /// <summary>
        /// 0 at start of sunset, 1 at end of sunset.
        /// </summary>
        public float GetSunsetProgress()
        {
            GetDaylightWindows(out _, out _, out double sunsetStart, out double sunsetEnd);
            double t = GetSecondsIntoDay();
            if (t <= sunsetStart) return 0f;
            if (t >= sunsetEnd) return 1f;
            if (sunsetEnd <= sunsetStart) return 1f;
            return (float)((t - sunsetStart) / (sunsetEnd - sunsetStart));
        }

        /// <summary>
        /// Interpolated night fraction for the current game day (seasonal daylight only).
        /// </summary>
        public float GetCurrentNightFraction()
        {
            GetSeasonBlend(out int seasonA, out int seasonB, out float t);

            float nightA = GetSeasonNightFraction(seasonA);
            float nightB = GetSeasonNightFraction(seasonB);
            return Mathf.Lerp(nightA, nightB, SmoothStep01(t));
        }

        /// <summary>
        /// Current primary season for daylight interpolation only (not a full season system).
        /// 0=Spring, 1=Summer, 2=Autumn, 3=Winter.
        /// </summary>
        public int GetCurrentSeasonIndex()
        {
            GetSeasonBlend(out int seasonA, out _, out _);
            return seasonA;
        }

        public string GetCurrentSeasonName()
        {
            switch (GetCurrentSeasonIndex())
            {
                case 0: return "Spring";
                case 1: return "Summer";
                case 2: return "Autumn";
                default: return "Winter";
            }
        }

        /// <summary>
        /// 0..1 blend toward the next season (for debug / gradual daylight transition).
        /// </summary>
        public float GetSeasonBlendProgress()
        {
            GetSeasonBlend(out _, out _, out float t);
            return t;
        }

        private void GetSeasonBlend(out int seasonA, out int seasonB, out float t)
        {
            if (_config == null)
            {
                seasonA = 0;
                seasonB = 1;
                t = 0f;
                return;
            }

            int daysPerYear = Mathf.Max(4, _config.GameDaysPerYear);
            float yearProgress = (GetDayIndex() + GetNormalizedDayProgress()) / daysPerYear;
            yearProgress = yearProgress - Mathf.Floor(yearProgress);

            // Shift so day 0 starts at StartingSeasonIndex.
            float seasonProgress = yearProgress * 4f + _config.StartingSeasonIndex;
            seasonProgress = seasonProgress - Mathf.Floor(seasonProgress / 4f) * 4f;

            seasonA = Mathf.FloorToInt(seasonProgress) % 4;
            seasonB = (seasonA + 1) % 4;
            t = seasonProgress - Mathf.Floor(seasonProgress);
        }

        public void GetDaylightWindows(
            out double sunriseStart,
            out double sunriseEnd,
            out double sunsetStart,
            out double sunsetEnd)
        {
            float nightFraction = Mathf.Clamp01(GetCurrentNightFraction());
            double nightSeconds = nightFraction * SecondsPerDay;
            double daySeconds = SecondsPerDay - nightSeconds;

            double sunriseDuration = GetTransitionSeconds(
                _config != null ? _config.SunriseDurationMinutes : 30f);
            double sunsetDuration = GetTransitionSeconds(
                _config != null ? _config.SunsetDurationMinutes : 30f);

            // Keep transitions inside the day window when possible.
            sunriseDuration = Math.Min(sunriseDuration, Math.Max(0d, daySeconds * 0.45d));
            sunsetDuration = Math.Min(sunsetDuration, Math.Max(0d, daySeconds * 0.45d));

            // Split night evenly before dawn and after dusk; day centered around noon.
            sunriseStart = nightSeconds * 0.5d;
            sunriseEnd = sunriseStart + sunriseDuration;
            sunsetEnd = SecondsPerDay - (nightSeconds * 0.5d);
            sunsetStart = sunsetEnd - sunsetDuration;

            if (sunsetStart < sunriseEnd)
            {
                double mid = (sunriseEnd + sunsetStart) * 0.5d;
                sunriseEnd = mid;
                sunsetStart = mid;
            }
        }

        private float GetSeasonNightFraction(int seasonIndex)
        {
            if (_config == null)
                return 0.25f;

            switch (seasonIndex)
            {
                case 0: return _config.SpringNightFraction;
                case 1: return _config.SummerNightFraction;
                case 2: return _config.AutumnNightFraction;
                default: return _config.WinterNightFraction;
            }
        }

        private static double GetTransitionSeconds(float minutes)
        {
            return Math.Max(0d, minutes) * SecondsPerMinute;
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void PushClockChangedIfNeeded(bool force)
        {
            GetClock(out int hour, out int minute, out int second);
            if (force || hour != _lastClockH || minute != _lastClockM || second != _lastClockS)
            {
                _lastClockH = hour;
                _lastClockM = minute;
                _lastClockS = second;
                OnClockChanged?.Invoke(hour, minute, second);
            }
        }

        private void PushDayPhaseChangedIfNeeded(bool force)
        {
            DayPhase phase = GetDayPhase();
            if (force || phase != _lastDayPhase)
            {
                DayPhase previous = _lastDayPhase;
                _lastDayPhase = phase;
                OnDayPhaseChanged?.Invoke(previous, phase);
            }
        }

        private static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }

        private static double Mod(double a, double m)
        {
            double r = a % m;
            return r < 0d ? r + m : r;
        }
    }
}
