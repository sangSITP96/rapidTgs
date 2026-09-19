using UnityEngine;

namespace Game.Core.WorldTime
{
    [CreateAssetMenu(menuName = "Game/Time/Time Config", fileName = "TimeConfig")]
    public class TimeConfig : ScriptableObject
    {
        [Header("Simulation Rate")]
        [Tooltip("Game seconds per 1 real second. For ~4 game days / 1 real day, use 4 (1 game day ≈ 6 real hours).")]
        [Min(0f)] public float GameSecondsPerRealSecond = 4f;

        [Header("Start Time (Game)")]
        [Range(0, 23)] public int StartHour = 6;
        [Range(0, 59)] public int StartMinute = 0;
        [Range(0, 59)] public int StartSecond = 0;

        [Header("Optional Limits")]
        public bool Wrap24Hours = false;

        public bool AllowBeyond24Hours = true;

        [Header("Day / Night Transitions (Game Minutes)")]
        [Tooltip("Duration of sunrise transition from Night to Day.")]
        [Min(0f)] public float SunriseDurationMinutes = 30f;

        [Tooltip("Duration of sunset transition from Day to Night.")]
        [Min(0f)] public float SunsetDurationMinutes = 30f;

        [Header("Seasonal Night Length (fraction of day that is night)")]
        [Tooltip("Night fraction at mid-Spring. Interpolates between seasons.")]
        [Range(0f, 1f)] public float SpringNightFraction = 0.21f;

        [Range(0f, 1f)] public float SummerNightFraction = 0.19f;

        [Range(0f, 1f)] public float AutumnNightFraction = 0.23f;

        [Range(0f, 1f)] public float WinterNightFraction = 0.25f;

        [Header("Season Calendar (Game Days)")]
        [Tooltip("Length of one full year used only for daylight interpolation. Not a full season system.")]
        [Min(4)] public int GameDaysPerYear = 360;

        [Tooltip("Season index at day 0: 0=Spring, 1=Summer, 2=Autumn, 3=Winter.")]
        [Range(0, 3)] public int StartingSeasonIndex = 0;

        [Header("Daily Clock Drift (Placeholder)")]
        [Tooltip("Placeholder only. ~1 game hour drift per real day. Not applied until persistence/network rules are confirmed.")]
        public bool EnableDailyClockDrift = false;

        [Tooltip("Game seconds of clock drift per real day when enabled.")]
        [Min(0f)] public float ClockDriftGameSecondsPerRealDay = 3600f;
    }
}
