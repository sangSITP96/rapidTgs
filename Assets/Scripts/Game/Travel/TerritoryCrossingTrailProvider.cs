using UnityEngine;

namespace Game.Travel
{
    /// <summary>
    /// Stub for future A/B test: trails that cut through territories (footpaths).
    /// Not implemented in Phase 12 — expects authored/baked path data later.
    /// </summary>
    public sealed class TerritoryCrossingTrailProvider : ITrailSurfaceProvider
    {
        public TrailMode Mode => TrailMode.TerritoryCrossing;

        public bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier)
        {
            movementBonusMultiplier = 1f;
            // TODO(Phase 13+): sample authored/baked crossing trail polylines or masks.
            return false;
        }
    }
}
