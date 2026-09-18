using UnityEngine;
using Game.Core.WorldTime;
using Game.Morale;

namespace Game.Travel
{
    /// <summary>
    /// Prototype-only manual controls for Phase 12 travel bones. Not final UI.
    /// </summary>
    public sealed class TravelDebugController : MonoBehaviour
    {
        [SerializeField] private TroopTravelController _travel;
        [SerializeField] private TroopMoraleSystem _morale;
        [SerializeField] private WorldTime _worldTime;

        [Header("Debug Destination")]
        [SerializeField] private Vector3 _destinationOffset = new Vector3(5f, 0f, 0f);
        [SerializeField] private Transform _destinationMarker;

        [Header("Hotkeys (Play Mode)")]
        [SerializeField] private KeyCode _beginTravelKey = KeyCode.T;
        [SerializeField] private KeyCode _makeCampKey = KeyCode.C;
        [SerializeField] private KeyCode _startRestKey = KeyCode.R;
        [SerializeField] private KeyCode _stopRestKey = KeyCode.X;
        [SerializeField] private KeyCode _resumeMarchKey = KeyCode.M;
        [SerializeField] private KeyCode _cancelTravelKey = KeyCode.V;

        [Header("Debug Overlay (iPad / mobile readable)")]
        [SerializeField, Range(0.8f, 3f)] private float _overlayScale = 1.6f;
        [SerializeField] private int _baseFontSize = 18;
        [SerializeField] private bool _showOverlay = true;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _boxBg;

        private void Awake()
        {
            if (_travel == null)
                _travel = GetComponent<TroopTravelController>();
            if (_morale == null)
                _morale = FindFirstObjectByType<TroopMoraleSystem>();
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
        }

        private void OnDestroy()
        {
            if (_boxBg != null)
                Destroy(_boxBg);
        }

        private void Update()
        {
            if (_travel == null)
                return;

            if (Input.GetKeyDown(_beginTravelKey))
                BeginTravelToOffset();

            if (Input.GetKeyDown(_makeCampKey))
                _travel.StopAndMakeCamp();

            if (Input.GetKeyDown(_startRestKey))
                _travel.StartRest();

            if (Input.GetKeyDown(_stopRestKey))
                _travel.StopRest();

            if (Input.GetKeyDown(_resumeMarchKey))
                _travel.ResumeMarch();

            if (Input.GetKeyDown(_cancelTravelKey))
                _travel.CancelTravel();
        }

        [ContextMenu("Begin Travel To Offset")]
        public void BeginTravelToOffset()
        {
            Vector3 origin = _travel != null && _travel.RuntimeState != null
                ? _travel.RuntimeState.CurrentPosition
                : transform.position;

            Vector3 destination = _destinationMarker != null
                ? _destinationMarker.position
                : origin + _destinationOffset;

            _travel.BeginTravel(destination);
        }

        [ContextMenu("Make Camp")]
        private void DebugMakeCamp() => _travel?.StopAndMakeCamp();

        [ContextMenu("Start Rest")]
        private void DebugStartRest() => _travel?.StartRest();

        [ContextMenu("Stop Rest")]
        private void DebugStopRest() => _travel?.StopRest();

        [ContextMenu("Resume March")]
        private void DebugResumeMarch() => _travel?.ResumeMarch();

        [ContextMenu("Cancel Travel")]
        private void DebugCancelTravel() => _travel?.CancelTravel();

        private void EnsureStyles(float scale)
        {
            int fontSize = Mathf.RoundToInt(_baseFontSize * scale);

            if (_boxBg == null)
            {
                _boxBg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _boxBg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
                _boxBg.Apply();
            }

            if (_boxStyle == null)
                _boxStyle = new GUIStyle(GUI.skin.box);

            _boxStyle.normal.background = _boxBg;
            _boxStyle.normal.textColor = Color.white;
            _boxStyle.fontSize = fontSize;
            _boxStyle.alignment = TextAnchor.UpperLeft;
            _boxStyle.wordWrap = true;
            _boxStyle.padding = new RectOffset(14, 14, 12, 12);

            if (_labelStyle == null)
                _labelStyle = new GUIStyle(GUI.skin.label);

            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = fontSize;
            _labelStyle.alignment = TextAnchor.UpperLeft;
            _labelStyle.wordWrap = true;
            _labelStyle.richText = false;
        }

        private void OnGUI()
        {
            if (!_showOverlay || _travel == null)
                return;

            TravelRuntimeState s = _travel.RuntimeState;
            if (s == null)
                return;

            // Scale up on tablets / high-DPI so text stays readable.
            float dpiScale = Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f) : 1f;
            float heightScale = Mathf.Clamp(Screen.height / 800f, 1f, 2.2f);
            float scale = Mathf.Max(dpiScale, heightScale) * _overlayScale;

            EnsureStyles(scale);

            string moraleLine = _morale != null
                ? $"HP:{_morale.Health:0} Sleep:{_morale.Sleep:0} Water:{_morale.Water:0} Food:{_morale.Food:0}"
                : "Morale: (none)";

            string clock = "";
            if (_worldTime != null)
            {
                _worldTime.GetClock(out int h, out int m, out int sec);
                float nightPct = _worldTime.GetCurrentNightFraction() * 100f;
                clock =
                    $"{h:00}:{m:00}:{sec:00} Day{_worldTime.GetDayIndex()} {s.CurrentDayPhase}\n" +
                    $"Season:{_worldTime.GetCurrentSeasonName()}  night:{nightPct:0.0}%  blend:{_worldTime.GetSeasonBlendProgress():0.00}";
            }

            string weatherLine =
                $"WeatherMove:{s.WeatherMovementMultiplier:0.00}  WeatherNeeds:{s.WeatherNeedsMultiplier:0.00}";

            string text =
                "Travel Debug\n" +
                $"Status: {s.Status}\n" +
                $"Surface: {s.CurrentSurface}  trail:{s.IsOnTrail}  passable:{s.IsPassable}\n" +
                $"Speed: {s.CurrentSpeed:0.00}  rem:{s.RemainingDistance:0.00}\n" +
                $"Route: {(s.HasRoute ? $"{s.RouteWaypointIndex}/{Mathf.Max(0, s.RouteWaypointCount - 1)}" : "none")}\n" +
                $"March:{s.MarchElapsedGameSeconds / 3600.0:0.00}h  Rest:{s.RestElapsedGameSeconds / 3600.0:0.00}h\n" +
                $"{clock}\n" +
                $"{weatherLine}\n" +
                $"{moraleLine}\n" +
                "Keys: T begin | C camp | R rest | X stop rest | M resume | V cancel\n" +
                "Nav: N destination mode → click → Approve";

            float pad = 16f * scale;
            float width = Mathf.Clamp(Screen.width * 0.55f, 420f * scale, Screen.width - pad * 2f);
            float height = Mathf.Min(340f * scale, Screen.height * 0.55f);
            var rect = new Rect(pad, pad, width, height);

            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 20f), text, _labelStyle);
        }
    }
}
