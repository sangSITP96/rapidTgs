using UnityEngine;

namespace Game.Weather.Storm
{
    [CreateAssetMenu(menuName = "Game/Weather/Storm Movement Config", fileName = "StormMovementConfig")]
    public class StormMovementConfig : ScriptableObject
    {
        [Header("Speed (units per game hour)")] 
        [Min(1f)] public float BaseSpeed = 12f;

        [Header("Steering Weights")] 
        [Range(0f, 1f)] public float WindWeight = 0.7f;

        [Range(0f, 1f)] public float ConvergenceWeight = 0.3f;

        [Header("Bounds")] 
        public bool DestroyIfOutsideBounds = true;
        
        // MERGE PARAMS
        [Header("Storm Merging")]
        [Tooltip("Storms merge when overlapping. 1.0 = touching, 0.8 = 80% overlap required")]
        [Range(0.5f, 1.5f)] public float MergeOverlapThreshold = 0.9f;

        [Tooltip("Maximum radius a storm can reach after merging (units)")] 
        [Min(0.5f)] public float MaxMergedRadius = 2.0f;
        
        [Tooltip("Enable storm merging feature")]
        public bool EnableMerging = true;


    }
}