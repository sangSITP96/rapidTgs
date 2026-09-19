using System;
using System.Collections.Generic;
using Game.Travel;
using Game.UI;
using TGS;
using UnityEngine;

namespace Game.Navigation
{
    [DefaultExecutionOrder(-20)]
    public sealed class NavigationController : MonoBehaviour
    {
        public static NavigationController Instance { get; private set; }

        public static bool IsDestinationSelectModeActive =>
            Instance != null && Instance._destinationSelectMode;

        [Header("Refs")]
        [SerializeField] private TgsNavigationPathfinder _pathfinder;
        [SerializeField] private TroopTravelController _travel;
        [SerializeField] private Transform _troop;
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private TgsBiomeMapData _biomeMapData;
        [SerializeField] private Camera _camera;
        [SerializeField] private InfiniteMapStreamer _mapStreamer;

        [Header("Destination Select")]
        [SerializeField] private bool _destinationSelectMode;
        [SerializeField] private KeyCode _toggleModeKey = KeyCode.N;
        [SerializeField] private LayerMask _groundLayer = 1 << 6;
        [SerializeField] private float _clickDragThresholdPixels = 12f;

        [Header("Debug Visual")]
        [SerializeField] private Color _destinationHighlightColor = new Color(1f, 0.85f, 0.2f, 0.65f);
        [SerializeField] private Color _pathHighlightColor = new Color(0.2f, 0.75f, 1f, 0.45f);
        [SerializeField] private bool _highlightPathCells = true;

        private NavigationRoute _pendingRoute;
        private bool _awaitingApproval;
        private int _highlightedDestinationCell = -1;
        private readonly List<int> _highlightedPathCells = new List<int>(256);

        private Vector2 _pointerDownScreenPos;
        private bool _pointerDownOnGround;
        private int _pointerFingerId = -1;

        public bool DestinationSelectMode => _destinationSelectMode;
        public bool IsAwaitingApproval => _awaitingApproval;
        public NavigationRoute PendingRoute => _pendingRoute;

        public event Action<bool> DestinationSelectModeChanged;
        public event Action<NavigationRoute> RoutePendingApproval;
        public event Action<NavigationRoute> RouteRequestFailed;
        public event Action RouteApprovalCancelled;
        public event Action<NavigationRoute> RouteApproved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{nameof(NavigationController)}: duplicate instance on {name}.");
            }

