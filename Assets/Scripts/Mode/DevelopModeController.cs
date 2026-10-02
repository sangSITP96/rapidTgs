using UnityEngine;
using UnityEngine.UI;
using Game.Travel;
using Game.Seasons.DebugTools;

public sealed class DevelopModeController : MonoBehaviour
{
    [Header("Sidebar")]
    [SerializeField] private Button _movementButton;
    [SerializeField] private Button _environmentButton;
    [SerializeField] private Button _travelDebugButton;
    [SerializeField] private Button _seasonDebugButton;

    [Header("Content Panels")]
    [SerializeField] private GameObject _movementPanel;
    [SerializeField] private GameObject _environmentPanel;
    [SerializeField] private GameObject _travelDebugPanel;
    [SerializeField] private GameObject _seasonDebugPanel;

    [Header("Travel Debug")]
    [SerializeField] private Toggle _showTravelOverlayToggle;
    [SerializeField] private TravelDebugController _travelDebug;

    [Header("Season Debug")]
    [SerializeField] private Toggle _showSeasonOverlayToggle;
    [SerializeField] private SeasonDebugController _seasonDebug;

    [Header("Movement Settings")]
    [SerializeField] private MarbleMovement _marbleMovement;

    [Header("Optional Title")]
    [SerializeField] private Text _contentTitle;

    [SerializeField] private ModeType _defaultMode = ModeType.Movement;

    private ModeType _currentMode = ModeType.Movement;

    public ModeType CurrentMode => _currentMode;

    private void Awake()
    {
        if (_travelDebug == null)
            _travelDebug = FindFirstObjectByType<TravelDebugController>();

        if (_marbleMovement == null)
            _marbleMovement = FindFirstObjectByType<MarbleMovement>();

        if (_showTravelOverlayToggle != null)
        {
            _showTravelOverlayToggle.onValueChanged.RemoveListener(SetTravelOverlayVisible);
            _showTravelOverlayToggle.onValueChanged.AddListener(SetTravelOverlayVisible);

            if (_travelDebug != null)
                _showTravelOverlayToggle.SetIsOnWithoutNotify(_travelDebug.ShowOverlay);
        }

        if (_showSeasonOverlayToggle != null)
        {
            _showSeasonOverlayToggle.onValueChanged.RemoveListener(SetSeasonOverlayVisible);
            _showSeasonOverlayToggle.onValueChanged.AddListener(SetSeasonOverlayVisible);
            _showSeasonOverlayToggle.SetIsOnWithoutNotify(false);
        }

        SetMode(_defaultMode);
    }

    private void OnEnable()
    {
        if (_showSeasonOverlayToggle != null && _seasonDebug != null)
            _showSeasonOverlayToggle.SetIsOnWithoutNotify(_seasonDebug.ShowOverlay);
    }

    private void OnDestroy()
    {
        if (_showTravelOverlayToggle != null)
            _showTravelOverlayToggle.onValueChanged.RemoveListener(SetTravelOverlayVisible);

        if (_showSeasonOverlayToggle != null)
            _showSeasonOverlayToggle.onValueChanged.RemoveListener(SetSeasonOverlayVisible);
    }

    public void SetMovementMode()
    {
        SetMode(ModeType.Movement);
    }

    public void SetEnvironmentMode()
    {
        SetMode(ModeType.Environment);
    }

    public void SetTravelDebugMode()
    {
        SetMode(ModeType.TravelDebug);
    }

    public void SetSeasonDebugMode()
    {
        SetMode(ModeType.SeasonDebug);
    }

    public void SetTravelOverlayVisible(bool visible)
    {
        if (_travelDebug == null)
            _travelDebug = FindFirstObjectByType<TravelDebugController>();

        if (_travelDebug != null)
            _travelDebug.ShowOverlay = visible;
    }

    public void SetSeasonOverlayVisible(bool visible)
    {
        if (_seasonDebug != null)
            _seasonDebug.ShowOverlay = visible;
    }

    public void SetMode(ModeType mode)
    {
        if (_currentMode == ModeType.Movement && mode != ModeType.Movement)
            _marbleMovement?.SaveMovementSettingsFromUi();

        _currentMode = mode;

        if (_movementPanel != null)
            _movementPanel.SetActive(mode == ModeType.Movement);

        if (_environmentPanel != null)
            _environmentPanel.SetActive(mode == ModeType.Environment);

        if (_travelDebugPanel != null)
            _travelDebugPanel.SetActive(mode == ModeType.TravelDebug);

        if (_seasonDebugPanel != null)
            _seasonDebugPanel.SetActive(mode == ModeType.SeasonDebug);

        if (mode == ModeType.Movement)
            _marbleMovement?.ApplyMovementSettingsToUi();

        if (_contentTitle != null)
        {
            _contentTitle.text = mode switch
            {
                ModeType.Movement => "Movement Settings",
                ModeType.Environment => "Environment",
                ModeType.TravelDebug => "Travel Debug",
                ModeType.SeasonDebug => "Season Debug",
                _ => "Develop Mode"
            };
        }
    }
}
