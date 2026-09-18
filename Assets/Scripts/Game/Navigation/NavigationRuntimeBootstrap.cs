using UnityEngine;
using Game.Travel;

namespace Game.Navigation
{
    /// <summary>
    /// Ensures Phase 13 Navigation components exist at runtime for RapidTgsPrototype_main.
    /// Attach to Marble (or any scene object). Creates pathfinder + controller if missing.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class NavigationRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _createIfMissing = true;
        [SerializeField] private TgsBiomeMapData _biomeMapDataOverride;

        [Header("March Waypoints (applied to pathfinder on Awake)")]
        [Tooltip("1 = every path cell (required to avoid cutting lakes). Higher only skips when land-safe.")]
        [SerializeField, Min(1)] private int _waypointStride = 1;

        [Tooltip("Optional spacing filter; 0 recommended so march stays on the cell path.")]
        [SerializeField, Min(0f)] private float _minWaypointSpacing = 0f;

        private void Awake()
        {
            if (!_createIfMissing)
                return;

            NavigationController controller = FindFirstObjectByType<NavigationController>();
            TgsNavigationPathfinder pathfinder = FindFirstObjectByType<TgsNavigationPathfinder>();

            if (controller == null)
            {
                pathfinder = gameObject.GetComponent<TgsNavigationPathfinder>() ??
                             gameObject.AddComponent<TgsNavigationPathfinder>();
                controller = gameObject.AddComponent<NavigationController>();
            }
            else if (pathfinder == null)
            {
                pathfinder = controller.GetComponent<TgsNavigationPathfinder>() ??
                             controller.gameObject.AddComponent<TgsNavigationPathfinder>();
            }

            TgsBiomeMapData mapData = _biomeMapDataOverride;
            if (mapData == null)
            {
                var generator = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (generator != null)
                    mapData = generator.MapData;
            }

            if (mapData != null)
                pathfinder.SetBiomeMapData(mapData);

            pathfinder.ConfigureWaypointSampling(_waypointStride, _minWaypointSpacing);
            pathfinder.ResolveRefs();
            controller.ResolveRefs();
            pathfinder.SyncTraversalFromBiomes(force: true);
        }

        private void Start()
        {
            // After InfiniteMapStreamer spawn: keep marble off lake (bake + TGS biome).
            var streamer = FindFirstObjectByType<InfiniteMapStreamer>();
            if (streamer != null)
                streamer.EnsureMarbleOnLand();
            else
                EnsureTroopOffLakeFallback();
        }

        private static void EnsureTroopOffLakeFallback()
        {
            var travel = FindFirstObjectByType<TroopTravelController>();
            Transform troop = travel != null ? travel.transform : null;
            if (troop == null)
                return;

            var terrain = FindFirstObjectByType<WorldTerrainQuery>();
            if (terrain == null)
                return;

            if (!terrain.IsMovementBlocked(troop.position) && !terrain.IsLake(troop.position))
                return;

            if (terrain.TryFindNearestLandPosition(
                    troop.position,
                    maxRadius: 4f,
                    ringCount: 20,
                    samplesPerRing: 24,
                    out Vector3 landPos))
            {
                landPos.y = troop.position.y;
                troop.position = landPos;
                Debug.Log($"{nameof(NavigationRuntimeBootstrap)}: Moved troop off lake to {landPos}.");
            }
        }
    }
}
