using System;
using UnityEngine;

namespace Game.Weather.Cloud
{
    public enum CloudState
    {
        Spawning,
        Drifting,
        InConvergence,
        Held,
        Absorbed,
        EvolvingToRain,
        RainBand
    }
    
    [Serializable]
    public class Cloud
    {
        public int Id;
        public Vector2 Position;
        public float Radius = 1f; // Small radius
        public CloudState State = CloudState.Spawning;
        
        // Lifetime
        public double SpawnGameSeconds;
        public double ExpireGameSeconds;
        public float Age;
        
        // Movement
        public float SpawnTimer; // Time on lake before detach
        public int SourceLakeId;

        public CloudVisualCategory VisualCategory;
        public float SourceLakeSize;

        public bool IsManagedByConvergence;
        public int HeldConvergencePointId = -1;
        public float ConvergenceIntensity;
        public bool IsReleasedFromConvergence;

        /// <summary>
        /// When true, this cluster cloud will evolve into a rain band (logic hook for future phases).
        /// </summary>
        public bool ShouldEvolveToRain;

        public bool IsEvolvingToRain;
        public bool IsRainBand;

        // Target offset around the convergence point so clouds don't pile on the center
        public bool HasHoldTarget;
        public Vector2 HoldTargetOffset;
        
        // Visual 
        public GameObject VisualObject;

        public bool IsExpired(double nowGameSeconds)
        {
            if (IsManagedByConvergence || IsReleasedFromConvergence || IsEvolvingToRain || IsRainBand)
                return false;

            return nowGameSeconds >= ExpireGameSeconds;
        }
    } 
}