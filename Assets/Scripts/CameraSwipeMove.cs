using TGS;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(10)]
public class CameraSwipeMove : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private float verticalTiles = 20f;
    [SerializeField] private float horizontalTiles = 6f;

    [SerializeField] private GameObject _marbleGameObject;

    [SerializeField] private InfiniteMapStreamer _mapStreamer;
    [SerializeField] private float _cameraBoundaryPadding = 0.05f;
    [SerializeField] private bool _focusOnMarbleAtStart = true;

    private float _tileW;
    private float _tileH;

    private const float MAP_WIDTH = 8.75f;
    private const float MAP_HEIGHT = 6.25f;

    private bool _dragging = false;
    private Vector2 _lastMousePosition;
    
    // Inertia system
    private Vector2 _velocity;
    [SerializeField] private float _inertiaDecay = 8f;
    private const float VELOCITY_THRESHOLD = 0.001f;
    
    // Velocity tracking for better inertia
    private const int VELOCITY_SAMPLES = 3;
    private Vector2[] _velocityHistory = new Vector2[VELOCITY_SAMPLES];
    private int _velocityHistoryIndex = 0;
    
    [SerializeField] private float baseTileSensitivity = 10f;
    
    private Camera _camera;

    private void Awake()
    {
        ResolveMapStreamer();
    }

    void Start()
    {
        _camera = GetComponent<Camera>();
        //
        float aspect = _camera.aspect;
    
        // Adjust tiles based on actual pan space
        float panSpaceX = MAP_WIDTH - (_camera.orthographicSize * aspect * 2);
        float panSpaceZ = MAP_HEIGHT - (_camera.orthographicSize * 2);
    
        horizontalTiles = baseTileSensitivity * (panSpaceZ / panSpaceX);
        verticalTiles = baseTileSensitivity;
        //
        
        transform.rotation = Quaternion.Euler(90f, 0, 0);

        _tileW = _terrainGridSystem.cellSize.x;
        _tileH = _terrainGridSystem.cellSize.y;

        float marbleScale = _tileW * 0.45f;
        
        _marbleGameObject.transform.localScale = new Vector3(marbleScale, marbleScale, marbleScale);

        if (_focusOnMarbleAtStart)
            FocusOnMarble();
    }

    private void ResolveMapStreamer()
    {
        if (_mapStreamer == null)
            _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();
    }

    private void FocusOnMarble()
    {
        if (_marbleGameObject == null)
            return;

        ResolveMapStreamer();

        if (_mapStreamer != null)
        {
            _mapStreamer.FocusCameraOn(_marbleGameObject.transform.position);
            return;
        }

        var marblePos = _marbleGameObject.transform.position;
        var pos = transform.position;
        pos.x = marblePos.x;
        pos.z = marblePos.z;
        transform.position = pos;
        ClampCameraToMap();
    }

    void Update()
    {
        HandleMouseInput();
        ClampCameraToMap();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMousePosition = Input.mousePosition;
            _velocity = Vector2.zero;
            
            // Clear velocity history
            for (int i = 0; i < VELOCITY_SAMPLES; i++)
            {
                _velocityHistory[i] = Vector2.zero;
            }
            _velocityHistoryIndex = 0;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            
            // Calculate average velocity from recent samples for smooth inertia
            Vector2 avgVelocity = Vector2.zero;
            for (int i = 0; i < VELOCITY_SAMPLES; i++)
            {
                avgVelocity += _velocityHistory[i];
            }
            _velocity = avgVelocity / VELOCITY_SAMPLES;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            _velocity = Vector2.zero;
            _dragging = false;
            return;
        }

        if (!_dragging)
        {
            // Inertia mode
            if (_velocity.magnitude > VELOCITY_THRESHOLD)
            {
                float moveX1 = _velocity.x * (horizontalTiles * _tileW);
                float moveZ1 = _velocity.y * (verticalTiles * _tileH);
                
                transform.position -= new Vector3(moveX1, 0f, moveZ1);

                _velocity = Vector2.Lerp(_velocity, Vector2.zero, _inertiaDecay * Time.deltaTime);
            }
            else
            {
                _velocity = Vector2.zero;
            }
            return;
        }
        
        // Dragging mode - direct movement
        Vector2 current = Input.mousePosition;
        Vector2 delta = current - _lastMousePosition;

        float normalizedX = delta.x / Screen.width;
        float normalizedY = delta.y / Screen.height;
        
        // Store velocity sample for inertia calculation on release
        _velocityHistory[_velocityHistoryIndex] = new Vector2(normalizedX, normalizedY);
        _velocityHistoryIndex = (_velocityHistoryIndex + 1) % VELOCITY_SAMPLES;
        
        // Apply movement directly - no smoothing during drag
        float moveX = normalizedX * (horizontalTiles * _tileW);
        float moveZ = normalizedY * (verticalTiles * _tileH);
        
        transform.position -= new Vector3(moveX, 0, moveZ);
        _lastMousePosition = current;
    }

    void ClampCameraToMap()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();

        ResolveMapStreamer();
        if (_camera == null || _mapStreamer == null)
            return;

        Vector3 pos = transform.position;
        Vector3 oldPos = pos;
        pos = _mapStreamer.ClampCameraPosition(pos, _camera, _cameraBoundaryPadding);
        transform.position = pos;

        if (!_dragging)
        {
            if (!Mathf.Approximately(pos.x, oldPos.x)) _velocity.x = 0f;
            if (!Mathf.Approximately(pos.z, oldPos.z)) _velocity.y = 0f;
        }
    }
}
