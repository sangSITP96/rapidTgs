using System.Text;
using Game.Core.WorldTime;
using Game.UI;
using UnityEngine;

namespace Game.Seasons.DebugTools
{
    /// <summary>
    /// Develop-only Season / weather controls. Not shown in Production builds.
    /// </summary>
    public sealed class SeasonDebugController : MonoBehaviour
    {
        [SerializeField] private SeasonSystem _seasonSystem;
        [SerializeField] private SeasonWeatherDirector _director;
        [SerializeField] private WorldTime _worldTime;

        [Header("Overlay")]
        [SerializeField] private bool _showOverlay = true;
        [SerializeField, Range(0.8f, 3f)] private float _overlayScale = 1.35f;
        [SerializeField] private int _baseFontSize = 16;

        [Header("Hotkeys (Play Mode, Develop only)")]
        [SerializeField] private KeyCode _toggleOverlayKey = KeyCode.F8;
        [SerializeField] private KeyCode _advanceHourKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode _advanceDayKey = KeyCode.LeftBracket;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _boxBg;

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => _showOverlay = value;
        }

        private void Awake()
        {
            ResolveRefs();
        }

        private void Update()
        {
            if (!IsDevelop())
                return;

            ResolveRefs();

            if (Input.GetKeyDown(_toggleOverlayKey))
                _showOverlay = !_showOverlay;

            if (_seasonSystem == null)
                return;

            if (Input.GetKeyDown(_advanceHourKey))
                _seasonSystem.AdvanceGameHours(1f);

            if (Input.GetKeyDown(_advanceDayKey))
                _seasonSystem.AdvanceGameDays(1f);
        }

        private void ResolveRefs()
        {
            if (_seasonSystem == null)
                _seasonSystem = SeasonSystem.Instance ?? FindFirstObjectByType<SeasonSystem>();
            if (_director == null)
                _director = SeasonWeatherDirector.Instance ?? FindFirstObjectByType<SeasonWeatherDirector>();
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
        }

        private static bool IsDevelop()
        {
            return UiOverlayExclusive.Instance == null || UiOverlayExclusive.Instance.IsDevelopBuild;
        }

        [ContextMenu("Force Spring")]
        public void ForceSpring() => Force(SeasonId.Spring);

        [ContextMenu("Force Summer")]
        public void ForceSummer() => Force(SeasonId.Summer);

        [ContextMenu("Force Autumn")]
        public void ForceAutumn() => Force(SeasonId.Autumn);

        [ContextMenu("Force Winter")]
        public void ForceWinter() => Force(SeasonId.Winter);

        [ContextMenu("Clear Forced Season")]
        public void ClearForcedSeason() => _seasonSystem?.ClearForcedSeason();

        [ContextMenu("Jump To Spring Start")]
        public void JumpToSpring() => _seasonSystem?.JumpToSeasonStart(SeasonId.Spring);

        [ContextMenu("Jump To Summer Start")]
        public void JumpToSummer() => _seasonSystem?.JumpToSeasonStart(SeasonId.Summer);

        [ContextMenu("Jump To Autumn Start")]
        public void JumpToAutumn() => _seasonSystem?.JumpToSeasonStart(SeasonId.Autumn);

        [ContextMenu("Jump To Winter Start")]
        public void JumpToWinter() => _seasonSystem?.JumpToSeasonStart(SeasonId.Winter);

        [ContextMenu("Advance 1 Game Hour")]
        public void AdvanceOneHour() => _seasonSystem?.AdvanceGameHours(1f);

        [ContextMenu("Advance 1 Game Day")]
        public void AdvanceOneDay() => _seasonSystem?.AdvanceGameDays(1f);

        [ContextMenu("Advance 7 Game Days")]
        public void AdvanceOneSeason() => _seasonSystem?.AdvanceGameDays(7f);

        public void Force(SeasonId season)
        {
            ResolveRefs();
            _seasonSystem?.ForceSeason(season);
            _director?.RebuildScheduleIfNeeded(force: true);
            if (_worldTime != null)
                _director?.ApplyAt(_worldTime.TotalGameSeconds, force: true);
        }

        public void DebugTriggerAllowedWeather(SeasonWeatherKind kind)
        {
            ResolveRefs();
            _director?.DebugForceWeather(kind, 2f);
        }

        private void OnGUI()
        {
            if (!_showOverlay || !IsDevelop() || _seasonSystem == null)
                return;

            float dpiScale = Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi / 160f, 1f, 2.5f) : 1f;
            float scale = dpiScale * _overlayScale;
            EnsureStyles(scale);

            var sb = new StringBuilder(512);
            sb.AppendLine("Season Debug (Develop)");
            sb.AppendLine($"Season: {_seasonSystem.CurrentSeason}" +
                          (_seasonSystem.HasForcedSeason ? " [FORCED]" : ""));
            sb.AppendLine($"DayInSeason: {_seasonSystem.DayInSeason + 1}/{_seasonSystem.DaysPerSeason}" +
                          $"  progress:{_seasonSystem.SeasonProgress01:0.00}");
            sb.AppendLine($"Weekday: {_seasonSystem.GetCurrentWeekdayName()}");
            sb.AppendLine($"Transition: {(_seasonSystem.IsTransitioning ? $"{_seasonSystem.TransitionProgress01:0.00}" : "done")}");
            sb.AppendLine($"Temp: {_seasonSystem.GetDisplayTemperatureText()}");

