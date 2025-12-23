using System;
using Game.Core.WorldTime;
using UnityEngine;

namespace Game.Weather.Global
{
    public class GlobalWindSystem : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private WindBiasProfile _windBiasProfile;

        [Header("Debug")] [SerializeField] private bool _logChanges = false;

        public WindDirection CurrentDirection => _currentDirection;

        public Vector2 CurrentBiasVector => WindDirectionUtil.ToVector2(_currentDirection) *
                                            (_windBiasProfile != null ? _windBiasProfile.WinStrength : 1f);
        public event Action<WindDirection> OnWindChanged;

        private WindDirection _baseDirection = WindDirection.WestToEast;
        private WindDirection _currentDirection = WindDirection.WestToEast;

        private double _nextBaseRerollAtGameSeconds;

        private bool _sporadicActive;
        private double _sporadicEndAtGameSeconds;

        private const double SecondsPerMinute = 60.0;
        private const double SecondsPerHour = 3600.0;

        private void OnEnable()
        {
            if (_worldTime == null)
            {
                _worldTime = FindFirstObjectByType<WorldTime>();
            }

            if (_worldTime == null)
            {
                Debug.LogError("No WorldTime object found!");
                enabled = false;
                return;
            }

            if (_windBiasProfile == null)
            {
                Debug.LogError("No WindBiasProfile found!");
                enabled = false;
                return;
            }

            _worldTime.OnTimeAdvanced += HandleTimeAdvanced;

            // Initialize immediately
            RerollBaseDirection(_worldTime.TotalGameSeconds);
            ScheduleNextReroll(_worldTime.TotalGameSeconds);
            ApplyEffectiveDirection(forceNotify: true);

        }

        private void OnDisable()
        {
            if (_worldTime != null)
            {
                _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
            }
        }

        private void HandleTimeAdvanced(double deltaGameSeconds, double totalGameSeconds)
        {
            // handle end of sporadic override
            if (_sporadicActive && totalGameSeconds >= _sporadicEndAtGameSeconds)
            {
                _sporadicActive = false;
                ApplyEffectiveDirection(forceNotify: true);
            }
            
            // handle base bias reroll
            if (totalGameSeconds >= _nextBaseRerollAtGameSeconds)
            {
                while (totalGameSeconds >= _nextBaseRerollAtGameSeconds)
                {
                    RerollBaseDirection(_nextBaseRerollAtGameSeconds);
                    _nextBaseRerollAtGameSeconds += _windBiasProfile.biasChangeHours * SecondsPerHour;
                }
                
                ApplyEffectiveDirection(forceNotify: true);
            }
        }

        private void ScheduleNextReroll(double nowGameSeconds)
        {
            _nextBaseRerollAtGameSeconds = nowGameSeconds + (_windBiasProfile.biasChangeHours * SecondsPerHour);
        }

        private void RerollBaseDirection(double nowGameSeconds)
        {
            _baseDirection = RollWeightedBaseDirection(_windBiasProfile);

            if (_baseDirection == WindDirection.WestToEast ||
                _baseDirection == WindDirection.WestToNortheast ||
                _baseDirection == WindDirection.WestToSoutheast)
            {
                _sporadicActive = false;
                return;
            }
            
            StartSporadicOverride(nowGameSeconds);
        }

        private void StartSporadicOverride(double nowGameSeconds)
        {
            WindDirection temp = RollSporadicTempDirection(_windBiasProfile);
            double durationSeconds =
                RollRange(_windBiasProfile.SporadicMinMinutes,
                    _windBiasProfile.SporadicMaxMinutes) * SecondsPerMinute;
            
            _sporadicActive = true;
            _currentDirection = temp;
            _sporadicEndAtGameSeconds = nowGameSeconds + durationSeconds;

            if (_logChanges)
            {
                Debug.Log($"[GlobalWind] Sporadic override: {temp} for {durationSeconds:0}s (until {_sporadicEndAtGameSeconds:0})");
            }
            
            OnWindChanged?.Invoke(_currentDirection);
        }

        private void ApplyEffectiveDirection(bool forceNotify)
        {
            WindDirection desired = _sporadicActive ? _currentDirection :
                NormalizeBaseToEffective(_baseDirection);

            if (!forceNotify && desired == _currentDirection)
                return;
            
            _currentDirection = desired;

            if (_logChanges)
            {
                Debug.Log($"[GlobalWind] Effective direction: {_currentDirection} (base={_baseDirection}, sporadic={_sporadicActive})");
            }
            OnWindChanged?.Invoke(_currentDirection);
        }
        
        private static WindDirection NormalizeBaseToEffective(WindDirection baseDirection)
        {
            if (baseDirection == WindDirection.North ||
                baseDirection == WindDirection.South ||
                baseDirection == WindDirection.East)
            {
                return WindDirection.WestToEast;
            }
            return baseDirection;
        }

        private static WindDirection RollWeightedBaseDirection(WindBiasProfile profile)
        {
            float wE = Mathf.Max(0f, profile.WeightWestToEast);
            float wNE = Mathf.Max(0f, profile.WeightWestToNorthEast);
            float wSE = Mathf.Max(0f, profile.WeightWestToSouthEast);
            float wS = Mathf.Max(0f, profile.WeightSporadic);
            
            float sum = wE + wNE + wSE + wE;
            if (sum <= 0f)
            {
                return WindDirection.WestToEast;
            }

            float rand = UnityEngine.Random.value * sum;

            if (rand < wE) return WindDirection.WestToEast;
            rand -= wE;

            if (rand < wNE) return WindDirection.WestToNortheast;
            rand -= wNE;

            if (rand < wSE) return WindDirection.WestToSoutheast;
            rand -= wSE;

            return WindDirection.East;
        }

        private static WindDirection RollSporadicTempDirection(WindBiasProfile profile)
        {
            int count = 0;
            if (profile.AllowTempEast) count++;
            if (profile.AllowTempNorth) count++;
            if (profile.AllowTempSouth) count++;

            if (count <= 0)
            {
                return WindDirection.East;
            }
            
            int pick = UnityEngine.Random.Range(0, count);
            if (profile.AllowTempEast)
            {
                if (pick == 0) return WindDirection.East;
                pick--;
            }

            if (profile.AllowTempNorth)
            {
                if(pick == 0) return WindDirection.North;
                pick--;
            }
            
            return WindDirection.South;
        }

        private static float RollRange(float min, float max)
        {
            if (min > max) (min,max) = (max, min);
            return UnityEngine.Random.Range(min, max);
        }
    }
}

