using System.Collections.Generic;
using Game.Weather.Cloud;
using Game.Weather.Convergence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConvergenceDebugUI : MonoBehaviour
{
    private sealed class ZoneRingPair
    {
        public LineRenderer Hold;
        public LineRenderer Attraction;
    }

    [Header("References")]
    [SerializeField] private ConvergenceManager _convergenceManager;
    [SerializeField] private CloudManager _cloudManager;
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

    [Header("Zone Rings")]
    [SerializeField] private bool _showZoneRings = true;
    [SerializeField] private float _ringHeight = 0.15f;
    [SerializeField] private float _lineWidth = 0.06f;
    [SerializeField] private int _circleSegments = 64;
    [SerializeField] private Color _holdZoneColor = new Color(0.65f, 0.2f, 1f, 1f);
    [SerializeField] private Color _attractionZoneColor = new Color(1f, 0.92f, 0.16f, 1f);

    private readonly List<RectTransform> _activeMarkers = new();
    private readonly List<ZoneRingPair> _zoneRings = new();
    private Transform _zoneRingsRoot;
    private Material _ringMaterial;

    private void Start()
    {
        ResolveReferences();

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

        EnsureZoneRingsRoot();
    }

    private void ResolveReferences()
    {
        if (_convergenceManager == null)
            _convergenceManager = FindFirstObjectByType<ConvergenceManager>();

        if (_cloudManager == null)
            _cloudManager = FindFirstObjectByType<CloudManager>();

        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_convergenceManager == null)
            return;

        if (_showInfoText && _infoText != null)
            UpdateInfoText();

        if (_showMarkers && _markerPrefab != null && _markersParent != null)
            UpdateMarkers();

        if (_showZoneRings)
            UpdateZoneRings();
        else
            HideAllZoneRings();
    }

    private void EnsureZoneRingsRoot()
    {
        if (_zoneRingsRoot != null)
            return;

        var root = new GameObject("ConvergenceZoneRings");
        root.transform.SetParent(transform, false);
        _zoneRingsRoot = root.transform;
    }

    private void UpdateZoneRings()
    {
        ResolveReferences();
        EnsureZoneRingsRoot();

        var points = _convergenceManager.ActivePoints;
        float holdRadius = GetHoldRadius();
        float attractionRadius = GetAttractionRadius();

        while (_zoneRings.Count < points.Count)
            _zoneRings.Add(CreateZoneRingPair(_zoneRings.Count));

        for (int i = points.Count; i < _zoneRings.Count; i++)
            SetRingPairActive(_zoneRings[i], false);

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var rings = _zoneRings[i];
            Vector3 center = new Vector3(point.Position.x, _ringHeight, point.Position.y);

            SetRingPairActive(rings, true);
            UpdateCircle(rings.Hold, center, holdRadius, _holdZoneColor);
            UpdateCircle(rings.Attraction, center, attractionRadius, _attractionZoneColor);
        }
    }

    private float GetHoldRadius()
    {
        return _cloudManager != null ? _cloudManager.ConvergenceHoldRadius : 1.2f;
    }

    private float GetAttractionRadius()
    {
        return _cloudManager != null ? _cloudManager.ConvergenceAttractionRadius : 3f;
    }

    private ZoneRingPair CreateZoneRingPair(int index)
    {
        var holdGo = new GameObject($"HoldRing_{index}");
        holdGo.transform.SetParent(_zoneRingsRoot, false);

        var attractionGo = new GameObject($"AttractionRing_{index}");
        attractionGo.transform.SetParent(_zoneRingsRoot, false);

        return new ZoneRingPair
        {
            Hold = CreateLineRenderer(holdGo, "Hold"),
            Attraction = CreateLineRenderer(attractionGo, "Attraction")
        };
    }

    private LineRenderer CreateLineRenderer(GameObject go, string label)
    {
        var line = go.AddComponent<LineRenderer>();
        line.name = label;
        line.useWorldSpace = true;
        line.loop = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = GetRingMaterial();
        line.widthMultiplier = 1f;
        line.startWidth = _lineWidth;
        line.endWidth = _lineWidth;
        return line;
    }

    private Material GetRingMaterial()
    {
        if (_ringMaterial != null)
            return _ringMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        _ringMaterial = new Material(shader);
        return _ringMaterial;
    }

    private void UpdateCircle(LineRenderer line, Vector3 center, float radius, Color color)
    {
        if (line == null)
            return;

        int segments = Mathf.Max(8, _circleSegments);
        line.positionCount = segments;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = _lineWidth;
        line.endWidth = _lineWidth;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, center.y, z));
        }
    }

    private static void SetRingPairActive(ZoneRingPair pair, bool active)
    {
        if (pair?.Hold != null)
            pair.Hold.gameObject.SetActive(active);

        if (pair?.Attraction != null)
            pair.Attraction.gameObject.SetActive(active);
    }

    private void HideAllZoneRings()
    {
        foreach (var pair in _zoneRings)
            SetRingPairActive(pair, false);
    }

    private void UpdateInfoText()
    {
        var points = _convergenceManager.ActivePoints;

        string info = "<b>=== CONVERGENCE DEBUG ===</b>\n";
        info += $"<color=yellow>Active Points: {points.Count}</color>\n";
        info += $"Hold Radius: {GetHoldRadius():F2} (purple)\n";
        info += $"Attraction Radius: {GetAttractionRadius():F2} (yellow)\n\n";

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];

            info += $"<color=cyan>Point #{i + 1}</color>\n";
            info += $"  Pos: ({point.Position.x:F1}, {point.Position.y:F1})\n";
            info += $"  Strength: {point.AttractionStrength:F2}\n\n";
        }

        _infoText.text = info;
    }

    private void UpdateMarkers()
    {
        var points = _convergenceManager.ActivePoints;

        while (_activeMarkers.Count < points.Count)
            CreateMarker();

        for (int i = points.Count; i < _activeMarkers.Count; i++)
            _activeMarkers[i].gameObject.SetActive(false);

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
            image.color = _markerColor;

        _activeMarkers.Add(rectTransform);
    }

    private void UpdateMarkerPosition(RectTransform marker, ConvergencePoint point)
    {
        Vector3 worldPos = new Vector3(point.Position.x, 0f, point.Position.y);
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
        _showZoneRings = !_showZoneRings;

        if (_infoText != null)
            _infoText.gameObject.SetActive(_showInfoText);

        if (!_showZoneRings)
            HideAllZoneRings();
    }

    private void OnDestroy()
    {
        foreach (var marker in _activeMarkers)
        {
            if (marker != null)
                Destroy(marker.gameObject);
        }

        _activeMarkers.Clear();

        if (_zoneRingsRoot != null)
            Destroy(_zoneRingsRoot.gameObject);

        _zoneRings.Clear();

        if (_ringMaterial != null)
            Destroy(_ringMaterial);
    }
}
