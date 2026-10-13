using Game.Navigation;
using Game.UI;
using TGS;
using UnityEngine;

namespace Game.Campaign
{
    /// <summary>
    /// Thin Colony Map hook: tap cell → territoryIndex → Campaign Offer (Offered state only).
    /// Does not create a Territory Selection System. Skips while Plan March mode is active.
    /// Lake cells/territories cannot be Campaign destinations.
    /// </summary>
    [DefaultExecutionOrder(5)]
    public sealed class CampaignMapOfferHook : MonoBehaviour
    {
        [SerializeField] private CampaignService _campaignService;
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private TgsNavigationPathfinder _pathfinder;
        [SerializeField] private TgsBiomeMapData _biomeMapData;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _groundLayer = 1 << 6;
        [SerializeField] private float _clickDragThresholdPixels = 12f;
        [SerializeField] private bool _enabled;

        private Vector2 _pointerDownScreenPos;
        private bool _pointerDownOnGround;

        public bool HookEnabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void Awake()
        {
            ResolveRefs();
            // Off by default — Develop Mode → Campaign Debug enables this.
            _enabled = false;
        }

        private void Update()
        {
            if (!_enabled)
                return;

            if (NavigationController.IsDestinationSelectModeActive)
                return;

            if (UiPointerUtility.IsPointerOverUi())
                return;

            HandlePointer();
        }

        private void ResolveRefs()
        {
            if (_campaignService == null)
                _campaignService = FindFirstObjectByType<CampaignService>();
            if (_tgs == null)
                _tgs = FindFirstObjectByType<TerrainGridSystem>();
            if (_pathfinder == null)
                _pathfinder = FindFirstObjectByType<TgsNavigationPathfinder>();
            if (_camera == null)
                _camera = Camera.main;
            if (_biomeMapData == null)
            {
                var gen = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (gen != null)
                    _biomeMapData = gen.MapData;
            }
        }

        private void HandlePointer()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _pointerDownScreenPos = Input.mousePosition;
                _pointerDownOnGround = TryRaycastGround(Input.mousePosition, out _);
            }

            if (Input.GetMouseButton(0) && _pointerDownOnGround)
            {
                if (Vector2.Distance(_pointerDownScreenPos, Input.mousePosition) > _clickDragThresholdPixels)
                    _pointerDownOnGround = false;
            }

            if (Input.GetMouseButtonUp(0) && _pointerDownOnGround)
            {
                if (Vector2.Distance(_pointerDownScreenPos, Input.mousePosition) <= _clickDragThresholdPixels &&
                    TryRaycastGround(Input.mousePosition, out RaycastHit hit))
                {
                    TryOfferAtHit(hit);
                }

                _pointerDownOnGround = false;
            }
        }

        private void TryOfferAtHit(RaycastHit hit)
        {
            ResolveRefs();
            if (_campaignService == null || _tgs == null)
                return;

            // Match TileSingleHighlight: recenter grid to clicked chunk before cell lookup
            // so territoryIndex matches the visible map under the cursor.
            MapChunkRuntime chunk = hit.collider.GetComponentInParent<MapChunkRuntime>();
            if (chunk != null)
                RecenterGridToChunkIfPossible(chunk);

            Cell cell = _tgs.CellGetAtPosition(hit.point, true);
            if (cell == null)
                return;

            int territoryIndex = cell.territoryIndex;
            if (territoryIndex < 0)
                return;

            int cellIndex = _tgs.CellGetIndex(cell);
            if (IsLakeBlocked(cellIndex, territoryIndex))
            {
                Debug.LogWarning("[Campaign] Lake is impassable — cannot offer Campaign on Lake.");
                return;
            }

            Campaign existing = _campaignService.ActiveCampaign;
            if (existing != null &&
                existing.LifecycleState != CampaignLifecycleState.Completed &&
                existing.OwnerResponse == CampaignResponse.Accepted)
            {
                // Campaign already accepted / in progress — ignore new map offers.
                return;
            }

            // Pass exact click world position for March — do not use territory centroid later.
            Vector3 destination = hit.point;
            if (cellIndex >= 0)
                destination = _tgs.CellGetPosition(cellIndex);

            _campaignService.OfferAtTerritory(territoryIndex, destination);
        }

        private bool IsLakeBlocked(int cellIndex, int territoryIndex)
        {
            if (_pathfinder != null && cellIndex >= 0 && _pathfinder.IsLakeCell(cellIndex))
                return true;

            if (_biomeMapData != null &&
                _biomeMapData.GetBiomeForTerritory(territoryIndex) == BiomeType.Lake)
                return true;

            return false;
        }

        private void RecenterGridToChunkIfPossible(MapChunkRuntime chunk)
        {
            var streamer = FindFirstObjectByType<InfiniteMapStreamer>();
            if (streamer == null || chunk == null)
                return;

            if (!streamer.TryGetChunkWorldBounds(
                    chunk.Coord,
                    out float minX,
                    out float maxX,
                    out float minZ,
                    out float maxZ))
            {
                return;
            }

            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                _tgs.transform.position.y,
                (minZ + maxZ) * 0.5f);
            _tgs.SetGridCenterWorldPosition(center, false);
        }

        private bool TryRaycastGround(Vector2 screenPos, out RaycastHit hit)
        {
            hit = default;
            if (_camera == null)
                return false;

            Ray ray = _camera.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                Mathf.Infinity,
                _groundLayer,
                QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.GetComponentInParent<MapChunkRuntime>() == null)
                    continue;
                if (hits[i].distance >= nearest)
                    continue;
                hit = hits[i];
                nearest = hits[i].distance;
                found = true;
            }

            return found;
        }
    }
}
