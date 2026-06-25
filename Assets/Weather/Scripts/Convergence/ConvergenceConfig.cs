using UnityEngine;

namespace  Game.Weather.Convergence
{
    [CreateAssetMenu(menuName = "Game/Weather/Convergence Config",
        fileName = "ConvergenceConfig")]
    public class ConvergenceConfig : ScriptableObject
    {
        [Header("Spawn Rules")] 
        [Min(0)] public int MinPoints = 3;
        [Min(0)] public int MaxPoints = 8;

        [Tooltip("Minimum distance between convergence points (world units).")] 
        [Min(0f)] public float MinSeparation = 20f;

        [Header("Drift")] 
        [Tooltip("Units per GAME hour.")] 
        [Min(0f)] public float DriftSpeed = 1.5f;

        [Tooltip("How often a new drift direction is chosen (game hour).")] 
        [Min(0.1f)] public float DriftRerollHours = 4f;

        [Header("Lifetime")] 
        [Tooltip("Minimum lifetime (game hours).")] 
        [Min(0.1f)] public float MinLifeTimehours = 6f;
        
        [Tooltip("Maximum lifetime (game hours).")]
        [Min(0.1f)] public float MaxLifeTimehours = 18f;

        [Header("Influence")] [Tooltip("How strongly storms are attracted")] 
        [Min(0f)] public float attractionStrength = 1f;

        [Header("Held Clouds")]
        [Tooltip("How often Convergence rolls release/dissipate for each held cloud (game seconds). " +
                 "Edit via ConvergenceConfig asset assigned on ConvergenceManager.")]
        [Min(1f)] public float HeldCloudCheckIntervalSeconds = 120f;

        [Tooltip("Chance per check that a held cloud is released to drift east off the map.")]
        [Range(0f, 1f)] public float ReleaseChancePerCheck = 0.05f;

        [Tooltip("Chance per check that a held cloud dissipates inside Convergence.")]
        [Range(0f, 1f)] public float DissipateChancePerCheck = 0.03f;

        [Header("Intensity Thresholds (future)")]
        [Min(0f)] public float StormIntensityThreshold = 20f;
        [Min(0f)] public float SnowIntensityThreshold = 15f;
        [Min(0f)] public float ReleaseIntensityThreshold = 8f;
    }
}