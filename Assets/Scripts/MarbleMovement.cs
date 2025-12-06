using TGS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MarbleMovement : MonoBehaviour
{
    [SerializeField] private Transform _marble;
    [SerializeField] private TerrainGridSystem _terrainGridSystem;

    [Header("List Sliders")] [FormerlySerializedAs("_speedSlider")] [SerializeField]
    private Slider _neutralSpeedSlider;

    [SerializeField] private Slider _upHillSSlowdownSlider; // 0 - 100%

    [SerializeField] private Slider _downHillBoostSlider; // 0 - 200%;

    //
    [SerializeField] private float _normalSpeed = 2f;

    [SerializeField] private float _superSlowSpeed = 0.0056f;
    //

    private Vector3 _targetPosition;
    private bool _moving;

    [Header("Panel Speed Config")] [SerializeField]
    private GameObject _panelGameObject;

    [FormerlySerializedAs("_closePanelButton")] [SerializeField]
    private Button _onOffConfigPanelButton;

    [Header("UI Text")] [SerializeField] private Text _neutralSpeedText;
    [SerializeField] private Text _upHillText;
    [SerializeField] private Text _downHillText;

    // Text Debug
    public Text TextShowSpeed;

    // phase 1B
    [SerializeField] private Renderer _groundRenderer;
    private Texture2D _heightmap;

    // Store values of Sliders
    private float _neutralSpeedValue = 5f;
    private float _upHillSpeedValue = 50f;
    private float _downHillSpeedValue = 0f;

    private bool _isShowConfigPanel = false;

    private enum SlopeState
    {
        Normal,
        Uphill,
        Downhill
    }

    private SlopeState _currentSlopeState = SlopeState.Normal;
    private float _distanceInCurrentState = 0f;
    private Vector3 _lastFramePosition;

    private float _previousHeight = 0f;


    void Awake()
    {
        _onOffConfigPanelButton.onClick.RemoveAllListeners();
        _onOffConfigPanelButton.onClick.AddListener(() => { OnOffSpeedConfigPanel(); });

        // Slider Config
        if (_neutralSpeedSlider != null)
        {
            _neutralSpeedSlider.minValue = 1f;
            _neutralSpeedSlider.maxValue = 10f;
        }

        if (_upHillSSlowdownSlider != null)
        {
            _upHillSSlowdownSlider.minValue = 0f;
            _upHillSSlowdownSlider.maxValue = 100f;
        }

        if (_downHillBoostSlider != null)
        {
            _downHillBoostSlider.minValue = 0f;
            _downHillBoostSlider.maxValue = 200f;
        }

        RegisterOnValueChangeSliders();
        // Initial Setup
        _neutralSpeedSlider.value = _neutralSpeedValue;
        _upHillSSlowdownSlider.value = _upHillSpeedValue;
        _downHillBoostSlider.value = _downHillSpeedValue;
        //
        _panelGameObject.SetActive(false);
    }

    void Start()
    {
        _targetPosition = _marble.position;
        _lastFramePosition = _marble.position;

        if (_groundRenderer != null)
        {
            _heightmap = _groundRenderer.material.mainTexture as Texture2D;
        }

        _previousHeight = GetHeightAtPosition(_marble.position);
    }

    void Update()
    {
        //UpdateSliderTexts();
        HandleTap();
        Move();
    }

    private void OnOffSpeedConfigPanel()
    {
        _isShowConfigPanel = !_isShowConfigPanel;
        if (!_isShowConfigPanel)
        {
            _neutralSpeedValue = _neutralSpeedSlider.value;
            _upHillSpeedValue = _upHillSSlowdownSlider.value;
            _downHillSpeedValue = _downHillBoostSlider.value;
        }

        _panelGameObject.SetActive(_isShowConfigPanel);
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

                _currentSlopeState = SlopeState.Normal;
                _distanceInCurrentState = 0f;
                _previousHeight = GetHeightAtPosition(_marble.position);
            }
        }
    }

    private void Move()
    {
        if (!_moving) return;

        Vector3 current = _marble.position;
        Vector3 toTarget = _targetPosition - current;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            _moving = false;
            _currentSlopeState = SlopeState.Normal;
            _distanceInCurrentState = 0f;
            return;
        }

        Vector3 direction = toTarget.normalized;

        float tValue = Mathf.InverseLerp(1f, 10f, _neutralSpeedValue);
        float baseSpeed = Mathf.Lerp(_superSlowSpeed, _normalSpeed, tValue);

        float movedDistance = Vector3.Distance(current, _lastFramePosition);
        _distanceInCurrentState += movedDistance;

        float currentHeight = GetHeightAtPosition(current);

        float actualSlopeDelta = currentHeight - _previousHeight;

        const float slopeLookAhead = 0.4f;
        float lookDistance = Mathf.Min(slopeLookAhead, toTarget.magnitude);
        Vector3 slopeSamplePos = current + direction * lookDistance;
        float predictedHeight = GetHeightAtPosition(slopeSamplePos);
        float predictedSlopeDelta = predictedHeight - currentHeight;

        // ===== STATE MACHINE LOGIC =====
        const float enterThreshold = 0.04f;
        const float minStateDistance = 0.3f;

        
        if (_currentSlopeState == SlopeState.Normal)
        {
            if (actualSlopeDelta > enterThreshold)
            {
                _currentSlopeState = SlopeState.Uphill;
                _distanceInCurrentState = 0f;
            }
            else if (actualSlopeDelta < -enterThreshold)
            {
                _currentSlopeState = SlopeState.Downhill;
                _distanceInCurrentState = 0f;
            }
        }
        else if (_currentSlopeState == SlopeState.Uphill)
        {
            if (actualSlopeDelta < -enterThreshold)
            {
                _currentSlopeState = SlopeState.Downhill;
                _distanceInCurrentState = 0f;
            }
            else if (_distanceInCurrentState >= minStateDistance && 
                     Mathf.Abs(actualSlopeDelta) < enterThreshold &&
                     Mathf.Abs(predictedSlopeDelta) < enterThreshold)
            {
                _currentSlopeState = SlopeState.Normal;
                _distanceInCurrentState = 0f;
            }
        }
        else if (_currentSlopeState == SlopeState.Downhill)
        {
            if (actualSlopeDelta > enterThreshold)
            {
                _currentSlopeState = SlopeState.Uphill;
                _distanceInCurrentState = 0f;
            }
            else if (_distanceInCurrentState >= minStateDistance && 
                     Mathf.Abs(actualSlopeDelta) < enterThreshold &&
                     Mathf.Abs(predictedSlopeDelta) < enterThreshold)
            {
                _currentSlopeState = SlopeState.Normal;
                _distanceInCurrentState = 0f;
            }
        }

        float speedFactor = 1f;

        if (_currentSlopeState == SlopeState.Uphill)
        {
            float upHillPct = _upHillSpeedValue;
            float upHillFactor = 1f - Mathf.Clamp01(upHillPct / 100f);
            speedFactor *= upHillFactor;
        }
        else if (_currentSlopeState == SlopeState.Downhill)
        {
            float downHillPct = _downHillSpeedValue;
            float downHillFactor = 1f + (Mathf.Clamp(downHillPct, 0f, 200f) / 100f);
            speedFactor *= downHillFactor;
        }

        float finalSpeed = baseSpeed * speedFactor;

        float stepDistance = finalSpeed * Time.deltaTime;
        Vector3 nextPos = current + direction * stepDistance;

        float brightness = GetBrightnessAtPosition(nextPos);

        if (IsLake(brightness))
        {
            _moving = false;
            return;
        }

        TextShowSpeed.text = finalSpeed.ToString("0.00");

        _marble.position = Vector3.MoveTowards(
            _marble.position,
            _targetPosition,
            finalSpeed * Time.deltaTime);

        // Lưu vị trí và height cho frame sau
        _lastFramePosition = current;
        _previousHeight = currentHeight;

        if (Vector3.Distance(_marble.position, _targetPosition) < 0.01f)
        {
            _moving = false;
            _currentSlopeState = SlopeState.Normal;
            _distanceInCurrentState = 0f;
        }
    }

    public LayerMask _groundLayerMask;

    private float GetBrightnessAtPosition(Vector3 worldPos)
    {
        if (_groundRenderer == null)
        {
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

            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * _heightmap.width), 0, _heightmap.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * _heightmap.height), 0, _heightmap.height - 1);
            Color c = _heightmap.GetPixel(x, y);

            float grayscale = (c.r + c.g + c.b) / 3f;

            grayscale = Mathf.Round(grayscale * 100f) / 100f;

            return grayscale;
        }

        return 0.5f;
    }

    private bool IsLake(float brightness)
    {
        return brightness < 0.05f;
    }

    private float GetHeightAtPosition(Vector3 worldPos)
    {
        return GetBrightnessAtPosition(worldPos);
    }

    private void RegisterOnValueChangeSliders()
    {
        _neutralSpeedSlider.onValueChanged.AddListener(x => { _neutralSpeedText.text = x.ToString("0"); });

        _upHillSSlowdownSlider.onValueChanged.AddListener(x => { _upHillText.text = x.ToString("0") + "%"; });

        _downHillBoostSlider.onValueChanged.AddListener(x => { _downHillText.text = x.ToString("0") + "%"; });
    }

    private void UpdateSliderTexts()
    {
        _neutralSpeedText.text = _neutralSpeedSlider.value.ToString("0");
        _upHillText.text = _upHillSSlowdownSlider.value.ToString("0") + "%";
        _downHillText.text = _downHillBoostSlider.value.ToString("0") + "%";
    }
}