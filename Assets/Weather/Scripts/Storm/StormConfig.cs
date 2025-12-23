using UnityEngine;

namespace Game.Weather.Storm
{
    [CreateAssetMenu(fileName = "StormConfig", menuName = "Game/Weather/Storm Config")]
    public class StormConfig : ScriptableObject
    {
        [Header("Spawn Chance")] [Range(0f, 1f)]
        public float ActiveChance = 0.35f;

        [Header("Duration (Game Hours)")]
        [Min(0.1f)] public float ShortMin = 0.33f;
        [Min(0.1f)] public float ShortMax = 1.0f;

        [Min(0.1f)] public float MediumMin = 1.0f;
        [Min(0.1f)] public float MediumMax = 4.0f;

        [Min(0.1f)] public float LargeMin = 4.0f;
        [Min(0.1f)] public float LargeMax = 12.0f;

        [Header("Radius (Prototype Placeholder)")] [Min(0.1f)]
        public float MinRadius = 8f;
        [Min(0.1f)] public float MaxRadius = 25f;
    }
}