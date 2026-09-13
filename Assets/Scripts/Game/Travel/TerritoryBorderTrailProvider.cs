using UnityEngine;

namespace Game.Travel
{
    /// <summary>
    /// Stub for future A/B test: trails along Kronnect TGS territory borders.
    /// Not implemented in Phase 12 — keeps architecture open without coupling Travel to TGS.
    /// </summary>
    public sealed class TerritoryBorderTrailProvider : ITrailSurfaceProvider
    {
        public TrailMode Mode => TrailMode.TerritoryBorder;

        public bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier)
        {
            movementBonusMultiplier = 1f;
            // TODO(Phase 13+): sample distance to territory border edges from a TGS-backed data layer.
            return false;
        }
    }
}
