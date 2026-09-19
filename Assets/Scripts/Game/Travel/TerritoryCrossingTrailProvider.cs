using UnityEngine;

namespace Game.Travel
{
    public sealed class TerritoryCrossingTrailProvider : ITrailSurfaceProvider
    {
        public TrailMode Mode => TrailMode.TerritoryCrossing;

        public bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier)
        {
            movementBonusMultiplier = 1f;

            return false;
        }
    }
}
