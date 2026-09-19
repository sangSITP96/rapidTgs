using UnityEngine;

namespace Game.Travel
{
    public sealed class NoneTrailProvider : ITrailSurfaceProvider
    {
        public TrailMode Mode => TrailMode.None;

        public bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier)
        {
            movementBonusMultiplier = 1f;
            return false;
        }
    }
}
