using System.Globalization;
using System.Text;
using UnityEngine;

namespace Game.Battle.DebugTools
{
    public sealed class BattleTestController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BattleSettings _settings;

        [Header("Overlay")]
        [SerializeField] private bool _showOverlay = false;
        [SerializeField, Range(0.8f, 2.5f)] private float _overlayScale = 1.2f;

        [Header("Attacker Input")]
        [SerializeField] private int _attackerInfantry = 5;
        [SerializeField] private int _attackerCavalry;
        [SerializeField] private int _attackerArchers;

        [Header("Defender Input")]
        [SerializeField] private int _defenderInfantry;
        [SerializeField] private int _defenderCavalry = 5;
        [SerializeField] private int _defenderArchers;

        private BattleResult _lastResult;

        private string _attackerInfantryText = "5";
        private string _attackerCavalryText = "0";
        private string _attackerArchersText = "0";
        private string _defenderInfantryText = "0";
        private string _defenderCavalryText = "5";
        private string _defenderArchersText = "0";

        private Vector2 _scroll;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private Texture2D _boxBg;

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => _showOverlay = value;
        }

        public BattleResult LastResult => _lastResult;

        private void OnValidate()
        {
            _attackerInfantry = Mathf.Max(0, _attackerInfantry);
            _attackerCavalry = Mathf.Max(0, _attackerCavalry);
            _attackerArchers = Mathf.Max(0, _attackerArchers);
            _defenderInfantry = Mathf.Max(0, _defenderInfantry);
            _defenderCavalry = Mathf.Max(0, _defenderCavalry);
            _defenderArchers = Mathf.Max(0, _defenderArchers);

            _attackerInfantryText = _attackerInfantry.ToString();
            _attackerCavalryText = _attackerCavalry.ToString();
            _attackerArchersText = _attackerArchers.ToString();
            _defenderInfantryText = _defenderInfantry.ToString();
            _defenderCavalryText = _defenderCavalry.ToString();
            _defenderArchersText = _defenderArchers.ToString();
        }

        private void OnGUI()
        {
            if (!_showOverlay)
                return;

            EnsureStyles();

            float width = 420f * _overlayScale;
            float height = Mathf.Min(Screen.height - 20f, 640f * _overlayScale);
            Rect area = new Rect(12f, 12f, width, height);

            GUILayout.BeginArea(area, _boxStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("BATTLE TEST (Phase 16A)", _titleStyle);
            GUILayout.Space(6f);

            DrawArmyInputs("ATTACKER", ref _attackerInfantryText, ref _attackerCavalryText, ref _attackerArchersText);
            GUILayout.Space(8f);
            DrawArmyInputs("DEFENDER", ref _defenderInfantryText, ref _defenderCavalryText, ref _defenderArchersText);

            GUILayout.Space(8f);
            float defenderAdvantage = _settings != null ? _settings.DefenderNeutralAdvantage : 0f;
            GUILayout.Label(
                $"Defender Advantage (neutral): {defenderAdvantage.ToString("0.###", CultureInfo.InvariantCulture)}",
                _labelStyle);

            GUILayout.Space(10f);
            if (GUILayout.Button("RUN BATTLE", GUILayout.Height(32f * _overlayScale)))
                RunBattle();

            GUILayout.Space(10f);
            DrawResult();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawArmyInputs(
            string title,
            ref string infantryText,
            ref string cavalryText,
            ref string archersText)
        {
            GUILayout.Label(title, _titleStyle);
            DrawCountRow("Infantry", ref infantryText);
            DrawCountRow("Cavalry", ref cavalryText);
            DrawCountRow("Archers", ref archersText);

            int total = ParseCount(infantryText) + ParseCount(cavalryText) + ParseCount(archersText);
            GUILayout.Label($"Total: {total}", _labelStyle);
        }

        private void DrawCountRow(string label, ref string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(90f * _overlayScale));
            text = GUILayout.TextField(text, GUILayout.Width(100f * _overlayScale));
            GUILayout.EndHorizontal();
        }

        private void DrawResult()
        {
            GUILayout.Label("BATTLE RESULT", _titleStyle);

            if (_lastResult == null)
            {
                GUILayout.Label("No result yet. Press RUN BATTLE.", _labelStyle);
                return;
            }

            var sb = new StringBuilder(512);
            sb.AppendLine($"Mode: {_lastResult.Mode}");
            sb.AppendLine($"α (counter anchor): {_lastResult.SampledCounterAdvantage:P1}");
            sb.AppendLine();
            sb.AppendLine($"Attacker start: {_lastResult.AttackerStarting}");
            sb.AppendLine($"Defender start: {_lastResult.DefenderStarting}");
            sb.AppendLine();

            if (_lastResult.Mode == BattleResolveMode.SmallWinProbability)
            {
                sb.AppendLine("Win Probability");
                sb.AppendLine(
                    $"  Attacker  {_lastResult.AttackerWinProbability.ToString("P1", CultureInfo.InvariantCulture)}");
                sb.AppendLine(
                    $"  Defender  {_lastResult.DefenderWinProbability.ToString("P1", CultureInfo.InvariantCulture)}");
            }
            else
            {
                sb.AppendLine("                  Attacker     Defender");
                sb.AppendLine(
                    $"Starting           {_lastResult.AttackerStarting.Total,8}   {_lastResult.DefenderStarting.Total,8}");
                sb.AppendLine(
                    $"Deaths             {_lastResult.AttackerDeaths.Total,8}   {_lastResult.DefenderDeaths.Total,8}");
                sb.AppendLine(
                    $"Remaining          {_lastResult.AttackerRemaining.Total,8}   {_lastResult.DefenderRemaining.Total,8}");
                sb.AppendLine();
                sb.AppendLine("Per-type deaths / remaining");
                sb.AppendLine(
                    $"  Inf  A {_lastResult.AttackerDeaths.Infantry}/{_lastResult.AttackerRemaining.Infantry}" +
                    $"   D {_lastResult.DefenderDeaths.Infantry}/{_lastResult.DefenderRemaining.Infantry}");
                sb.AppendLine(
                    $"  Cav  A {_lastResult.AttackerDeaths.Cavalry}/{_lastResult.AttackerRemaining.Cavalry}" +
                    $"   D {_lastResult.DefenderDeaths.Cavalry}/{_lastResult.DefenderRemaining.Cavalry}");
                sb.AppendLine(
                    $"  Arch A {_lastResult.AttackerDeaths.Archers}/{_lastResult.AttackerRemaining.Archers}" +
                    $"   D {_lastResult.DefenderDeaths.Archers}/{_lastResult.DefenderRemaining.Archers}");
            }

            GUILayout.Label(sb.ToString(), _labelStyle);
        }

        [ContextMenu("Run Battle")]
        public void RunBattle()
        {
            if (_settings == null)
            {
                Debug.LogWarning($"{nameof(BattleTestController)}: BattleSettings is missing.");
                return;
            }

            SyncParsedInputs();

            var input = new BattleInput(
                new ArmyComposition(_attackerInfantry, _attackerCavalry, _attackerArchers),
                new ArmyComposition(_defenderInfantry, _defenderCavalry, _defenderArchers));

            var context = new BattleContext(applyDefenderAdvantage: true, seed: null);
            _lastResult = BattleResolver.Resolve(input, context, _settings);

            Debug.Log(
                $"[BattleTest] mode={_lastResult.Mode} α={_lastResult.SampledCounterAdvantage:F3} " +
                (_lastResult.Mode == BattleResolveMode.SmallWinProbability
                    ? $"P(A)={_lastResult.AttackerWinProbability:P1} P(D)={_lastResult.DefenderWinProbability:P1}"
                    : $"deaths A={_lastResult.AttackerDeaths.Total} D={_lastResult.DefenderDeaths.Total}"));
        }

        private void SyncParsedInputs()
        {
            _attackerInfantry = ParseCount(_attackerInfantryText);
            _attackerCavalry = ParseCount(_attackerCavalryText);
            _attackerArchers = ParseCount(_attackerArchersText);
            _defenderInfantry = ParseCount(_defenderInfantryText);
            _defenderCavalry = ParseCount(_defenderCavalryText);
            _defenderArchers = ParseCount(_defenderArchersText);

            _attackerInfantryText = _attackerInfantry.ToString();
            _attackerCavalryText = _attackerCavalry.ToString();
            _attackerArchersText = _attackerArchers.ToString();
            _defenderInfantryText = _defenderInfantry.ToString();
            _defenderCavalryText = _defenderCavalry.ToString();
            _defenderArchersText = _defenderArchers.ToString();
        }

        private static int ParseCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return int.TryParse(text.Trim(), out int value) ? Mathf.Max(0, value) : 0;
        }

        private void EnsureStyles()
        {
            if (_boxBg == null)
            {
                _boxBg = new Texture2D(1, 1);
                _boxBg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.82f));
                _boxBg.Apply();
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = _boxBg;
                _boxStyle.padding = new RectOffset(10, 10, 10, 10);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(13f * _overlayScale),
                    wordWrap = true,
                    richText = true
                };
                _labelStyle.normal.textColor = Color.white;
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = Mathf.RoundToInt(15f * _overlayScale),
                    fontStyle = FontStyle.Bold
                };
            }
            else
            {
                _labelStyle.fontSize = Mathf.RoundToInt(13f * _overlayScale);
                _titleStyle.fontSize = Mathf.RoundToInt(15f * _overlayScale);
            }
        }

        private void OnDestroy()
        {
            if (_boxBg != null)
                Destroy(_boxBg);
        }
    }
}