            if (_worldTime != null)
            {
                _worldTime.GetClock(out int h, out int m, out int s);
                sb.AppendLine($"Clock: {h:00}:{m:00}:{s:00}  Day{_worldTime.GetDayIndex()}  night:{_worldTime.GetCurrentNightFraction() * 100f:0.0}%");
            }

            SeasonDefinition def = _seasonSystem.GetCurrentDefinition();
            if (def != null)
            {
                sb.Append("Allowed: ");
                AppendAllowed(sb, def, true);
                sb.AppendLine();
                sb.Append("Disallowed: ");
                AppendAllowed(sb, def, false);
                sb.AppendLine();
            }

            if (_director != null)
            {
                sb.AppendLine($"Weather: {_director.CurrentWeatherKind}");
                if (_director.ActiveEntry != null)
                {
                    double rem = _director.ActiveEntry.EndGameSeconds -
                                 (_worldTime != null ? _worldTime.TotalGameSeconds : 0d);
                    sb.AppendLine($"Duration left: {rem / 3600d:0.00}h");
                }

                sb.Append("Upcoming: ");
                bool any = false;
                foreach (var e in _director.GetUpcoming(5))
                {
                    any = true;
                    double startIn = e.StartGameSeconds -
                                     (_worldTime != null ? _worldTime.TotalGameSeconds : 0d);
                    sb.Append($"{e.Kind}(+{startIn / 3600d:0.0}h) ");
                }

                if (!any)
                    sb.Append("(none)");
                sb.AppendLine();
            }

            sb.AppendLine("F8 overlay | [ -1 day | ] +1 hour");
            sb.AppendLine("ContextMenu: Force/Jump seasons, advance time");

            float pad = 12f * scale;
            float width = Mathf.Clamp(Screen.width * 0.42f, 360f * scale, Screen.width - pad * 2f);
            float height = Mathf.Min(420f * scale, Screen.height * 0.7f);
            var rect = new Rect(Screen.width - width - pad, pad, width, height);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), sb.ToString(), _labelStyle);

            DrawQuickButtons(rect, scale);
        }

        private void DrawQuickButtons(Rect overlayRect, float scale)
        {
            float y = overlayRect.yMax + 6f * scale;
            float x = overlayRect.x;
            float bw = 70f * scale;
            float bh = 28f * scale;
            float gap = 4f * scale;

            if (GUI.Button(new Rect(x, y, bw, bh), "Spring"))
                Force(SeasonId.Spring);
            if (GUI.Button(new Rect(x + (bw + gap), y, bw, bh), "Summer"))
                Force(SeasonId.Summer);
            if (GUI.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Autumn"))
                Force(SeasonId.Autumn);
            if (GUI.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Winter"))
                Force(SeasonId.Winter);

            y += bh + gap;
            if (GUI.Button(new Rect(x, y, bw * 1.4f, bh), "+1 Hour"))
                _seasonSystem.AdvanceGameHours(1f);
            if (GUI.Button(new Rect(x + bw * 1.4f + gap, y, bw * 1.4f, bh), "+1 Day"))
                _seasonSystem.AdvanceGameDays(1f);
            if (GUI.Button(new Rect(x + (bw * 1.4f + gap) * 2f, y, bw * 1.6f, bh), "+7 Days"))
                _seasonSystem.AdvanceGameDays(7f);
        }

        private static void AppendAllowed(StringBuilder sb, SeasonDefinition def, bool allowed)
        {
            bool first = true;
            for (int i = 0; i < def.WeatherRules.Count; i++)
            {
                SeasonWeatherEventRule rule = def.WeatherRules[i];
                if (rule == null || rule.Allowed != allowed)
                    continue;
                if (rule.Kind == SeasonWeatherKind.MightyStorm && !def.MightyStormEnabled && allowed)
                    continue;
                if (!first)
                    sb.Append(", ");
                sb.Append(rule.Kind);
                first = false;
            }

            if (first)
                sb.Append("-");
        }

        private void EnsureStyles(float scale)
        {
            int fontSize = Mathf.RoundToInt(_baseFontSize * scale);

            if (_boxBg == null)
            {
                _boxBg = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _boxBg.SetPixel(0, 0, new Color(0.05f, 0.12f, 0.18f, 0.78f));
                _boxBg.Apply();
            }

            if (_boxStyle == null)
                _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _boxBg;

            if (_labelStyle == null)
                _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = fontSize;
            _labelStyle.wordWrap = true;
            _labelStyle.alignment = TextAnchor.UpperLeft;
        }

        private void OnDestroy()
        {
            if (_boxBg != null)
                Destroy(_boxBg);
        }
    }
}
