using System.Collections.Generic;
using Game.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Mutual exclusion: Event Log / Develop Mode vs Plan March HUD + destination select.
    /// Develop Mode chrome is only shown when <see cref="AppBuildMode.Develop"/>.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class UiOverlayExclusive : MonoBehaviour
    {
        public static UiOverlayExclusive Instance { get; private set; }

        [Header("Build")]
        [SerializeField] private AppBuildMode _appBuildMode = AppBuildMode.Develop;

        [Header("Overlays that block Plan March")]
        [SerializeField] private GameObject _eventLogPanel;
        [SerializeField] private GameObject _developConfigPanel;
        [SerializeField] private Toggle _developModeToggle;

        [Header("Chrome hidden while Plan March is active")]
        [SerializeField] private GameObject _eventLogButton;
        [SerializeField] private GameObject _developModeRoot;

        [Header("Restore when closing Event Log for Plan March")]
        [SerializeField] private GameObject _moralePanel;

        [Header("Plan March HUD")]
        [SerializeField] private GameObject _navigationHud;

        public AppBuildMode AppBuildMode => _appBuildMode;

        public bool IsDevelopBuild => _appBuildMode == AppBuildMode.Develop;

        public bool BlocksPlanMarch =>
            IsActive(_eventLogPanel) || (IsDevelopBuild && IsActive(_developConfigPanel));

        public static bool IsPlanMarchBlocked =>
            Instance != null && Instance.BlocksPlanMarch;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"{nameof(UiOverlayExclusive)}: duplicate instance on {name}.");

            Instance = this;
            ResolveFallbackRefs();
            ApplyDevelopVisibility(forceHidePanel: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            ResolveFallbackRefs();

            if (!IsDevelopBuild)
            {
                ApplyDevelopVisibility(forceHidePanel: true);
                bool eventLogOpen = IsActive(_eventLogPanel);
                bool planActiveProd = NavigationController.IsDestinationSelectModeActive;

                if (eventLogOpen && planActiveProd)
                {
                    NavigationController.Instance?.SetDestinationSelectMode(false);
                    planActiveProd = false;
                }

                SetActiveSafe(_navigationHud, !eventLogOpen);
                SetActiveSafe(_eventLogButton, !planActiveProd);
                return;
            }

            bool overlayOpen = BlocksPlanMarch;
            bool planActive = NavigationController.IsDestinationSelectModeActive;

            if (overlayOpen && planActive)
            {
                NavigationController.Instance?.SetDestinationSelectMode(false);
                planActive = false;
            }

            SetActiveSafe(_navigationHud, !overlayOpen);

            if (planActive)
            {
                SetActiveSafe(_eventLogButton, false);
                SetActiveSafe(_developModeRoot, false);
            }
            else if (IsActive(_eventLogPanel))
            {
                SetActiveSafe(_developModeRoot, false);
                SetActiveSafe(_eventLogButton, true);
            }
            else if (IsActive(_developConfigPanel))
            {
                SetActiveSafe(_eventLogButton, false);
                SetActiveSafe(_developModeRoot, true);
            }
            else
            {
                SetActiveSafe(_eventLogButton, true);
                SetActiveSafe(_developModeRoot, true);
            }
        }

        public void PrepareForPlanMarch()
        {
            ResolveFallbackRefs();

            if (_eventLogPanel != null && _eventLogPanel.activeSelf)
                _eventLogPanel.SetActive(false);

            if (IsDevelopBuild)
            {
                if (_developConfigPanel != null && _developConfigPanel.activeSelf)
                    _developConfigPanel.SetActive(false);

                if (_developModeToggle != null && _developModeToggle.isOn)
                    _developModeToggle.SetIsOnWithoutNotify(false);
            }

            if (_moralePanel != null && !_moralePanel.activeSelf)
                _moralePanel.SetActive(true);

            if (_navigationHud != null && !_navigationHud.activeSelf)
                _navigationHud.SetActive(true);

            SetActiveSafe(_eventLogButton, false);
            SetActiveSafe(_developModeRoot, false);
        }

        private void ApplyDevelopVisibility(bool forceHidePanel)
        {
            if (IsDevelopBuild)
                return;

            SetActiveSafe(_developModeRoot, false);

            if (!forceHidePanel)
                return;

            if (_developConfigPanel != null && _developConfigPanel.activeSelf)
                _developConfigPanel.SetActive(false);

            if (_developModeToggle != null && _developModeToggle.isOn)
                _developModeToggle.SetIsOnWithoutNotify(false);
        }

        private void ResolveFallbackRefs()
        {
            if (_eventLogPanel == null)
                _eventLogPanel = GameObject.Find("EventLogPanel");
            if (_developConfigPanel == null)
                _developConfigPanel = GameObject.Find("PanelSpeedConfig");
            if (_moralePanel == null)
                _moralePanel = GameObject.Find("MoralePanel");
            if (_navigationHud == null)
                _navigationHud = GameObject.Find("NavigationHUD");
            if (_eventLogButton == null)
                _eventLogButton = GameObject.Find("EventLogBtn");
            if (_developModeRoot == null)
                _developModeRoot = GameObject.Find("DevelopMode");
            if (_developModeToggle == null && _developModeRoot != null)
                _developModeToggle = _developModeRoot.GetComponentInChildren<Toggle>(true);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }

        private static bool IsActive(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }
    }

    /// <summary>
    /// Reliable UI hit-test for both legacy Input and Input System UI modules.
    /// </summary>
    public static class UiPointerUtility
    {
        private static readonly List<RaycastResult> Results = new List<RaycastResult>(16);

        public static bool IsPointerOverUi()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
                return false;

            Vector2 screenPos;
            if (Input.touchCount > 0)
                screenPos = Input.GetTouch(0).position;
            else
                screenPos = Input.mousePosition;

            var eventData = new PointerEventData(es) { position = screenPos };
            Results.Clear();
            es.RaycastAll(eventData, Results);
            return Results.Count > 0;
        }
    }
}
