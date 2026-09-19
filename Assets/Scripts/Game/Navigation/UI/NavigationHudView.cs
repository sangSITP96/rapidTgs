using TMPro;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Navigation.UI
{
    [DefaultExecutionOrder(10)]
    public sealed class NavigationHudView : MonoBehaviour
    {
        [Header("System")]
        [SerializeField] private NavigationController _controller;

        [Header("Plan Mode")]
        [SerializeField] private Button _planMarchButton;
        [SerializeField] private TMP_Text _planMarchButtonLabel;
        [SerializeField] private TMP_Text _hintLabel;

        [Header("Approval Popup")]
        [SerializeField] private GameObject _approvalPanel;
        [SerializeField] private TMP_Text _routeInfoLabel;
        [SerializeField] private Button _approveButton;
        [SerializeField] private Button _cancelButton;

        [Header("Failed Popup")]
        [SerializeField] private GameObject _failedPanel;
        [SerializeField] private TMP_Text _failedLabel;

        private void Awake()
        {
            EnsureControllerBound();
            WireButtons(true);
        }

        private void OnEnable()
        {
            EnsureControllerBound();
            Subscribe(true);
            Refresh();
        }

        private void Start()
        {
            bool alreadyBound = _controller != null;
            EnsureControllerBound();
            if (!alreadyBound && _controller != null)
                Subscribe(true);
            Refresh();
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        private void OnDestroy()
        {
            WireButtons(false);
        }

        private void EnsureControllerBound()
        {
            if (_controller == null)
                _controller = FindFirstObjectByType<NavigationController>();
        }

        public void Refresh()
        {
            if (_controller == null)
                return;

            bool planMode = _controller.DestinationSelectMode;
            if (_planMarchButtonLabel != null)
                _planMarchButtonLabel.text = planMode ? "Exit Plan Mode" : "Plan March";

            if (_hintLabel != null)
            {
                _hintLabel.text = planMode
                    ? "Tap map to set destination"
                    : "Tap Plan March, then tap destination";
            }

            NavigationRoute pending = _controller.PendingRoute;
            bool awaiting = _controller.IsAwaitingApproval &&
                            pending != null &&
                            pending.IsValid;

            if (_approvalPanel != null)
                _approvalPanel.SetActive(awaiting);

            if (awaiting && _routeInfoLabel != null)
            {
                _routeInfoLabel.text =
                    "Route Found\n" +
                    $"Cells: {pending.CellIndices.Count}   Dist ≈ {pending.ApproximateDistance:0.00}";
            }

            bool failed = planMode &&
                          pending != null &&
                          !pending.IsValid &&
                          !awaiting;

            if (_failedPanel != null)
                _failedPanel.SetActive(failed);

            if (failed && _failedLabel != null)
                _failedLabel.text = "Route Failed\n" + pending.FailureReason;
        }

        private void WireButtons(bool add)
        {
            if (_planMarchButton != null)
            {
                _planMarchButton.onClick.RemoveListener(OnPlanMarchClicked);
                if (add)
                    _planMarchButton.onClick.AddListener(OnPlanMarchClicked);
            }

            if (_approveButton != null)
            {
                _approveButton.onClick.RemoveListener(OnApproveClicked);
                if (add)
                    _approveButton.onClick.AddListener(OnApproveClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
                if (add)
                    _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void Subscribe(bool add)
        {
            if (_controller == null)
                return;

            if (add)
            {
                _controller.DestinationSelectModeChanged += OnModeChanged;
                _controller.RoutePendingApproval += OnRouteChanged;
                _controller.RouteRequestFailed += OnRouteChanged;
                _controller.RouteApprovalCancelled += OnCancelled;
                _controller.RouteApproved += OnApproved;
            }
            else
            {
                _controller.DestinationSelectModeChanged -= OnModeChanged;
                _controller.RoutePendingApproval -= OnRouteChanged;
                _controller.RouteRequestFailed -= OnRouteChanged;
                _controller.RouteApprovalCancelled -= OnCancelled;
                _controller.RouteApproved -= OnApproved;
            }
        }

        private void OnPlanMarchClicked()
        {
            if (_controller == null)
                _controller = FindFirstObjectByType<NavigationController>();

            if (_controller == null)
                return;

            if (!_controller.DestinationSelectMode)
                UiOverlayExclusive.Instance?.PrepareForPlanMarch();

            _controller.ToggleDestinationSelectMode();
            Refresh();
        }

        private void OnApproveClicked()
        {
            _controller?.ApprovePendingRoute();
            Refresh();
        }

        private void OnCancelClicked()
        {
            _controller?.CancelPendingRoute();
            Refresh();
        }

        private void OnModeChanged(bool _)
        {
            Refresh();
        }

        private void OnRouteChanged(NavigationRoute _)
        {
            Refresh();
        }

        private void OnCancelled()
        {
            Refresh();
        }

        private void OnApproved(NavigationRoute _)
        {
            Refresh();
        }
    }
}
