using TGS;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraSwipeMove : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private float verticalTiles = 20f;
    [SerializeField] private float horizontalTiles = 6f;

    [SerializeField] private GameObject _marbleGameObject;

    private Vector2 _fingerStart;
    private bool _swiping;

    private float _tileW;
    private float _tileH;

    private const float MAP_WIDTH = 8.75f;
    private const float MAP_HEIGHT = 6.25f;

    private bool _dragging = false;
    private Vector2 _lastMousePosition;
    
    // Inertia system
    private Vector2 _velocity;

    [SerializeField] private float _inertiaDecay = 8f;
    [SerializeField] [Range(0f, 1f)] private float _velocitySmoothing = 0.3f; // NEW: Smoothing factor
    private const float VELOCITY_THRESHOLD = 0.001f;
    
    Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
        
        transform.rotation = Quaternion.Euler(90f, 0, 0);

        _tileW = _terrainGridSystem.cellSize.x;
        _tileH = _terrainGridSystem.cellSize.y;

        float marbleScale = _tileW * 0.45f;
        
        _marbleGameObject.transform.localScale = new Vector3(marbleScale, marbleScale, marbleScale);
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
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            _velocity = Vector2.zero;
            return;
        }

        if (!_dragging)
        {
            if (_velocity.magnitude > VELOCITY_THRESHOLD)
            {
                float moveX1 = _velocity.x * (horizontalTiles * _tileW);
                float moveZ1 = _velocity.y * (verticalTiles * _tileH);
                
                transform.position -= new Vector3(moveX1, 0f , moveZ1);

                _velocity = Vector2.Lerp(_velocity, Vector2.zero, _inertiaDecay * Time.deltaTime);
            }
            else
            {
                _velocity = Vector2.zero;
            }
            return;
        }
        
        Vector2 current = Input.mousePosition;
        Vector2 delta = current - _lastMousePosition;

        float normalizedX = delta.x / Screen.width;
        float normalizedY = delta.y / Screen.height;
        
        // FIX: Use smoothing instead of direct assignment
        // This prevents velocity from dropping to 0 when finger momentarily stops
        Vector2 targetVelocity = new Vector2(normalizedX, normalizedY);
        _velocity = Vector2.Lerp(_velocity, targetVelocity, _velocitySmoothing);
        
        float moveX = normalizedX * (horizontalTiles * _tileW);
        float moveZ = normalizedY * (verticalTiles * _tileH);
        
        transform.position -= new Vector3(moveX, 0 , moveZ);
        _lastMousePosition = current;
    }

    void ClampCameraToMap()
    {
        float halfHeight = _camera.orthographicSize;
        float halfWidth = _camera.orthographicSize * _camera.aspect;
    
        float mapLeft = -MAP_WIDTH / 2;
        float mapRight = MAP_WIDTH / 2;
        float mapBottom = -MAP_HEIGHT / 2;
        float mapTop = MAP_HEIGHT / 2;

        float minX = mapLeft + halfWidth;
        float maxX = mapRight - halfWidth;
        float minZ = mapBottom + halfHeight;
        float maxZ = mapTop - halfHeight;

        Vector3 pos = transform.position;
    
        // Track if position was clamped
        bool clampedX = false;
        bool clampedZ = false;
    
        // Clamp X
        if (pos.x < minX)
        {
            pos.x = minX;
            clampedX = true;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            clampedX = true;
        }
    
        // Clamp Z
        if (pos.z < minZ)
        {
            pos.z = minZ;
            clampedZ = true;
        }
        else if (pos.z > maxZ)
        {
            pos.z = maxZ;
            clampedZ = true;
        }
    
        transform.position = pos;
    
        // FIX: Only clear velocity when NOT dragging
        // When dragging, user is in direct control, don't interfere with velocity
        if (!_dragging)
        {
            // Clear velocity when hitting boundary to prevent sticky feeling
            if (clampedX)
            {
                _velocity.x = 0f;
            }
            if (clampedZ)
            {
                _velocity.y = 0f;
            }
        }
    }
}
