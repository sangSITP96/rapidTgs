using System.Globalization;
using System.Text;
using Game.Battle;
using Game.Core.WorldTime;
using Game.Currency;
using Game.Troops;
using UnityEngine;

namespace Game.Campaign.DebugTools
{
    /// <summary>
    /// Legacy OnGUI overlay. Prefer Unity UI prefabs via CampaignFlowController / CampaignHudRoot.
    /// Keep disabled unless explicitly needed for low-level debugging.
    /// </summary>
    public sealed class CampaignDebugController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private CampaignService _service;
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private CurrencySystem _currency;

        [Header("Overlay")]
        [SerializeField] private bool _showOverlay;
        [SerializeField, Range(0.8f, 2.5f)] private float _overlayScale = 1.15f;

        [Header("Create Inputs")]
        [SerializeField] private int _territoryIndex;
        [SerializeField] private string _ownerId = "local-commander";
        [SerializeField] private string _supportId = "support-commander-b";
        [SerializeField] private string _supportIdC = "support-commander-c";

        [Header("Troop Contribution Inputs (Large Battle Alpha)")]
        [SerializeField] private int _infantry = 40;
        [SerializeField] private int _cavalry = 30;
        [SerializeField] private int _archers = 30;

        private Vector2 _scroll;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _titleStyle;
        private Texture2D _boxBg;
        private string _lastMessage = string.Empty;

        public bool ShowOverlay
        {
            get => _showOverlay;
            set => _showOverlay = value;
        }

        private void Awake()
        {
            ResolveRefs();
        }

        private void ResolveRefs()
        {
            if (_service == null)
                _service = FindFirstObjectByType<CampaignService>();
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();
            if (_currency == null)
                _currency = FindFirstObjectByType<CurrencySystem>();
        }

