using UnityEngine;

namespace Game.Travel
{
    /// <summary>
    /// Default Phase 12 trail provider: no trails.
    /// </summary>
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
