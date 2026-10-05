using UnityEngine;

namespace Game.Seasons
{
    [CreateAssetMenu(menuName = "Game/Seasons/Season Calendar Config", fileName = "SeasonCalendarConfig")]
    public sealed class SeasonCalendarConfig : ScriptableObject
    {
        [Header("Alpha calendar")]
        [Min(1)] public int DaysPerSeason = 7;

        [Min(4)] public int GameDaysPerYear = 28;

        public SeasonId StartingSeason = SeasonId.Spring;

        [Header("Weekday mapping (game calendar)")]
        [Range(0, 6)] public int DayZeroWeekday = 0;

        [Range(0, 6)] public int SeasonChangeWeekday = 0;

        [Header("Display")]
        public TemperatureUnit DisplayTemperatureUnit = TemperatureUnit.Fahrenheit;

        [Header("Schedule")]
        public int ScheduleSeed = 14;

        public int EffectiveDaysPerYear => Mathf.Max(4, DaysPerSeason * 4);

        public void SyncYearLength()
        {
            GameDaysPerYear = EffectiveDaysPerYear;
        }

        public int GetWeekday(int dayIndex)
        {
            int weekday = DayZeroWeekday + (dayIndex % 7);
            weekday %= 7;
            if (weekday < 0)
                weekday += 7;
            return weekday;
        }

        public string GetWeekdayName(int dayIndex)
        {
            switch (GetWeekday(dayIndex))
            {
                case 0: return "Monday";
                case 1: return "Tuesday";
                case 2: return "Wednesday";
                case 3: return "Thursday";
                case 4: return "Friday";
                case 5: return "Saturday";
                default: return "Sunday";
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncYearLength();
        }
#endif
    }
}
