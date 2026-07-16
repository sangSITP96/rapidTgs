using TGS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class TileSingleHighlight : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private Camera _camera;

    [Header("Highlight Settings")]
    [SerializeField] private Color _highlightColor = new Color(1, 1, 0.8f, 0.6f);
    [FormerlySerializedAs("_hightlightDuration")] [SerializeField] private float _highlightDuration = 0.5f;
    [SerializeField] private bool _fadeOut = true;

    [Header("Input")]
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
            _terrainGridSystem = FindObjectOfType<TerrainGridSystem>();
    }

    private void OnEnable()
    {
        //TGSViewportSync.GridSynced += HandleGridSynced;
    }

    private void OnDisable()
    {
        //TGSViewportSync.GridSynced -= HandleGridSynced;
    }

    private void Start()
    {
        if (_terrainGridSystem != null)
            _terrainGridSystem.highlightMode = HighlightMode.None;
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
                TryHighlightAtWorld(hit.point);
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
        return Physics.Raycast(ray, out hit);
    }

    private void TryHighlightAtWorld(Vector3 worldPoint)
    {
        if (_terrainGridSystem == null)
            return;

        var cellSelected = _terrainGridSystem.CellGetAtPosition(worldPoint, true);
        int cellIndex = _terrainGridSystem.CellGetIndex(cellSelected);

        if (cellIndex < 0)
            return;

        HighlightCell(cellIndex);
    }

    private void HandleGridSynced()
    {
        if (_currentHighlightCell < 0)
            return;

        ClearHighlight();
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
        if (_currentHighlightCell < 0)
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