            Instance = this;
            ResolveRefs();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            ResolveRefs();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleModeKey))
            {
                if (!_destinationSelectMode)
                    UiOverlayExclusive.Instance?.PrepareForPlanMarch();

                SetDestinationSelectMode(!_destinationSelectMode);
            }

            if (!_destinationSelectMode)
                return;

            if (UiOverlayExclusive.IsPlanMarchBlocked)
                return;

            if (_awaitingApproval)
                return;

            if (UiPointerUtility.IsPointerOverUi())
                return;

            HandleDestinationPointer();
        }

        private static bool IsPointerOverGameUi()
        {
            return UiPointerUtility.IsPointerOverUi();
        }

        public void ResolveRefs()
        {
            if (_pathfinder == null)
                _pathfinder = GetComponent<TgsNavigationPathfinder>() ?? FindFirstObjectByType<TgsNavigationPathfinder>();
            if (_travel == null)
                _travel = FindFirstObjectByType<TroopTravelController>();
            if (_troop == null && _travel != null)
                _troop = _travel.transform;
            if (_tgs == null)
                _tgs = FindFirstObjectByType<TerrainGridSystem>();
            if (_camera == null)
                _camera = Camera.main;
            if (_mapStreamer == null)
                _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();

            if (_biomeMapData == null)
            {
                var generator = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (generator != null)
                    _biomeMapData = generator.MapData;
            }

            if (_pathfinder != null && _biomeMapData != null)
                _pathfinder.SetBiomeMapData(_biomeMapData);
        }

        public void SetDestinationSelectMode(bool active)
        {
            if (active)
                UiOverlayExclusive.Instance?.PrepareForPlanMarch();

            if (_destinationSelectMode == active)
                return;

            _destinationSelectMode = active;
            DestinationSelectModeChanged?.Invoke(active);

            if (!active)
            {
                ClearPendingApproval(clearHighlights: true);
            }

            Debug.Log($"{nameof(NavigationController)}: Destination select mode = {active}");
        }

        public void ToggleDestinationSelectMode()
        {
            SetDestinationSelectMode(!_destinationSelectMode);
        }

        public void RequestRouteToWorld(Vector3 destinationWorld)
        {
            ResolveRefs();

            if (_pathfinder == null)
            {
                Debug.LogWarning($"{nameof(NavigationController)}: Pathfinder missing.");
                return;
            }

            Vector3 origin = _troop != null ? _troop.position : transform.position;
            NavigationRoute route = _pathfinder.FindRouteToWorld(origin, destinationWorld);

            ClearPathHighlights();

            int highlightCell = route != null && route.DestinationCellIndex >= 0
                ? route.DestinationCellIndex
                : _pathfinder.TryGetCellIndexAtWorld(destinationWorld);
            HighlightDestination(highlightCell);

            if (route == null || !route.IsValid)
            {
                _pendingRoute = route;
                _awaitingApproval = false;
                Debug.LogWarning(
                    $"{nameof(NavigationController)}: Route failed — " +
                    $"{(route != null ? route.FailureReason : "null")}");
                RouteRequestFailed?.Invoke(route);
                return;
            }

            _pendingRoute = route;
            _awaitingApproval = true;
            if (_highlightPathCells)
                HighlightPath(route);

            RoutePendingApproval?.Invoke(route);
        }

        public void RequestRouteToCell(int destinationCellIndex)
        {
            ResolveRefs();

            if (_pathfinder == null)
            {
                Debug.LogWarning($"{nameof(NavigationController)}: Pathfinder missing.");
                return;
            }

            Vector3 origin = _troop != null ? _troop.position : transform.position;
            NavigationRoute route = _pathfinder.FindRoute(origin, destinationCellIndex);

            ClearPathHighlights();
            HighlightDestination(destinationCellIndex);

            if (route == null || !route.IsValid)
            {
                _pendingRoute = route;
                _awaitingApproval = false;
                Debug.LogWarning(
                    $"{nameof(NavigationController)}: Route failed — " +
                    $"{(route != null ? route.FailureReason : "null")}");
                RouteRequestFailed?.Invoke(route);
                return;
            }

            _pendingRoute = route;
            _awaitingApproval = true;
            if (_highlightPathCells)
                HighlightPath(route);

            RoutePendingApproval?.Invoke(route);
        }

        public void ApprovePendingRoute()
        {
            if (!_awaitingApproval || _pendingRoute == null || !_pendingRoute.IsValid)
            {
                Debug.LogWarning($"{nameof(NavigationController)}: No valid pending route to approve.");
                return;
            }

            ResolveRefs();
            if (_travel == null)
            {
                Debug.LogWarning($"{nameof(NavigationController)}: TroopTravelController missing.");
                return;
            }

            NavigationRoute approved = _pendingRoute;
            _awaitingApproval = false;
            _pendingRoute = null;

            _travel.BeginTravel(approved);
            RouteApproved?.Invoke(approved);

            SetDestinationSelectMode(false);
            ClearHighlights();
        }

        public void CancelPendingRoute()
        {
            ClearPendingApproval(clearHighlights: true);
            RouteApprovalCancelled?.Invoke();
        }

        private void ClearPendingApproval(bool clearHighlights)
        {
            _awaitingApproval = false;
            _pendingRoute = null;
            if (clearHighlights)
                ClearHighlights();
        }

        private void HandleDestinationPointer()
        {
            if (_camera == null || _pathfinder == null)
                return;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (IsPointerOverGameUi())
                        return;

                    _pointerFingerId = touch.fingerId;
                    _pointerDownScreenPos = touch.position;
                    _pointerDownOnGround = TryRaycastGround(touch.position, out _);
                }
                else if (touch.fingerId == _pointerFingerId)
                {
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        if (_pointerDownOnGround &&
                            Vector2.Distance(_pointerDownScreenPos, touch.position) > _clickDragThresholdPixels)
                        {
                            _pointerDownOnGround = false;
                        }
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (IsPointerOverGameUi())
                        {
                            _pointerDownOnGround = false;
                            _pointerFingerId = -1;
                            return;
                        }

                        if (_pointerDownOnGround &&
                            Vector2.Distance(_pointerDownScreenPos, touch.position) <= _clickDragThresholdPixels &&
                            TryRaycastGround(touch.position, out RaycastHit touchHit))
                        {
                            int cellIndex = _pathfinder.TryGetCellIndexAtWorld(touchHit.point);
                            if (cellIndex >= 0)
                                RequestRouteToWorld(touchHit.point);
                        }

                        _pointerDownOnGround = false;
                        _pointerFingerId = -1;
                    }
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverGameUi())
                {
                    _pointerDownOnGround = false;
                    return;
                }

                _pointerDownScreenPos = Input.mousePosition;
                _pointerDownOnGround = TryRaycastGround(Input.mousePosition, out _);
            }

            if (Input.GetMouseButton(0) && _pointerDownOnGround)
            {
                float drag = Vector2.Distance(_pointerDownScreenPos, Input.mousePosition);
                if (drag > _clickDragThresholdPixels)
                    _pointerDownOnGround = false;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (IsPointerOverGameUi())
                {
                    _pointerDownOnGround = false;
                    return;
                }

                if (_pointerDownOnGround)
                {
                    float drag = Vector2.Distance(_pointerDownScreenPos, Input.mousePosition);
                    if (drag <= _clickDragThresholdPixels &&
                        TryRaycastGround(Input.mousePosition, out RaycastHit hit))
                    {
                    int cellIndex = _pathfinder.TryGetCellIndexAtWorld(hit.point);
                    if (cellIndex >= 0)
                        RequestRouteToWorld(hit.point);
                    }
                }

                _pointerDownOnGround = false;
            }
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
                RaycastHit candidate = hits[i];
                if (_mapStreamer != null &&
                    candidate.collider.GetComponentInParent<MapChunkRuntime>() == null)
                {
                    continue;
                }

                if (candidate.distance >= nearest)
                    continue;

                hit = candidate;
                nearest = candidate.distance;
                found = true;
            }

            if (!found && Physics.Raycast(ray, out hit, Mathf.Infinity, _groundLayer))
                found = true;

            return found;
        }

        private void HighlightDestination(int cellIndex)
        {
            if (_tgs == null)
                _tgs = _pathfinder != null ? _pathfinder.Tgs : FindFirstObjectByType<TerrainGridSystem>();

            if (_tgs == null || cellIndex < 0)
                return;

            if (_highlightedDestinationCell >= 0 && _highlightedDestinationCell != cellIndex)
                _tgs.CellHideRegionSurface(_highlightedDestinationCell);

            _tgs.CellToggleRegionSurface(
                cellIndex,
                true,
                _destinationHighlightColor,
                refreshGeometry: false,
                texture: null,
                textureScale: Vector2.one,
                textureOffset: Vector2.zero,
                textureRotation: 0f,
                overlay: true,
                localSpace: false,
                isCanvasTexture: false);
            _highlightedDestinationCell = cellIndex;
        }

        private void HighlightPath(NavigationRoute route)
        {
            if (_tgs == null || route == null || route.CellIndices == null)
                return;

            ClearPathHighlights();

            for (int i = 0; i < route.CellIndices.Count; i++)
            {
                int cellIndex = route.CellIndices[i];
                if (cellIndex == route.DestinationCellIndex)
                    continue;

                _tgs.CellToggleRegionSurface(
                    cellIndex,
                    true,
                    _pathHighlightColor,
                    refreshGeometry: false,
                    texture: null,
                    textureScale: Vector2.one,
                    textureOffset: Vector2.zero,
                    textureRotation: 0f,
                    overlay: true,
                    localSpace: false,
                    isCanvasTexture: false);
                _highlightedPathCells.Add(cellIndex);
            }
        }

        private void ClearPathHighlights()
        {
            if (_tgs == null)
                return;

            for (int i = 0; i < _highlightedPathCells.Count; i++)
                _tgs.CellHideRegionSurface(_highlightedPathCells[i]);

            _highlightedPathCells.Clear();
        }

        private void ClearHighlights()
        {
            ClearPathHighlights();

            if (_tgs != null && _highlightedDestinationCell >= 0)
            {
                _tgs.CellHideRegionSurface(_highlightedDestinationCell);
                _highlightedDestinationCell = -1;
            }
        }

    }
}
