using UnityEngine;

namespace Game.Core.WorldTime
{
    [CreateAssetMenu(menuName = "Game/Time/Time Config", fileName = "TimeConfig")]
    public class TimeConfig : ScriptableObject
    {
        [Header("Simulation Rate")]
        [Tooltip("How many GAME seconds pass per 1 REAL second. Example: 60 = 1 game minute per real second.")]
        [Min(0f)] public float GameSecondsPerRealSecond = 60f;

        [Header("Start Time (Game)")] 
        [Range(0, 23)] public int StartHour = 6;
        [Range(0, 59)] public int StartMinute = 0;
        [Range(0, 59)] public int StartSecond = 0;
        
        [Header("Optional Limits")]
        public bool Wrap24Hours = true;
        
        public bool AllowBeyond24Hours = false;
    }
}
