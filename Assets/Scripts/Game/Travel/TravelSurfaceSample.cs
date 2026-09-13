using UnityEngine;

namespace Game.Travel
{
    public struct TravelSurfaceSample
    {
        public TravelSurfaceType SurfaceType;
        public bool IsPassable;
        public float MovementModifier;
        public float NeedsModifier;
        public bool IsTrail;
        public Vector3 WorldPosition;

        public static TravelSurfaceSample Impassable(Vector3 worldPos, TravelSurfaceType type)
        {
            return new TravelSurfaceSample
            {
                SurfaceType = type,
                IsPassable = false,
                MovementModifier = 0f,
                NeedsModifier = 1f,
                IsTrail = false,
                WorldPosition = worldPos
            };
        }
    }
}
