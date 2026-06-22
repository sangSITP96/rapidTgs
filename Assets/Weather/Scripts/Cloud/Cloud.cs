using System;
using UnityEngine;

namespace Game.Weather.Cloud
{
    public enum CloudState
    {
        Spawning,
        Drifting,
        InConvergence,
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
        
        // Visual 
        public GameObject VisualObject;

        public bool IsExpired(double nowGameSeconds)
        {
            return nowGameSeconds >= ExpireGameSeconds;
        }
    } 
}