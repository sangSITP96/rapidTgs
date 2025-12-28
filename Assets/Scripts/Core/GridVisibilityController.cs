using TGS;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridVisibilityController : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;

    [Header("Visibility Settings")] 
    [SerializeField] private bool _hideByDefault = true;

    [SerializeField] private float _autoHideDelay = 2.5f;

    [Header("Trigger Modes")]
    [SerializeField] private bool _showOnTerrainClick = true;

    [SerializeField] private bool _showOnMarbleMovement = true;
    [SerializeField] private KeyCode _manualToggleKey = KeyCode.G;

    private float _lastInteractionTime;
    private bool _isGridVisible;
    
    private Camera _camera;

    void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        if (_terrainGridSystem == null)
        {
            _terrainGridSystem = FindObjectOfType<TerrainGridSystem>();
        }

        // if (_hideByDefault)
        // {
        //     HideGrid();
        // }
        // else
        // {
        //     ShowGrid();
        // }
    }

    private void Update()
    {
        //HandleManualTogle();
        //HandleTerrainClick();
        //HandleAutoHide();
    }

    private void HandleManualTogle()
    {
        if (Input.GetKeyDown(_manualToggleKey))
        {
            ToggleGrid();
        }
    }

    private void HandleTerrainClick()
    {
        if (!_showOnTerrainClick) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ShowGrid();
                _lastInteractionTime = Time.time;
            }
        }
    }

    private void HandleAutoHide()
    {
        if (!_isGridVisible) return;

        if (Time.time - _lastInteractionTime > _autoHideDelay)
        {
            HideGrid();
        }
    }

    public void ToggleGrid()
    {
        if (_isGridVisible)
        {
            HideGrid();
        }
        else
        {
            ShowGrid();
            _lastInteractionTime = Time.time;
        }
    }

    public void ShowGrid()
    {
        //_terrainGridSystem.showCells = true;
        _isGridVisible = true;
    }

    public void HideGrid()
    {
        _terrainGridSystem.showCells = false;
        _isGridVisible = false;
    }

    public void OnMarbleMovementStarted()
    {
        if (_showOnMarbleMovement)
        {
            ShowGrid();
            _lastInteractionTime = Time.time;
        }
    }
}