        private void OnGUI()
        {
            if (!_showOverlay)
                return;

            ResolveRefs();
            EnsureStyles();

            float width = 460f * _overlayScale;
            float height = Mathf.Min(Screen.height - 16f, 720f * _overlayScale);
            Rect area = new Rect(Screen.width - width - 12f, 12f, width, height);

            GUILayout.BeginArea(area, _boxStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("CAMPAIGN DEBUG (Phase 17)", _titleStyle);
            GUILayout.Label(
                "Large Battle Alpha only. Support Silver = TEMPORARY Owner-only payout assumption.",
                _labelStyle);
            GUILayout.Space(6f);

            DrawCreateSection();
            GUILayout.Space(6f);
            DrawAcceptSection();
            GUILayout.Space(6f);
            DrawContributionSection();
            GUILayout.Space(6f);
            DrawLifecycleSection();
            GUILayout.Space(6f);
            DrawStatus();
            GUILayout.Space(6f);
            if (!string.IsNullOrEmpty(_lastMessage))
                GUILayout.Label(_lastMessage, _labelStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawCreateSection()
        {
            GUILayout.Label("1. Create / Identity", _titleStyle);
            _territoryIndex = IntField("Territory Index", _territoryIndex);
            _ownerId = TextField("Owner Id", _ownerId);
            _supportId = TextField("Support B Id", _supportId);
            _supportIdC = TextField("Support C Id", _supportIdC);

            if (GUILayout.Button("Create Campaign (Offered)"))
            {
                EnsureService();
                var c = _service.CreateCampaign(_territoryIndex, _ownerId);
                _lastMessage = c != null ? $"Created {c.CampaignId}" : "Create failed";
            }
        }

        private void DrawAcceptSection()
        {
            GUILayout.Label("2. Accept / Decline", _titleStyle);
            if (GUILayout.Button("Owner Accept"))
            {
                EnsureService();
                _lastMessage = _service.OwnerAccept(_ownerId) ? "Owner Accepted" : "Owner Accept failed";
            }

            if (GUILayout.Button("Owner Decline"))
            {
                EnsureService();
                _lastMessage = _service.OwnerDecline(_ownerId) ? "Owner Declined" : "Owner Decline failed";
            }

            if (GUILayout.Button("Support B Accept"))
            {
                EnsureService();
                _lastMessage = _service.SupportAccept(_supportId) ? "Support B Accepted" : "Support B Accept failed";
            }

            if (GUILayout.Button("Support B Decline"))
            {
                EnsureService();
                _lastMessage = _service.SupportDecline(_supportId) ? "Support B Declined" : "Support B Decline failed";
            }

            if (GUILayout.Button("Support C Accept (reinforcement)"))
            {
                EnsureService();
                _lastMessage = _service.SupportAccept(_supportIdC) ? "Support C Accepted" : "Support C Accept failed";
            }
        }

        private void DrawContributionSection()
        {
            GUILayout.Label("3. Contributions / March / Camp", _titleStyle);
            _infantry = IntField("Infantry", _infantry);
            _cavalry = IntField("Cavalry", _cavalry);
            _archers = IntField("Archers", _archers);
            GUILayout.Label(
                $"Total={_infantry + _cavalry + _archers} (use >= BattleSettings.SmallBattleThreshold for Large)",
                _labelStyle);

            if (GUILayout.Button("Add Owner Contribution (Selected)"))
            {
                EnsureService();
                var unit = BuildUnit("OwnerUnit");
                var contrib = _service.AddOwnerContribution(unit, _ownerId);
                _lastMessage = contrib != null
                    ? $"Owner contrib {contrib.ContributionId} ownership={contrib.CommanderId}"
                    : "Owner contribution failed";
            }

            if (GUILayout.Button("Add Support B Contribution"))
            {
                EnsureService();
                var unit = BuildUnit("SupportBUnit");
                var contrib = _service.AddSupportContribution(_supportId, unit);
                _lastMessage = contrib != null
                    ? $"Support B contrib {contrib.ContributionId} ownership={contrib.CommanderId}"
                    : "Support B contribution failed";
            }

            if (GUILayout.Button("Add Support C Contribution (while Active OK)"))
            {
                EnsureService();
                var unit = BuildUnit("SupportCUnit");
                var contrib = _service.AddSupportContribution(_supportIdC, unit);
                _lastMessage = contrib != null
                    ? $"Support C contrib {contrib.ContributionId} ownership={contrib.CommanderId}"
                    : "Support C contribution failed";
            }

            if (GUILayout.Button("Join Camp Immediate (all Selected → InCamp)"))
            {
                EnsureService();
                int n = JoinAllSelectedToCamp();
                _lastMessage = $"Joined {n} contributions to Camp (sim arrival)";
            }

            if (GUILayout.Button("March Selected → Territory (Travel group)"))
            {
                EnsureService();
                _lastMessage = _service.BeginMarchExistingSelectedContributions()
                    ? "March group started"
                    : "March failed (no Selected contributions?)";
            }
        }

        private void DrawLifecycleSection()
        {
            GUILayout.Label("4. Lifecycle / Time / Battle", _titleStyle);

            if (GUILayout.Button("Start Campaign (Active)"))
            {
                EnsureService();
                _lastMessage = _service.StartCampaign() ? "Campaign Active" : "Start failed";
            }

            if (GUILayout.Button("Force Resolve Battle Now (debug)"))
            {
                EnsureService();
                _lastMessage = _service.DebugForceResolveBattle() ? "Battle resolved" : "Battle failed (see Console)";
            }

            if (GUILayout.Button("Try Scheduled Battle"))
            {
                EnsureService();
                _lastMessage = _service.TryResolveScheduledBattle()
                    ? "Scheduled battle resolved"
                    : "No scheduled battle due (day/night/slots)";
            }

            if (GUILayout.Button("Advance +2 Game Hours"))
            {
                if (_worldTime != null)
                {
                    _worldTime.Advance(2d * WorldTime.SecondsPerHour);
                    _lastMessage = "Advanced 2 game hours";
                }
            }

            if (GUILayout.Button("Advance +6 Game Hours"))
            {
                if (_worldTime != null)
                {
                    _worldTime.Advance(6d * WorldTime.SecondsPerHour);
                    _lastMessage = "Advanced 6 game hours";
                }
            }

            if (GUILayout.Button("Complete Campaign Now"))
            {
                EnsureService();
                _service.CompleteCampaign();
                _lastMessage = "CompleteCampaign called";
            }

            if (GUILayout.Button("Pay Rewards (Owner TEMPORARY Alpha)"))
            {
                EnsureService();
                _lastMessage = _service.TryPayRewards() ? "Reward payout attempted" : "Reward payout failed / already paid";
            }
        }

        private void DrawStatus()
        {
            GUILayout.Label("5. Status", _titleStyle);
            var sb = new StringBuilder(1024);

            if (_worldTime != null)
            {
                _worldTime.GetClock(out int h, out int m, out int s);
                sb.AppendLine(
                    $"WorldTime day={_worldTime.GetDayIndex()} {h:00}:{m:00}:{s:00} " +
                    $"phase={_worldTime.GetDayPhase()} night={_worldTime.IsNight()}");
            }

            if (_currency != null)
                sb.AppendLine($"Silver={_currency.Silver} Gold={_currency.Gold}");

            Campaign c = _service != null ? _service.ActiveCampaign : null;
            if (c == null)
            {
                sb.AppendLine("No active campaign.");
                GUILayout.Label(sb.ToString(), _labelStyle);
                return;
            }

            sb.AppendLine($"Id={c.CampaignId}");
            sb.AppendLine($"Territory={c.TerritoryIndex} Owner={c.OwnerCommanderId} OwnerResp={c.OwnerResponse}");
            sb.AppendLine($"State={c.LifecycleState} Outcome={c.Outcome}");
            sb.AppendLine($"Controller={c.CurrentController}");
            sb.AppendLine(
                $"CmdControlH={(c.CommanderControlDurationGameSeconds / 3600d).ToString("F2", CultureInfo.InvariantCulture)} " +
                $"FistControlH={(c.FistControlDurationGameSeconds / 3600d).ToString("F2", CultureInfo.InvariantCulture)}");
            sb.AppendLine($"RecordedOwnerTerritoryWin={c.RecordedOwnerTerritoryWin} RewardPaid={c.RewardPaid}");
            sb.AppendLine(
                $"BattlesToday={c.BattlesCompletedToday} History={c.BattleHistory.Count} NextIdx={c.NextBattleIndex}");

            ArmyComposition agg = _service.GetAggregatedInCampForce();
            sb.AppendLine($"InCamp Aggregate: {agg}");

            sb.AppendLine("--- Owner Contributions ---");
            for (int i = 0; i < c.OwnerContributions.Count; i++)
            {
                var x = c.OwnerContributions[i];
                if (x == null) continue;
                sb.AppendLine($"  {x.ContributionId} owner={x.CommanderId} {x.Status} {x.GetComposition()}");
            }

            sb.AppendLine("--- Supports ---");
            for (int s = 0; s < c.Supports.Count; s++)
            {
                var support = c.Supports[s];
                if (support == null) continue;
                sb.AppendLine($"  {support.CommanderId} resp={support.Response} contribs={support.Contributions.Count}");
                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var x = support.Contributions[i];
                    if (x == null) continue;
                    sb.AppendLine($"    {x.ContributionId} {x.Status} {x.GetComposition()}");
                }
            }

            sb.AppendLine("--- Battle History ---");
            for (int i = 0; i < c.BattleHistory.Count; i++)
            {
                var b = c.BattleHistory[i];
                if (b == null) continue;
                sb.AppendLine(
                    $"  #{b.BattleIndex} {b.SideResult} " +
                    $"CmdWP={b.CommanderWinningPercentage:P1} FistWP={b.FistWinningPercentage:P1} " +
                    $"sent {b.CommanderTroopsSent.Total}/{b.FistTroopsSent.Total} " +
                    $"surv {b.CommanderSurviving.Total}/{b.FistSurviving.Total}");
            }

            if (_service.Settings != null)
            {
                int silver = CampaignRewardCalculator.CalculateSilverAmount(c, _service.Settings);
                sb.AppendLine(
                    $"Projected Silver (formula)={silver} " +
                    $"wins={CampaignRewardCalculator.CountBattleWins(c)} " +
                    $"losses={CampaignRewardCalculator.CountBattleLosses(c)} " +
                    $"draws={CampaignRewardCalculator.CountBattleDraws(c)}");
                sb.AppendLine(
                    $"TEMPORARY Alpha RewardOwnerOnly={_service.Settings.TemporaryAlphaRewardOwnerOnly}");
            }

            GUILayout.Label(sb.ToString(), _labelStyle);
        }

        private TroopUnit BuildUnit(string name)
        {
            var unit = new TroopUnit(TroopUnitType.Company, name);
            unit.TrySetDirectTroops(_infantry, _cavalry, _archers, out _);
            return unit;
        }

        private int JoinAllSelectedToCamp()
        {
            Campaign c = _service.ActiveCampaign;
            if (c == null)
                return 0;

            int n = 0;
            for (int i = 0; i < c.OwnerContributions.Count; i++)
            {
                var x = c.OwnerContributions[i];
                if (x != null && x.Status == TroopContributionStatus.Selected)
                {
                    _service.JoinCampImmediate(x.ContributionId);
                    n++;
                }
            }

            for (int s = 0; s < c.Supports.Count; s++)
            {
                var support = c.Supports[s];
                if (support == null) continue;
                for (int i = 0; i < support.Contributions.Count; i++)
                {
                    var x = support.Contributions[i];
                    if (x != null &&
                        (x.Status == TroopContributionStatus.Selected ||
                         x.Status == TroopContributionStatus.Marching))
                    {
                        _service.JoinCampImmediate(x.ContributionId);
                        n++;
                    }
                }
            }

            return n;
        }

        private void EnsureService()
        {
            ResolveRefs();
            if (_service == null)
            {
                var go = new GameObject("CampaignService");
                _service = go.AddComponent<CampaignService>();
            }

            _service.ResolveRefs();
        }

        private static int IntField(string label, int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140f));
            string text = GUILayout.TextField(value.ToString());
            GUILayout.EndHorizontal();
            return int.TryParse(text, out int parsed) ? parsed : value;
        }

        private string TextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(140f));
            value = GUILayout.TextField(value ?? string.Empty);
            GUILayout.EndHorizontal();
            return value;
        }

        private void EnsureStyles()
        {
            if (_boxBg == null)
            {
                _boxBg = new Texture2D(1, 1);
                _boxBg.SetPixel(0, 0, new Color(0.05f, 0.08f, 0.12f, 0.88f));
                _boxBg.Apply();
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = _boxBg },
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(12f * _overlayScale),
                    wordWrap = true,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = Mathf.RoundToInt(14f * _overlayScale),
                    fontStyle = FontStyle.Bold
                };
            }
            else
            {
                _labelStyle.fontSize = Mathf.RoundToInt(12f * _overlayScale);
                _titleStyle.fontSize = Mathf.RoundToInt(14f * _overlayScale);
            }
        }

        private void OnDestroy()
        {
            if (_boxBg != null)
                Destroy(_boxBg);
        }
    }
}
