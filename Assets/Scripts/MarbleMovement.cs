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

    void Start()
    {
        _targetPosition = _marble.position;
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
        float currentSpeed = Mathf.Lerp(_superSlowSpeed, _normalSpeed, tValue);

        _marble.position = Vector3.MoveTowards(
            _marble.position, 
            _targetPosition, 
            currentSpeed * Time.deltaTime);

        if (Vector3.Distance(_marble.position, _targetPosition) < 0.01f)
        {
            _moving = false;
        }
    }
}
