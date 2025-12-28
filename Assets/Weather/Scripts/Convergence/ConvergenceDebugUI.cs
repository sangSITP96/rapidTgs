using System.Collections.Generic;
using Game.Weather.Convergence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConvergenceDebugUI : MonoBehaviour
{
        [Header("References")]
        [SerializeField] private ConvergenceManager _convergenceManager;
        [SerializeField] private Camera _mainCamera;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _infoText; 
        [SerializeField] private Transform _markersParent;
        [SerializeField] private GameObject _markerPrefab;
        
        [Header("Debug Settings")]
        [SerializeField] private bool _showMarkers = true;
        [SerializeField] private bool _showInfoText = true;
        [SerializeField] private Color _markerColor = Color.cyan;
        [SerializeField] private float _markerSize = 30f;
        
        private readonly List<RectTransform> _activeMarkers = new();

        private void Start()
        {
            if (_convergenceManager == null)
            {
                _convergenceManager = FindFirstObjectByType<ConvergenceManager>();
            }
            
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // Validation
            if (_convergenceManager == null)
            {
                Debug.LogError("[ConvergenceDebugUI] Cannot found ConvergenceManager!");
                enabled = false;
                return;
            }

            if (_mainCamera == null)
            {
                Debug.LogError("[ConvergenceDebugUI] Cannot found Camera!");
                enabled = false;
                return;
            }

            if (_markersParent == null && _markerPrefab != null)
            {
                GameObject go = new GameObject("ConvergenceMarkers");
                go.transform.SetParent(transform, false);
                _markersParent = go.transform;
            }
        }

        private void Update()
        {
            // if (_convergenceManager == null) return;
            //
            // if (_showInfoText && _infoText != null)
            // {
            //     UpdateInfoText();
            // }
            //
            // if (_showMarkers && _markerPrefab != null && _markersParent != null)
            // {
            //     UpdateMarkers();
            // }
        }

        private void UpdateInfoText()
        {
            var points = _convergenceManager.ActivePoints;
            
            string info = $"<b>=== CONVERGENCE DEBUG ===</b>\n";
            info += $"<color=yellow>Active Points: {points.Count}</color>\n\n";

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                
                double remaining = point.ExpireGameSeconds - point.SpawnGameSeconds; // Placeholder
                double elapsed = 0; // Placeholder - cần WorldTime.TotalGameSeconds
                
                info += $"<color=cyan>Point #{i + 1}</color>\n";
                info += $"  Pos: ({point.Position.x:F1}, {point.Position.y:F1})\n";
                info += $"  Drift: ({point.DriftDirection.x:F2}, {point.DriftDirection.y:F2})\n";
                info += $"  Strength: {point.AttractionStrength:F2}\n";
                // info += $"  Lifetime: {remaining / 3600:F1}h\n"; // Nếu có WorldTime
                info += "\n";
            }

            _infoText.text = info;
        }

        private void UpdateMarkers()
        {
            var points = _convergenceManager.ActivePoints;

            while (_activeMarkers.Count < points.Count)
            {
                CreateMarker();
            }

            for (int i = points.Count; i < _activeMarkers.Count; i++)
            {
                _activeMarkers[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var marker = _activeMarkers[i];
                
                marker.gameObject.SetActive(true);
                UpdateMarkerPosition(marker, point);
            }
        }

        private void CreateMarker()
        {
            GameObject markerObj = Instantiate(_markerPrefab, _markersParent);
            RectTransform rectTransform = markerObj.GetComponent<RectTransform>();
            
            if (rectTransform == null)
            {
                Debug.LogError("[ConvergenceDebugUI] Marker prefab have to RectTransform!");
                Destroy(markerObj);
                return;
            }

            rectTransform.sizeDelta = new Vector2(_markerSize, _markerSize);
            
            Image image = markerObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = _markerColor;
            }

            _activeMarkers.Add(rectTransform);
        }

        private void UpdateMarkerPosition(RectTransform marker, ConvergencePoint point)
        {
            Vector3 worldPos = new Vector3(point.Position.x, 0, point.Position.y);
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0 && 
                screenPos.x >= 0 && screenPos.x <= Screen.width &&
                screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                marker.gameObject.SetActive(true);
                marker.position = screenPos;
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }

        public void ToggleDebugUI()
        {
            _showMarkers = !_showMarkers;
            _showInfoText = !_showInfoText;
            
            if (_infoText != null)
            {
                _infoText.gameObject.SetActive(_showInfoText);
            }
        }

        private void OnDestroy()
        {
            // Cleanup markers
            foreach (var marker in _activeMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
            }
            _activeMarkers.Clear();
        }
}
