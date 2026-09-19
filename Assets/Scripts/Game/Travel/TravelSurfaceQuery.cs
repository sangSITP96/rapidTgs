using UnityEngine;

namespace Game.Travel
{
    public sealed class TravelSurfaceQuery : MonoBehaviour
    {
        [SerializeField] private WorldTerrainQuery _terrainQuery;
        [SerializeField] private TravelSettings _settings;

        private ITrailSurfaceProvider _trailProvider = new NoneTrailProvider();

        public TravelSettings Settings => _settings;

        private void Awake()
        {
            if (_terrainQuery == null)
                _terrainQuery = FindFirstObjectByType<WorldTerrainQuery>();

            RefreshTrailProvider();
        }

        public void SetSettings(TravelSettings settings)
        {
            _settings = settings;
            RefreshTrailProvider();
        }

        public void SetTrailProvider(ITrailSurfaceProvider provider)
        {
            _trailProvider = provider ?? new NoneTrailProvider();
        }

        public void RefreshTrailProvider()
        {
            TrailMode mode = _settings != null ? _settings.TrailMode : TrailMode.None;
            switch (mode)
            {
                case TrailMode.TerritoryBorder:
                    _trailProvider = new TerritoryBorderTrailProvider();
                    break;
                case TrailMode.TerritoryCrossing:
                    _trailProvider = new TerritoryCrossingTrailProvider();
                    break;
                default:
                    _trailProvider = new NoneTrailProvider();
                    break;
            }
        }

        public TravelSurfaceSample Sample(Vector3 worldPos)
        {
            if (_settings == null)
            {
                return new TravelSurfaceSample
                {
                    SurfaceType = TravelSurfaceType.Unknown,
                    IsPassable = true,
                    MovementModifier = 1f,
                    NeedsModifier = 1f,
                    IsTrail = false,
                    WorldPosition = worldPos
                };
            }

            if (_trailProvider != null &&
                _trailProvider.TrySampleTrail(worldPos, out _) &&
                _settings.IsPassable(TravelSurfaceType.RoadTrail))
            {
                return new TravelSurfaceSample
                {
                    SurfaceType = TravelSurfaceType.RoadTrail,
                    IsPassable = true,
                    MovementModifier = _settings.GetMovementModifier(TravelSurfaceType.RoadTrail),
                    NeedsModifier = _settings.GetNeedsModifier(TravelSurfaceType.RoadTrail),
                    IsTrail = true,
                    WorldPosition = worldPos
                };
            }

            TravelSurfaceType surface = ResolveBiomeSurface(worldPos);
            bool passable = _settings.IsPassable(surface);

            if (!passable)
                return TravelSurfaceSample.Impassable(worldPos, surface);

            return new TravelSurfaceSample
            {
                SurfaceType = surface,
                IsPassable = true,
                MovementModifier = Mathf.Max(0f, _settings.GetMovementModifier(surface)),
                NeedsModifier = _settings.GetNeedsModifier(surface),
                IsTrail = false,
                WorldPosition = worldPos
            };
        }

        private TravelSurfaceType ResolveBiomeSurface(Vector3 worldPos)
        {
            if (_terrainQuery == null)
                return TravelSurfaceType.Grassland;

            if (_terrainQuery.IsLake(worldPos) || _terrainQuery.IsMovementBlocked(worldPos))
                return TravelSurfaceType.Lake;

            if (_terrainQuery.IsMountain(worldPos))
                return TravelSurfaceType.Mountain;

            if (_terrainQuery.IsForest(worldPos))
                return TravelSurfaceType.Forest;

            if (_terrainQuery.TryGetBiomeType(worldPos, out BiomeType biome))
            {
                switch (biome)
                {
                    case BiomeType.Lake: return TravelSurfaceType.Lake;
                    case BiomeType.Mountain: return TravelSurfaceType.Mountain;
                    case BiomeType.Forest: return TravelSurfaceType.Forest;
                    default: return TravelSurfaceType.Grassland;
                }
            }

            return TravelSurfaceType.Grassland;
        }
    }
}
