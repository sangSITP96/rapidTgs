using UnityEngine;

namespace Game.Travel
{
    public interface ITrailSurfaceProvider
    {
        TrailMode Mode { get; }

        bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier);
    }
}
