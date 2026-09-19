using Game.Travel;
using Game.Navigation;
using Game.Utilities;
using Game.UI;
using TGS;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MarbleMovement : MonoBehaviour
{
    [SerializeField] private InfiniteMapStreamer _mapStreamer;
    [SerializeField] private float _marbleBoundaryPadding = 0.05f;

    [SerializeField] private Transform _marble;
    [SerializeField] private TerrainGridSystem _terrainGridSystem;

    [Header("Phase 12 Travel")]
    [SerializeField] private TroopTravelController _travelController;

    [Header("Click / Tap Move")]
    [SerializeField] private bool _enableClickToMove = false;

    [Header("List Sliders")] [FormerlySerializedAs("_speedSlider")] [SerializeField]
    private Slider _neutralSpeedSlider;

    [SerializeField] private Slider _upHillSSlowdownSlider; // 0 - 100%

    [SerializeField] private Slider _downHillBoostSlider; // 0 - 200%;
    //
    [Header("Terrain masks")] 
    [SerializeField] private Texture2D _heightmapTexture;
    [SerializeField] private Texture2D _smallLakeMaskTexture;
    [SerializeField] private Texture2D _bigLakeMaskTexture;
    [SerializeField] private Texture2D _forestMaskTexture;
    [SerializeField] private Texture2D _albedoTexture;

    [Header("Biome Slowdown")]
    [SerializeField] private float _forestSlowdownFactor = 0.5f;
    [SerializeField] private float _mountainSlowdownFactor = 0.6f;
    [SerializeField] private float _lakeMaskThreshold = 0.5f;
    [SerializeField] private float _forestMaskThreshold = 0.5f;

    //
    [SerializeField] private float _normalSpeed = 2f;

    [SerializeField] private float _superSlowSpeed = 0.0056f;
    //

    private Vector3 _targetPosition;
    private bool _moving;

    [Header("Panel Speed Config")] [SerializeField]
    private GameObject _panelGameObject;

    [SerializeField]
    private GameObject _moralePanelGameObject;

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
    
    // phase 2
    [Header("Weather Settings")]
    [SerializeField] private WeatherManager _weatherManager;
    
    // Grid Visibility
    [Header("Grid Visibility")] [SerializeField]
    private GridVisibilityController _gridVisibility;

    [SerializeField] private WorldTerrainQuery _worldTerrainQuery;
    
    // Store values of Sliders
    private float _neutralSpeedValue = 5f;
    private float _upHillSpeedValue = 50f;
    private float _downHillSpeedValue = 0f;

    private bool _wasDevelopPanelActive;
    
    private Camera _camera;

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
    
    // cache delegates
    private UnityEngine.Events.UnityAction<float> _neutralSpeedAction;
    private UnityEngine.Events.UnityAction<float> _upHillAction;
    private UnityEngine.Events.UnityAction<float> _downHillAction;


    void Awake()
    {
        _neutralSpeedAction = (x) =>
        {
            if (_neutralSpeedText != null)
                _neutralSpeedText.text = x.ToString("0");
        };
        _upHillAction = (x) =>
        {
            if (_upHillText != null)
                _upHillText.text = x.ToString("0") + "%";
        };
        _downHillAction = (x) =>
        {
            if (_downHillText != null)
                _downHillText.text = x.ToString("0") + "%";
        };

        if (_travelController == null)
            _travelController = FindFirstObjectByType<TroopTravelController>();

        _camera = Camera.main;

        // Legacy toggle button (optional). Develop Mode now opens PanelSpeedConfig.
        if (_onOffConfigPanelButton != null)
        {
            _onOffConfigPanelButton.onClick.RemoveAllListeners();
            _onOffConfigPanelButton.onClick.AddListener(OnOffSpeedConfigPanel);
        }

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
        ApplyMovementSettingsToUi();

        if (_panelGameObject != null)
            _panelGameObject.SetActive(false);

        _wasDevelopPanelActive = false;
    }

    void Start()
    {
        _targetPosition = _marble.position;
        _lastFramePosition = _marble.position;

        if (_heightmapTexture == null && _groundRenderer != null)
        {
            _heightmap = _groundRenderer.sharedMaterial.mainTexture as Texture2D;
        }

        _previousHeight = GetHeightAtPosition(_marble.position);
    }

    void OnEnable()
    {
        if (_travelController == null)
            _travelController = FindFirstObjectByType<TroopTravelController>();

        if (_travelController != null)
            _travelController.MarchControlStarted += StopMoving;
    }

    void OnDisable()
    {
        if (_travelController != null)
            _travelController.MarchControlStarted -= StopMoving;
    }

    public void StopMoving()
    {
        _moving = false;
        _currentSlopeState = SlopeState.Normal;
        _distanceInCurrentState = 0f;
        if (_marble != null)
            _targetPosition = _marble.position;
    }

    void Update()
    {
        SyncDevelopPanelLifecycle();
        HandleTap();
        Move();
    }

    /// <summary>
    /// Fills Movement Settings sliders/texts from saved values.
    /// Called when Develop Mode → Movement Settings is shown.
    /// </summary>
    public void ApplyMovementSettingsToUi()
    {
        _neutralSpeedValue = PlayerPrefs.GetFloat("neutralspeed", _neutralSpeedValue);
        _upHillSpeedValue = PlayerPrefs.GetFloat("uphillspeed", _upHillSpeedValue);
        _downHillSpeedValue = PlayerPrefs.GetFloat("downhillspeed", _downHillSpeedValue);

        if (_neutralSpeedSlider != null)
            _neutralSpeedSlider.SetValueWithoutNotify(_neutralSpeedValue);

        if (_upHillSSlowdownSlider != null)
            _upHillSSlowdownSlider.SetValueWithoutNotify(_upHillSpeedValue);

        if (_downHillBoostSlider != null)
            _downHillBoostSlider.SetValueWithoutNotify(_downHillSpeedValue);

        UpdateSliderTexts();
    }

    /// <summary>
    /// Persists current slider values (same as closing the old config button).
    /// </summary>
    public void SaveMovementSettingsFromUi()
    {
        if (_neutralSpeedSlider != null)
            _neutralSpeedValue = _neutralSpeedSlider.value;

        if (_upHillSSlowdownSlider != null)
            _upHillSpeedValue = _upHillSSlowdownSlider.value;

        if (_downHillBoostSlider != null)
            _downHillSpeedValue = _downHillBoostSlider.value;

        PlayerPrefs.SetFloat("neutralspeed", _neutralSpeedValue);
        PlayerPrefs.SetFloat("uphillspeed", _upHillSpeedValue);
        PlayerPrefs.SetFloat("downhillspeed", _downHillSpeedValue);
        PlayerPrefs.Save();
    }

    private void SyncDevelopPanelLifecycle()
    {
        if (_panelGameObject == null)
            return;

        bool active = _panelGameObject.activeSelf;
        if (active == _wasDevelopPanelActive)
            return;

        if (active)
        {
            ApplyMovementSettingsToUi();
            if (_moralePanelGameObject != null)
                _moralePanelGameObject.SetActive(false);
        }
        else
        {
            SaveMovementSettingsFromUi();
            if (_moralePanelGameObject != null)
                _moralePanelGameObject.SetActive(true);
        }

        _wasDevelopPanelActive = active;
    }

    // Legacy entry point if an old toggle button is still wired.
    private void OnOffSpeedConfigPanel()
    {
        if (_panelGameObject == null)
            return;

        bool show = !_panelGameObject.activeSelf;
        _panelGameObject.SetActive(show);
        // SyncDevelopPanelLifecycle handles apply/save + morale panel.
    }

    private void HandleTap()
    {
        if (!_enableClickToMove)
            return;

        if (NavigationController.IsDestinationSelectModeActive)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (UiPointerUtility.IsPointerOverUi())
                return;
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (_travelController != null && _travelController.IsDrivingMover)
                    _travelController.CancelTravel();

                Vector3 tapped = new Vector3(hit.point.x, _marble.position.y, hit.point.z);
                _targetPosition = _mapStreamer != null?
                    _mapStreamer.ClampWorldPositionXZ(tapped, _marbleBoundaryPadding)
                    : tapped;
                _moving = true;

                _currentSlopeState = SlopeState.Normal;
                _distanceInCurrentState = 0f;
                _previousHeight = GetHeightAtPosition(_marble.position);
                
                //
                if (_gridVisibility != null)
                {
                    _gridVisibility.OnMarbleMovementStarted();
                }
            }
        }
    }

    private void Move()
    {
        if (!_moving) return;

        if (_travelController != null && _travelController.IsDrivingMover)
        {
            StopMoving();
            return;
        }

        Vector3 current = _marble.position;
        if(_mapStreamer != null)
        {
            current = _mapStreamer.ClampWorldPositionXZ(current, _marbleBoundaryPadding);
            _marble.position = current;

            _targetPosition = _mapStreamer.ClampWorldPositionXZ(_targetPosition, _marbleBoundaryPadding);
        }
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

        float currentHeight = _worldTerrainQuery != null
                                ?_worldTerrainQuery.GetHeight(current)
                                :GetHeightAtPosition(current);

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

        // phase 2, calculate and apply weather condition
        float weatherMultiplier = 1f;
        if (_weatherManager != null)
        {
            weatherMultiplier = _weatherManager.GetTotalWeatherMultiplier();
        }
        
        // Apply biome slowdown at current position
        float biomeSlowdown = GetBiomeSlowdownFactor(current);
        float finalSpeed = baseSpeed * speedFactor * weatherMultiplier * biomeSlowdown;
        

        float stepDistance = finalSpeed * Time.deltaTime;
        Vector3 nextPos = current + direction * stepDistance;

        //float brightness = GetBrightnessAtPosition(nextPos);
        var movementBlocked = _worldTerrainQuery != null
                        ?_worldTerrainQuery.IsMovementBlocked(nextPos)
                        :TerrainMaskUtility.Instance.IsLake(nextPos);   

        if (movementBlocked)
        {
            _moving = false;
            return;
        }

        TextShowSpeed.text = finalSpeed.ToString("0.00");

        _marble.position = Vector3.MoveTowards(
            _marble.position,
            _targetPosition,
            finalSpeed * Time.deltaTime);

        if(_mapStreamer != null)
        {
            _marble.position = _mapStreamer.
                ClampWorldPositionXZ(_marble.position,
                _marbleBoundaryPadding);
        }

        // Save pos and height for after frame
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
                _heightmap = _groundRenderer.sharedMaterial.mainTexture as Texture2D;

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
    //
    private Vector2 GetUVFromWorldPosition(Vector3 worldPosition)
    {
        if (_groundRenderer == null) return Vector2.zero;
        Ray ray = new Ray(worldPosition + Vector3.up * 1f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 50f, _groundLayerMask))
        {
            return hit.textureCoord;
        }
        return Vector2.zero;
    }

    private float GetTextureValueAtPosition(Texture2D texture, Vector3 worldPosition)
    {
        if (texture == null)
        {
            return 0f;
        }
        
        Vector2 uv = GetUVFromWorldPosition(worldPosition);
        if (uv == Vector2.zero)
        {
            return 0f;
        }
        
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * texture.width), 0, texture.width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * texture.height), 0, texture.height - 1);
        Color pixel = texture.GetPixel(x, y);

        // Return grayscale value
        return (pixel.r + pixel.g + pixel.b) / 3f;
    }

    // private bool IsLake(Vector3 worldPos)
    // {
    //     if (_smallLakeMaskTexture != null)
    //     {
    //         float lakeValue = GetTextureValueAtPosition(_smallLakeMaskTexture, worldPos);
    //         var isLake = lakeValue > _lakeMaskThreshold;
    //         if (!isLake && _bigLakeMaskTexture != null)
    //         {
    //             lakeValue = GetTextureValueAtPosition(_bigLakeMaskTexture, worldPos);
    //             return lakeValue > _lakeMaskThreshold;
    //         }
    //         else
    //         {
    //             return isLake;
    //         }
    //     }
    //     
    //     // Fallback to old method if texture not assigned
    //     float brightness = GetBrightnessAtPosition(worldPos);
    //     return brightness < 0.05f;
    // }
    
    private bool IsInForest(Vector3 worldPos)
    {
        if (_forestMaskTexture == null)
        {
            return false;
        }

        float forestValue = GetTextureValueAtPosition(_forestMaskTexture, worldPos);
        return forestValue > _forestMaskThreshold;
    }
    
    private float GetBiomeSlowdownFactor(Vector3 worldPos)
    {
        bool isForest = _worldTerrainQuery != null
            ? _worldTerrainQuery.IsForest(worldPos)
            : IsInForest(worldPos);

        if (isForest)
            return _forestSlowdownFactor;

        bool isMountain = _worldTerrainQuery != null &&
                          _worldTerrainQuery.IsMountain(worldPos);

        if (isMountain)
            return _mountainSlowdownFactor;

        return 1f;
    }

    private float GetHeightAtPosition(Vector3 worldPos)
    {
        if(_worldTerrainQuery != null)
        {
            return _worldTerrainQuery.GetHeight(worldPos);
        }

        if (_heightmapTexture != null)
        {
            return GetTextureValueAtPosition(_heightmapTexture, worldPos);
        }
        // Fallback to old method if texture not assigned
        if (_heightmap != null)
        {
            return GetBrightnessAtPosition(worldPos);
        }
        return 0.5f;
    }

    private void RegisterOnValueChangeSliders()
    {
        if (_neutralSpeedSlider != null)
            _neutralSpeedSlider.onValueChanged.AddListener(_neutralSpeedAction);

        if (_upHillSSlowdownSlider != null)
            _upHillSSlowdownSlider.onValueChanged.AddListener(_upHillAction);

        if (_downHillBoostSlider != null)
            _downHillBoostSlider.onValueChanged.AddListener(_downHillAction);
    }

    private void UpdateSliderTexts()
    {
        if (_neutralSpeedText != null && _neutralSpeedSlider != null)
            _neutralSpeedText.text = _neutralSpeedSlider.value.ToString("0");

        if (_upHillText != null && _upHillSSlowdownSlider != null)
            _upHillText.text = _upHillSSlowdownSlider.value.ToString("0") + "%";

        if (_downHillText != null && _downHillBoostSlider != null)
            _downHillText.text = _downHillBoostSlider.value.ToString("0") + "%";
    }

    public void BackToMenu()
    {
        // Weather preset picker now lives in Develop Mode → Environment.
        // Close the develop panel instead of loading MenuWithWeather.
        if (_panelGameObject != null)
            _panelGameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (_onOffConfigPanelButton != null)
            _onOffConfigPanelButton.onClick.RemoveListener(OnOffSpeedConfigPanel);

        if (_neutralSpeedSlider != null)
            _neutralSpeedSlider.onValueChanged.RemoveListener(_neutralSpeedAction);

        if (_upHillSSlowdownSlider != null)
            _upHillSSlowdownSlider.onValueChanged.RemoveListener(_upHillAction);

        if (_downHillBoostSlider != null)
            _downHillBoostSlider.onValueChanged.RemoveListener(_downHillAction);
    }
}