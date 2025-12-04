using TGS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MarbleMovement : MonoBehaviour
{
    [SerializeField] private Transform _marble;
    [SerializeField] private TerrainGridSystem _terrainGridSystem;
    [SerializeField] private Slider _speedSlider;
    //
    [SerializeField] private float _normalSpeed = 2f;

    [SerializeField] private float _superSlowSpeed = 0.0056f;
    //

    private Vector3 _targetPosition;
    private bool _moving;
    
    // Text Debug
    public Text TextShowSpeed;
    
    // phase 1B
    [SerializeField] private Renderer _groundRenderer;
    private Texture2D _heightmap;

    void Start()
    {
        _targetPosition = _marble.position;
        
        _heightmap = _groundRenderer.material.mainTexture as Texture2D;
    }

    void Update()
    {
        HandleTap();
        Move();
    }

    private void HandleTap()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _targetPosition = new Vector3(hit.point.x, _marble.position.y, hit.point.z);
                _moving = true;
            }
        }
    }

    private void Move()
    {
        if (!_moving) return;

        float tValue = _speedSlider.value;
        float baseSpeed = Mathf.Lerp(_superSlowSpeed, _normalSpeed, tValue);
        //
        Vector3 current = _marble.position;
        Vector3 toTarget = _targetPosition - current;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            _moving = false;
            return;
        }
        //
        Vector3 direction = toTarget.normalized;
        float stepDistance = baseSpeed * Time.deltaTime;
        Vector3 nextPos = current + direction * stepDistance;

        float brightness = GetBrightnessAtPosition(nextPos);

        if (IsLake(brightness))
        {
            _moving = false;
            return;
        }

        float speedMult = GetSpeedMultiplier(brightness);

        float finalSpeed = baseSpeed * speedMult;
        TextShowSpeed.text = finalSpeed.ToString();
        
        _marble.position = Vector3.MoveTowards(
            _marble.position, 
            _targetPosition, 
            finalSpeed * Time.deltaTime);

        if (Vector3.Distance(_marble.position, _targetPosition) < 0.01f)
        {
            _moving = false;
        }
    }

    public LayerMask _groundLayerMask;

    private float GetBrightnessAtPosition(Vector3 worldPos)
    {
        if (_groundRenderer == null)
        {
            Debug.LogWarning("GroundRenderer is NULL");
            return 0.5f;
        }

        Ray ray = new Ray(worldPos + Vector3.up * 1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, _groundLayerMask))
        {
            Vector2 uv = hit.textureCoord;

            if (_heightmap == null)
                _heightmap = _groundRenderer.material.mainTexture as Texture2D;

            if (_heightmap == null)
                return 0.5f;

            Color c = _heightmap.GetPixelBilinear(uv.x, uv.y);
            return c.r;
        }

        return 0.5f;
    }

    private float GetSpeedMultiplier(float brightness)
    {
        float speedMult = 1f;
        if (brightness > 0.5f)
        {
            float t = (brightness - 0.5f) / 0.5f;
            speedMult = 1f - (0.3f * t);    // -30%
        }
        else if(brightness < 0.5f)
        {
            float t = (0.5f - brightness) / 0.5f;
            speedMult = 1f + (0.15f * t); // +15%
        }
        
        return speedMult;
    }

    private bool IsLake(float brightness)
    {
        return brightness < 0.05f;
    }
}
