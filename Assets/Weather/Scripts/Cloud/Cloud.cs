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
        Absorbed
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
        
        // Visual 
        public GameObject VisualObject;

        public bool IsExpired(double nowGameSeconds)
        {
            if (IsManagedByConvergence || IsReleasedFromConvergence)
                return false;

            return nowGameSeconds >= ExpireGameSeconds;
        }
    } 
}