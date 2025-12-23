using System;
using UnityEngine;

namespace Game.Weather.Storm
{
    [Serializable]
    public class Storm
    {
        public StormDebugView view;
        public Vector2 Position;
        public float Radius;

        public StormState State;

        public double SpawnGameSeconds;
        public double ExpireGameSeconds;

        public bool IsActive;

        public bool IsExpired(double now)
        {
            return now >= ExpireGameSeconds;
        }

    }
}

