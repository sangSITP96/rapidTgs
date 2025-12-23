using System;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Core.WorldTime
{
    public class WorldTime : MonoBehaviour
    {
        [SerializeField] private TimeConfig _config;
        
        public double TotalGameSeconds { get; private set; }
        
        public float GameSecondsPerRealSecond => _config!=null?_config.GameSecondsPerRealSecond:0;
        public bool IsPaused { get; private set; }

        public event Action<double, double> OnTimeAdvanced;

        public event Action<int, int, int> OnClockChanged;

        private int _lastClockH, _lastClockM, _lastClockS;

        private const int SecondsPerMinute = 60;
        private const int SecondsPerHour = 3600;
        private const int SecondsPerDay = 86400;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning($"{nameof(WorldTime)}: No TimeConfig has been assigned. Time will not advance");
            }

            SetTime(_config != null ? (_config.StartHour * SecondsPerHour)+
                                      (_config.StartMinute*SecondsPerMinute)+
                                      _config.StartSecond : 0);
        }

        private void Update()
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
            PushClockChangedIfNeeded(force:true);
        }

        public void SetTime(double timeOfDaySeconds)
        {
            timeOfDaySeconds = Math.Max(0d, timeOfDaySeconds);

            if (_config != null && _config.Wrap24Hours)
            {
                timeOfDaySeconds %= SecondsPerDay;
            }
            TotalGameSeconds = timeOfDaySeconds;
            PushClockChangedIfNeeded(force:true);
        }

        public void Advance(double deltaGameSeconds)
        {
            if(deltaGameSeconds <= 0d) return;
            
            double newTotal = TotalGameSeconds + deltaGameSeconds;

            if (_config != null && !_config.AllowBeyond24Hours)
            {
                newTotal = Math.Min(newTotal, SecondsPerDay);
            }

            if (_config != null && _config.Wrap24Hours && _config.AllowBeyond24Hours)
            {
                TotalGameSeconds = newTotal;
            }
            else if (_config != null && _config.Wrap24Hours && !_config.AllowBeyond24Hours)
            {
                TotalGameSeconds = newTotal;
            }
            else
            {
                TotalGameSeconds = newTotal;
            }
            
            OnTimeAdvanced?.Invoke(deltaGameSeconds, TotalGameSeconds);
            PushClockChangedIfNeeded(force:false);
        }

        public int GetTimeOfDaySeconds()
        {
            int t = (int)Math.Floor(TotalGameSeconds);
            if (_config != null && _config.Wrap24Hours)
            {
                t = Mod(t, SecondsPerDay);
            }

            return t;
        }

        public void GetClock(out int hour, out int minute, out int second)
        {
            int t = GetTimeOfDaySeconds();
            hour = t / SecondsPerHour;
            t -= hour * SecondsPerHour;
            minute = t / SecondsPerMinute;
            second = t - minute * SecondsPerMinute;
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

        private static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }
    }
}
 