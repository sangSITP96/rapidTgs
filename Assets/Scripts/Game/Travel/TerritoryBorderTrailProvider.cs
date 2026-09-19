using UnityEngine;

namespace Game.Travel
{
    public sealed class TerritoryBorderTrailProvider : ITrailSurfaceProvider
    {
        public TrailMode Mode => TrailMode.TerritoryBorder;

        public bool TrySampleTrail(Vector3 worldPos, out float movementBonusMultiplier)
        {
            movementBonusMultiplier = 1f;

            return false;
        }
    }
}
