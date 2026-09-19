using System;
using System.Collections.Generic;
using Game.Travel;
using TGS;
using UnityEngine;
using UnityEngine.EventSystems;

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

        [Header("Touch / WebGL UI")]
        [SerializeField] private bool _showTouchControls = true;
        [SerializeField] private float _touchButtonHeight = 56f;

        [Header("Approval UI (placeholder)")]
        [SerializeField] private bool _showApprovalUi = true;
        [SerializeField, Range(0.8f, 3f)] private float _overlayScale = 1.4f;
        [SerializeField] private int _baseFontSize = 18;

        private NavigationRoute _pendingRoute;
        private bool _awaitingApproval;
        private int _highlightedDestinationCell = -1;
        private readonly List<int> _highlightedPathCells = new List<int>(256);

        private Vector2 _pointerDownScreenPos;
        private bool _pointerDownOnGround;
        private int _pointerFingerId = -1;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _boxBg;

        private Rect _navUiGuiRect;

        public bool DestinationSelectMode => _destinationSelectMode;
        public bool IsAwaitingApproval => _awaitingApproval;
        public NavigationRoute PendingRoute => _pendingRoute;

        public event Action<bool> DestinationSelectModeChanged;
        public event Action<NavigationRoute> RoutePendingApproval;
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

            if (_boxBg != null)
                Destroy(_boxBg);
        }

        private void OnEnable()
        {
            ResolveRefs();
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleModeKey))
                SetDestinationSelectMode(!_destinationSelectMode);

            if (!_destinationSelectMode)
                return;

            if (IsPointerOverNavUi())
                return;

            HandleDestinationPointer();
        }

        private bool IsPointerOverNavUi()
        {
            if (_navUiGuiRect.width <= 1f || _navUiGuiRect.height <= 1f)
                return false;

            if (TryGetPointerScreenPosition(out Vector2 screenPos))
            {
                Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
                if (_navUiGuiRect.Contains(guiPos))
                    return true;
            }

            return false;
        }

        private static bool TryGetPointerScreenPosition(out Vector2 screenPos)
        {
            if (Input.touchCount > 0)
            {
                screenPos = Input.GetTouch(0).position;
                return true;
            }

            screenPos = Input.mousePosition;
            return true;
        }

        private static bool IsPointerOverGameUi()
        {
            if (EventSystem.current == null)
                return false;

            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

            return EventSystem.current.IsPointerOverGameObject();
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
                    if (IsPointerOverGameUi() || IsPointerOverNavUi())
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
                        if (_pointerDownOnGround &&
                            Vector2.Distance(_pointerDownScreenPos, touch.position) <= _clickDragThresholdPixels &&
                            TryRaycastGround(touch.position, out RaycastHit touchHit))
                        {
                            int cellIndex = _pathfinder.TryGetCellIndexAtWorld(touchHit.point);
                            if (cellIndex >= 0)
                                RequestRouteToCell(cellIndex);
                        }

                        _pointerDownOnGround = false;
                        _pointerFingerId = -1;
                    }
                }

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverGameUi() || IsPointerOverNavUi())
                    return;

                _pointerDownScreenPos = Input.mousePosition;
                _pointerDownOnGround = TryRaycastGround(Input.mousePosition, out _);
            }

            if (Input.GetMouseButton(0) && _pointerDownOnGround)
            {
                float drag = Vector2.Distance(_pointerDownScreenPos, Input.mousePosition);
                if (drag > _clickDragThresholdPixels)
                    _pointerDownOnGround = false;
            }

            if (Input.GetMouseButtonUp(0) && _pointerDownOnGround)
            {
                float drag = Vector2.Distance(_pointerDownScreenPos, Input.mousePosition);
                if (drag <= _clickDragThresholdPixels &&
                    TryRaycastGround(Input.mousePosition, out RaycastHit hit))
                {
                    int cellIndex = _pathfinder.TryGetCellIndexAtWorld(hit.point);
                    if (cellIndex >= 0)
                        RequestRouteToCell(cellIndex);
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

            _tgs.CellToggleRegionSurface(cellIndex, true, _destinationHighlightColor);
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

                _tgs.CellToggleRegionSurface(cellIndex, true, _pathHighlightColor);
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

        private void EnsureStyles(float scale)
        {
            int fontSize = Mathf.RoundToInt(_baseFontSize * scale);

            if (_boxBg == null)
            {
                _boxBg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _boxBg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
                _boxBg.Apply();
            }

            if (_boxStyle == null)
                _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _boxBg;
            _boxStyle.normal.textColor = Color.white;
            _boxStyle.fontSize = fontSize;
            _boxStyle.alignment = TextAnchor.UpperLeft;
            _boxStyle.wordWrap = true;
            _boxStyle.padding = new RectOffset(12, 12, 10, 10);

            if (_labelStyle == null)
                _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = fontSize;
            _labelStyle.wordWrap = true;

            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = Mathf.RoundToInt(fontSize * 1.05f);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;
            _buttonStyle.wordWrap = true;
            _buttonStyle.padding = new RectOffset(10, 10, 8, 8);
        }

        private void OnGUI()
        {
            if (!_showApprovalUi && !_showTouchControls)
            {
                _navUiGuiRect = default;
                return;
            }

            float dpiScale = Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f) : 1f;
            float scale = dpiScale * _overlayScale;
            EnsureStyles(scale);

            float pad = 12f * scale;
            float width = Mathf.Clamp(380f * scale, 300f, Screen.width * 0.5f);
            float btnH = Mathf.Max(44f, _touchButtonHeight) * scale;

            float top = pad;
            float left = Screen.width - width - pad;
            float contentBottom = top;

            if (_showTouchControls || _showApprovalUi)
            {
                string planLabel = _destinationSelectMode ? "Exit Plan Mode" : "Plan March";
                var planBtn = new Rect(left, top, width, btnH);
                if (GUI.Button(planBtn, planLabel, _buttonStyle))
                    ToggleDestinationSelectMode();

                contentBottom = planBtn.yMax;
                top = contentBottom + pad * 0.5f;

                string hint = _destinationSelectMode
                    ? "Tap map to set destination"
                    : "Tap Plan March, then tap destination";
                float hintH = 40f * scale;
                var hintRect = new Rect(left, top, width, hintH);
                GUI.Box(hintRect, GUIContent.none, _boxStyle);
                GUI.Label(
                    new Rect(hintRect.x + 10f, hintRect.y + 6f, hintRect.width - 20f, hintRect.height - 12f),
                    hint,
                    _labelStyle);
                contentBottom = hintRect.yMax;
                top = contentBottom + pad * 0.5f;
            }

            if (_pendingRoute != null && !_pendingRoute.IsValid && _destinationSelectMode && _showApprovalUi)
            {
                float failH = 110f * scale;
                var failRect = new Rect(left, top, width, failH);
                GUI.Box(failRect, GUIContent.none, _boxStyle);
                GUI.Label(
                    new Rect(failRect.x + 10f, failRect.y + 8f, failRect.width - 20f, failRect.height - 16f),
                    "Route Failed\n" + _pendingRoute.FailureReason,
                    _labelStyle);
                contentBottom = failRect.yMax;
                top = contentBottom + pad * 0.5f;
            }

            if (_awaitingApproval && _pendingRoute != null && _pendingRoute.IsValid && _showApprovalUi)
            {
                float panelH = 70f * scale + btnH + pad;
                var panel = new Rect(left, top, width, panelH);
                GUI.Box(panel, GUIContent.none, _boxStyle);

                string info =
                    "Route Found\n" +
                    $"Cells: {_pendingRoute.CellIndices.Count}   Dist ≈ {_pendingRoute.ApproximateDistance:0.00}";
                GUI.Label(
                    new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, 54f * scale),
                    info,
                    _labelStyle);

                float btnW = (panel.width - 30f) * 0.5f;
                float btnY = panel.yMax - btnH - 10f * scale;
                if (GUI.Button(new Rect(panel.x + 10f, btnY, btnW, btnH), "Approve March", _buttonStyle))
                    ApprovePendingRoute();

                if (GUI.Button(new Rect(panel.x + 20f + btnW, btnY, btnW, btnH), "Cancel", _buttonStyle))
                    CancelPendingRoute();

                contentBottom = panel.yMax;
            }

            _navUiGuiRect = new Rect(left - pad * 0.5f, pad * 0.5f, width + pad, contentBottom - pad * 0.5f + pad);
        }
    }
}
