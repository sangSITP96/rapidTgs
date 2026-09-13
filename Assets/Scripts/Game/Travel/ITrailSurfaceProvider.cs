using UnityEngine;

namespace Game.Travel
{
    /// <summary>
    /// Trail overlay source. Travel must not know TGS/Kronnect internals.
    /// Phase 12 ships None. Border/Crossing providers come later for A/B/C tests.
    /// </summary>
    public interface ITrailSurfaceProvider
    {
        TrailMode Mode { get; }

        /// <summary>
        /// Returns true if worldPos is considered on a trail/road overlay.
        /// </summary>
        bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier);
    }
}
