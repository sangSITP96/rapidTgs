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
    }
}