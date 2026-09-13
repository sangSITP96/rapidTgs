using UnityEngine;

namespace Game.Travel
{
    [CreateAssetMenu(menuName = "Game/Travel/Travel Settings", fileName = "TravelSettings")]
    public sealed class TravelSettings : ScriptableObject
    {
        [Header("Base March")]
        [Tooltip("World units traveled per game hour at 1.0x movement modifier.")]
        [Min(0.01f)] public float BaseMarchSpeedUnitsPerGameHour = 12f;

        [Tooltip("Arrival distance threshold in world units.")]
        [Min(0.01f)] public float ArrivalDistanceThreshold = 0.15f;

        [Header("Surface Movement Modifiers")]
        [Min(0f)] public float GrasslandMovementModifier = 1f;
        [Min(0f)] public float ForestMovementModifier = 0.70f;
        [Min(0f)] public float MountainMovementModifier = 0.45f;
        [Min(0f)] public float RoadTrailMovementModifier = 1.25f;
        [Tooltip("Lake/Water is impassable. Kept for clarity; blocked surfaces use 0.")]
        [Min(0f)] public float LakeMovementModifier = 0f;

        [Header("Surface Passability")]
        public bool GrasslandPassable = true;
        public bool ForestPassable = true;
        public bool MountainPassable = true;
        public bool RoadTrailPassable = true;
        public bool LakePassable = false;

        [Header("Direct Terrain Needs Penalties (optional)")]
        [Tooltip("If false, terrain only slows travel (longer time => more natural needs drain).")]
        public bool EnableDirectTerrainNeedsPenalty = false;

        [Min(0f)] public float GrasslandNeedsModifier = 1f;
        [Min(0f)] public float ForestNeedsModifier = 1f;
        [Min(0f)] public float MountainNeedsModifier = 1f;
        [Min(0f)] public float RoadTrailNeedsModifier = 1f;
        [Min(0f)] public float LakeNeedsModifier = 1f;

        [Header("Needs — Marching (per game hour)")]
        [Min(0f)] public float MarchSleepConsumptionPerHour = 8f;
        [Min(0f)] public float MarchWaterConsumptionPerHour = 6f;
        [Min(0f)] public float MarchFoodConsumptionPerHour = 5f;

        [Header("Needs — Resting (per game hour)")]
        [Tooltip("Sleep restored while resting.")]
        [Min(0f)] public float RestSleepRestorePerHour = 12f;
        [Min(0f)] public float RestWaterConsumptionPerHour = 2f;
        [Min(0f)] public float RestFoodConsumptionPerHour = 2f;

        [Header("Needs — Camped / Idle (per game hour)")]
        [Min(0f)] public float IdleSleepConsumptionPerHour = 2f;
        [Min(0f)] public float IdleWaterConsumptionPerHour = 2f;
        [Min(0f)] public float IdleFoodConsumptionPerHour = 2f;

        [Header("Trail Experiment")]
        public TrailMode TrailMode = TrailMode.None;

        [Tooltip("Half-width in world units used later by border/crossing trail providers.")]
        [Min(0.01f)] public float TrailSampleHalfWidth = 0.35f;

        public float GetMovementModifier(TravelSurfaceType type)
        {
            switch (type)
            {
                case TravelSurfaceType.Forest: return ForestMovementModifier;
                case TravelSurfaceType.Mountain: return MountainMovementModifier;
                case TravelSurfaceType.Lake: return LakeMovementModifier;
                case TravelSurfaceType.RoadTrail: return RoadTrailMovementModifier;
                case TravelSurfaceType.Grassland: return GrasslandMovementModifier;
                default: return GrasslandMovementModifier;
            }
        }

        public bool IsPassable(TravelSurfaceType type)
        {
            switch (type)
            {
                case TravelSurfaceType.Forest: return ForestPassable;
                case TravelSurfaceType.Mountain: return MountainPassable;
                case TravelSurfaceType.Lake: return LakePassable;
                case TravelSurfaceType.RoadTrail: return RoadTrailPassable;
                case TravelSurfaceType.Grassland: return GrasslandPassable;
                default: return true;
            }
        }

        public float GetNeedsModifier(TravelSurfaceType type)
        {
            if (!EnableDirectTerrainNeedsPenalty)
                return 1f;

            switch (type)
            {
                case TravelSurfaceType.Forest: return ForestNeedsModifier;
                case TravelSurfaceType.Mountain: return MountainNeedsModifier;
                case TravelSurfaceType.Lake: return LakeNeedsModifier;
                case TravelSurfaceType.RoadTrail: return RoadTrailNeedsModifier;
                case TravelSurfaceType.Grassland: return GrasslandNeedsModifier;
                default: return 1f;
            }
        }
    }
}
