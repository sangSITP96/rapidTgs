using System;
using UnityEngine;

namespace Game.Seasons
{
    [Serializable]
    public sealed class SeasonWeatherEventRule
    {
        public SeasonWeatherKind Kind = SeasonWeatherKind.Clear;

        [Tooltip("If false, this kind is disallowed for the season.")]
        public bool Allowed = true;

        [Tooltip("If true, scheduler may place discrete timed events for this kind.")]
        public bool ScheduleAsEvents = false;

        [Min(0)] public int MinCountPerSeason = 0;
        [Min(0)] public int MaxCountPerSeason = 0;

        [Min(0f)] public float MinDurationHours = 1f;
        [Min(0f)] public float MaxDurationHours = 4f;

        [Header("Placement (0..1 season progress)")]
        [Range(0f, 1f)] public float PreferProgressMin = 0f;
        [Range(0f, 1f)] public float PreferProgressMax = 1f;

        [Header("Optional weekday window (game week: 0=Mon .. 6=Sun)")]
        public bool UseWeekdayWindow = false;
        [Range(0, 6)] public int WindowStartWeekday = 0;
        [Range(0, 23)] public int WindowStartHour = 0;
        [Range(0, 6)] public int WindowEndWeekday = 6;
        [Range(0, 23)] public int WindowEndHour = 23;

        [Tooltip("Minimum gap between events of this kind (game hours).")]
        [Min(0f)] public float MinGapHours = 6f;
    }
}
