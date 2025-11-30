using TGS;
using UnityEngine;

public class CameraSwipeMove : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private float verticalTiles = 20f;
    [SerializeField] private float horizontalTiles = 6f;

    private Vector2 _fingerStart;
    private bool _swiping;

    private float _tileW;
    private float _tileH;

    private const float MAP_WIDTH = 8.75f;
    private const float MAP_HEIGHT = 6.25f;

    private bool _dragging = false;
    private Vector2 _lastMousePosition;
    
    Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
        
        transform.rotation = Quaternion.Euler(90f, 0, 0);

        _tileW = _terrainGridSystem.cellSize.x;
        _tileH = _terrainGridSystem.cellSize.y;
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
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
        }

        if (!_dragging) return;
        
        Vector2 current = Input.mousePosition;
        Vector2 delta = current - _lastMousePosition;

        float normalizedX = delta.x / Screen.width;
        float normalizedY = delta.y / Screen.height;
        
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
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }
}
