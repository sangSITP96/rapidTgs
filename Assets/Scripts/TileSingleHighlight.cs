using TGS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class TileSingleHighlight : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private Camera _camera;
    [SerializeField] private InfiniteMapStreamer _mapStreamer;

    [Header("Highlight Settings")]
    [SerializeField] private Color _highlightColor = new Color(1, 1, 0.8f, 0.6f);
    [FormerlySerializedAs("_hightlightDuration")] [SerializeField] private float _highlightDuration = 0.5f;
    [SerializeField] private bool _fadeOut = true;
    [SerializeField] private bool _keepHighlightUntilNextClick;

    [Header("Chunk Grid Sync")]
    [SerializeField] private bool _recenterGridToClickedChunk = true;

    [Header("Input")]
    [SerializeField] private LayerMask _groundLayer = 1 << 6;
    [SerializeField] private float _clickDragThresholdPixels = 12f;

    private int _currentHighlightCell = -1;
    private float _highlightStartTime;
    private Vector2 _pointerDownScreenPos;
    private bool _pointerDownOnGround;

    void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_terrainGridSystem == null)
            _terrainGridSystem = FindFirstObjectByType<TerrainGridSystem>();

        if (_mapStreamer == null)
            _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();

    }

    private void OnEnable()
    {
        SubscribeToStreamer();
    }

    private void OnDisable()
    {
        if (_mapStreamer != null)
            _mapStreamer.LoadedChunksChanged -= HandleLoadedChunksChanged;
    }

    private void Start()
    {
        if (_terrainGridSystem != null)
        {
            _terrainGridSystem.highlightMode = HighlightMode.None;
            _terrainGridSystem.showCells = false;
        }

        SubscribeToStreamer();
    }

    private void Update()
    {
        HandlePointerInput();
        UpdateHighlightFade();
    }

    private void HandlePointerInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
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
                TryHighlightAtWorld(hit);
            }

            _pointerDownOnGround = false;
        }
    }

    private bool TryRaycastGround(Vector2 screenPos, out RaycastHit hit)
    {
        hit = default;
        if (_camera == null || _terrainGridSystem == null)
            return false;

        Ray ray = _camera.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            Mathf.Infinity,
            _groundLayer,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];

            if (candidate.collider.GetComponentInParent<MapChunkRuntime>() == null)
                continue;

            if (candidate.distance >= nearestDistance)
            {
                continue;
            }

            hit = candidate;
            nearestDistance = candidate.distance;
            found = true;
        }

        return found;
    }

    private void TryHighlightAtWorld(RaycastHit hit)
    {
        if (_terrainGridSystem == null)
            return;

        MapChunkRuntime chunk = hit.collider.GetComponentInParent<MapChunkRuntime>();

        if (chunk == null)
            return;

        if (_recenterGridToClickedChunk)
            RecenterGridToChunk(chunk.Coord);

        Cell cellSelected = _terrainGridSystem.CellGetAtPosition(hit.point, true);
        int cellIndex = _terrainGridSystem.CellGetIndex(cellSelected);

        if (cellIndex < 0)
            return;

        HighlightCell(cellIndex);
    }

    private void SubscribeToStreamer()
    {
        if (_mapStreamer == null)
            _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();

        if (_mapStreamer == null)
            return;

        _mapStreamer.LoadedChunksChanged -= HandleLoadedChunksChanged;
        _mapStreamer.LoadedChunksChanged += HandleLoadedChunksChanged;
    }

    private void HandleLoadedChunksChanged()
    {
        ClearHighlight();
    }

    private void RecenterGridToChunk(Vector2Int chunkCoord)
    {
        if (_terrainGridSystem == null ||
            _mapStreamer == null ||
            !_mapStreamer.TryGetChunkWorldBounds(
                chunkCoord,
                out float minX,
                out float maxX,
                out float minZ,
                out float maxZ))
        {
            return;
        }

        Vector3 center = new Vector3(
            (minX + maxX) * 0.5f,
            _terrainGridSystem.transform.position.y,
            (minZ + maxZ) * 0.5f);

        _terrainGridSystem.SetGridCenterWorldPosition(center, false);
    }

    private void HighlightCell(int cellIndex)
    {
        if (_currentHighlightCell >= 0)
            ClearHighlight();

        _terrainGridSystem.CellToggleRegionSurface(cellIndex, true, _highlightColor);
        _currentHighlightCell = cellIndex;
        _highlightStartTime = Time.time;
    }

    private void UpdateHighlightFade()
    {
        if (_currentHighlightCell < 0 || _keepHighlightUntilNextClick)
            return;

        float elapsed = Time.time - _highlightStartTime;

        if (elapsed >= _highlightDuration)
        {
            ClearHighlight();
        }
        else if (_fadeOut)
        {
            float fadeProgress = elapsed / _highlightDuration;
            Color fadeColor = _highlightColor;
            fadeColor.a = _highlightColor.a * (1f - fadeProgress);

            _terrainGridSystem.CellSetColor(_currentHighlightCell, fadeColor);
        }
    }

    private void ClearHighlight()
    {
        if (_currentHighlightCell < 0)
            return;

        _terrainGridSystem.CellHideRegionSurface(_currentHighlightCell);
        _currentHighlightCell = -1;
    }

    public void ClearCurrentHighlight()
    {
        ClearHighlight();
    }
}
