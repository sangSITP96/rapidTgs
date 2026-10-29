using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Battle;
using Game.Morale;
using Game.Navigation;
using Game.Seasons;
using Game.Travel;
using Game.Troops;
using TGS;
using UnityEngine;
using CampaignModel = Game.Campaign.Campaign;
using CampaignSettings = Game.Campaign.CampaignSettings;
using CampaignTerritoryLabel = Game.Campaign.CampaignTerritoryLabel;

namespace Game.General
{
    /// <summary>
    /// Read-only advisor. Builds briefing / troop / route / monitoring advice from existing systems.
    /// Never accepts campaigns, selects troops, approves routes, marches, or rests.
    /// </summary>
    [DefaultExecutionOrder(15)]
    public sealed class GeneralAdvisor : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Game.Campaign.CampaignService _campaign;
        [SerializeField] private TemporaryAlphaTroopRoster _roster;
        [SerializeField] private TroopTravelController _travel;
        [SerializeField] private TroopMoraleSystem _morale;
        [SerializeField] private TgsNavigationPathfinder _pathfinder;
        [SerializeField] private TravelSurfaceQuery _surfaceQuery;
        [SerializeField] private TravelWeatherAdapter _weatherAdapter;
        [SerializeField] private WeatherManager _weatherManager;
        [SerializeField] private SeasonWeatherDirector _seasonWeather;
        [SerializeField] private TgsBiomeMapData _biomeMapData;
        [SerializeField] private TerrainGridSystem _tgs;

        [Header("Presentation")]
        [SerializeField] private string _generalDisplayName = "The General";

        public string GeneralDisplayName => _generalDisplayName;

        private void Awake()
        {
            ResolveRefs();
        }

        public void ResolveRefs()
        {
            if (_campaign == null)
                _campaign = FindFirstObjectByType<Game.Campaign.CampaignService>();
            if (_roster == null)
                _roster = FindFirstObjectByType<TemporaryAlphaTroopRoster>();
            if (_travel == null)
                _travel = FindFirstObjectByType<TroopTravelController>();
            if (_morale == null)
                _morale = FindFirstObjectByType<TroopMoraleSystem>();
            if (_pathfinder == null)
                _pathfinder = FindFirstObjectByType<TgsNavigationPathfinder>();
            if (_surfaceQuery == null)
                _surfaceQuery = FindFirstObjectByType<TravelSurfaceQuery>();
            if (_weatherAdapter == null)
                _weatherAdapter = FindFirstObjectByType<TravelWeatherAdapter>();
            if (_weatherManager == null)
                _weatherManager = FindFirstObjectByType<WeatherManager>();
            if (_seasonWeather == null)
                _seasonWeather = FindFirstObjectByType<SeasonWeatherDirector>();
            if (_tgs == null)
                _tgs = FindFirstObjectByType<TerrainGridSystem>();

            if (_biomeMapData == null)
            {
                var gen = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (gen != null)
                    _biomeMapData = gen.MapData;
            }
        }

        public GeneralBriefingAdvice BuildCampaignBriefing(CampaignModel campaign = null)
        {
            ResolveRefs();
            campaign ??= _campaign != null ? _campaign.ActiveCampaign : null;

            var advice = new GeneralBriefingAdvice
            {
                GeneralMessage = string.Empty
            };

            if (campaign == null)
            {
                advice.GeneralMessage = "No campaign offer is available.";
                advice.BodyLines.Add("No active campaign offer.");
                return advice;
            }

            advice.ObjectiveLabel = CampaignTerritoryLabel.GetObjectivePlaceholder();
            advice.TerritoryIndex = campaign.TerritoryIndex;
            advice.LocationLabel = CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData);
            advice.HasDestination = campaign.HasMarchDestination;
            advice.DestinationWorld = campaign.MarchDestinationWorld;
            advice.TerrainLabel = advice.LocationLabel;

            FillWeather(advice);

            CampaignSettings settings = _campaign != null ? _campaign.Settings : null;
            if (settings != null)
            {
                advice.BaseSilverReward = settings.BaseSilverPayment;
                advice.RewardNote =
                    $"Base Silver {settings.BaseSilverPayment}. Final payout adjusts after battles " +
                    $"(+{settings.SilverPerBattleWin} per Commander win, {settings.SilverPerBattleLoss} per loss).";
                advice.CampaignDurationNote =
                    $"Campaign duration: {settings.CampaignDurationGameHours.ToString("0.##", CultureInfo.InvariantCulture)} game hours.";
                advice.BattleScheduleNote =
                    $"Scheduled battles: up to {settings.BattlesPerDay}/day while Active " +
                    $"(~{settings.BattleDurationGameHours.ToString("0.##", CultureInfo.InvariantCulture)}h each).";
            }
            else
            {
                advice.RewardNote = "Reward settings unavailable.";
            }

            // Risk factors: only extras not already listed in the summary above.
            AppendTroopConditionRisk(advice.RiskFactors);

            if (_campaign != null &&
                _campaign.TryGetCampaignDestination(out Vector3 destination) &&
                _pathfinder != null &&
                _travel != null)
            {
                Vector3 origin = _travel.transform.position;
                NavigationRoute route = _pathfinder.FindRouteToWorld(origin, destination);
                if (route != null && route.IsValid)
                {
                    advice.HasRoute = true;
                    advice.ApproximateDistance = route.ApproximateDistance;
                    advice.RouteCellCount = route.CellIndices != null ? route.CellIndices.Count : 0;
                }
                else
                {
                    advice.HasRoute = false;
                    advice.RouteFailureReason = route != null ? route.FailureReason : "No route.";
                    advice.RiskFactors.Add(new AdviceFactor(
                        AdviceFactorKind.Known,
                        "Route preview failed",
                        advice.RouteFailureReason));
                }
            }

            BuildBriefingBody(advice);
            return advice;
        }

        public GeneralTroopAdvice BuildTroopAdvice(CampaignModel campaign = null)
        {
            ResolveRefs();
            campaign ??= _campaign != null ? _campaign.ActiveCampaign : null;

            var advice = new GeneralTroopAdvice
            {
                GeneralMessage =
                    "Available forces are listed below. Exact F.I.S.T. composition is unknown until battle — " +
                    "I cannot judge an optimal mix against an unseen enemy. You choose which units to send.",
                UncertaintyNote =
                    "Exact F.I.S.T. forces remain unknown until battle."
            };

            IReadOnlyList<TroopUnit> available = _roster != null
                ? _roster.GetAvailableUnits()
                : System.Array.Empty<TroopUnit>();

            var totals = new ArmyComposition(0, 0, 0);
            for (int i = 0; i < available.Count; i++)
            {
                TroopUnit unit = available[i];
                if (unit == null)
                    continue;
                var summary = new AvailableTroopUnitSummary(unit);
                advice.AvailableUnits.Add(summary);
                totals += summary.Composition;
            }

            advice.AvailableTotals = totals;

            if (campaign != null)
            {
                advice.ContextFactors.Add(new AdviceFactor(
                    AdviceFactorKind.Known,
                    "Objective",
                    CampaignTerritoryLabel.GetObjectivePlaceholder()));
                advice.ContextFactors.Add(new AdviceFactor(
                    AdviceFactorKind.Known,
                    "Location",
                    CampaignTerritoryLabel.GetDisplayName(campaign.TerritoryIndex, _biomeMapData)));
            }

            advice.ContextFactors.Add(new AdviceFactor(
                AdviceFactorKind.Unknown,
                "Enemy force mix",
                "Unknown until battle."));

            if (_morale != null)
            {
                advice.TroopConditionNote =
                    $"Current march-group condition — Morale {_morale.Morale:0}, " +
                    $"Health {_morale.Health:0}, Sleep {_morale.Sleep:0}, " +
                    $"Food {_morale.Food:0}, Water {_morale.Water:0}.";
                advice.ContextFactors.Add(new AdviceFactor(
                    AdviceFactorKind.Known,
                    "Troop condition (shared march group)",
                    advice.TroopConditionNote));
            }
            else
            {
                advice.TroopConditionNote = "Troop condition data unavailable.";
            }

            advice.BodyLines.Add(advice.GeneralMessage);
            advice.BodyLines.Add(
                $"Available total: I {totals.Infantry} / C {totals.Cavalry} / A {totals.Archers} " +
                $"(headcount {totals.Total}).");
            for (int i = 0; i < advice.AvailableUnits.Count; i++)
            {
                AvailableTroopUnitSummary u = advice.AvailableUnits[i];
                advice.BodyLines.Add(
                    $"• {u.Name} ({u.UnitType}) — I:{u.Composition.Infantry} C:{u.Composition.Cavalry} " +
                    $"A:{u.Composition.Archers} (cap {u.Capacity})");
            }

            advice.BodyLines.Add(advice.UncertaintyNote);
            if (!string.IsNullOrEmpty(advice.TroopConditionNote))
                advice.BodyLines.Add(advice.TroopConditionNote);

            return advice;
        }

        public GeneralRouteAdvice BuildRouteAdvice(NavigationRoute route, Vector3 destinationWorld)
        {
            ResolveRefs();

            var advice = new GeneralRouteAdvice
            {
                DestinationWorld = destinationWorld,
                Route = route,
                WeatherLabel = GetWeatherLabel(),
                WeatherMovementMultiplier = GetWeatherMovementMultiplier()
            };

            if (route == null || !route.IsValid)
            {
                advice.IsValid = false;
                advice.FailureReason = route != null ? route.FailureReason : "No route.";
                advice.GeneralMessage = string.Empty;
                advice.RiskFactors.Add(new AdviceFactor(
                    AdviceFactorKind.Known,
                    "Route invalid",
                    advice.FailureReason));
                advice.BodyLines.Add(advice.FailureReason);
                advice.BodyLines.Add($"Weather: {advice.WeatherLabel}");
                return advice;
            }

            advice.IsValid = true;
            advice.ApproximateDistance = route.ApproximateDistance;
            advice.RouteCellCount = route.CellIndices != null ? route.CellIndices.Count : 0;
            advice.TerrainAlongRouteSummary = SummarizeRouteTerrain(route);
            advice.GeneralMessage = string.Empty;

            // Risk factors: only extras not already listed in the summary above.
            AppendTroopConditionRisk(advice.RiskFactors);

            advice.BodyLines.Add(
                $"Distance: ≈ {advice.ApproximateDistance.ToString("0.00", CultureInfo.InvariantCulture)}");
            advice.BodyLines.Add($"Terrain: {advice.TerrainAlongRouteSummary}");
            advice.BodyLines.Add($"Weather: {advice.WeatherLabel}");
            return advice;
        }

        public GeneralMonitoringAdvice BuildMonitoringAdvice()
        {
            ResolveRefs();

            var advice = new GeneralMonitoringAdvice();
            TravelStatus status = _travel != null ? _travel.Status : TravelStatus.Idle;
            advice.TravelStatus = status;

            if (_morale != null)
            {
                advice.Health = _morale.Health;
                advice.Morale = _morale.Morale;
                advice.Sleep = _morale.Sleep;
                advice.Food = _morale.Food;
                advice.Water = _morale.Water;
            }

            float sleepThreshold = GetHealthDamageThreshold();
            bool alreadyResting = status == TravelStatus.Resting;
            bool canRest =
                status == TravelStatus.Marching ||
                status == TravelStatus.Camped ||
                status == TravelStatus.Idle;

            // Existing behavior (TravelNeedsSimulator): Resting → RestoreSleep;
            // Water/Food still consumed while Resting. Rest does not restore Health/Food/Water.
            // NeedsHealthModifier: Sleep below HealthDamageThreshold contributes to Health drain.
            bool sleepLow = _morale != null && advice.Sleep < sleepThreshold;
            advice.RecommendRest = canRest && !alreadyResting && sleepLow;
            advice.CanOfferRestAction = canRest && !alreadyResting;
            advice.CanOfferResumeAction =
                _travel != null &&
                _travel.HasActiveRoute &&
                (status == TravelStatus.Resting || status == TravelStatus.Camped);

            if (advice.RecommendRest)
            {
                advice.RestRecommendationReason =
                    $"Sleep is {advice.Sleep:0} (below needs threshold {sleepThreshold:0}). " +
                    "Rest restores Sleep. Low Sleep contributes to Health loss while needs stay low. " +
                    "Rest does not restore Food or Water (they still deplete while resting).";
                advice.GeneralMessage =
                    "Commander, Sleep is low. I recommend Rest so the troops can recover Sleep before continuing.";
            }
            else if (alreadyResting)
            {
                advice.GeneralMessage =
                    "Troops are Resting. Sleep is recovering; Food and Water still deplete during Rest.";
                advice.RestRecommendationReason = string.Empty;
            }
            else if (status == TravelStatus.Marching)
            {
                advice.GeneralMessage = "Troops are marching. Needs continue to deplete on the road.";
            }
            else if (status == TravelStatus.Arrived)
            {
                advice.GeneralMessage = "Troops have arrived at the campaign destination.";
            }
            else
            {
                advice.GeneralMessage = "Monitoring troop condition.";
            }

            advice.BodyLines.Add(advice.GeneralMessage);
            advice.BodyLines.Add($"Travel: {status}");
            if (_morale != null)
            {
                advice.BodyLines.Add(
                    $"Morale {advice.Morale:0} | Health {advice.Health:0} | Sleep {advice.Sleep:0} | " +
                    $"Food {advice.Food:0} | Water {advice.Water:0}");
            }

            if (advice.RecommendRest && !string.IsNullOrEmpty(advice.RestRecommendationReason))
                advice.BodyLines.Add(advice.RestRecommendationReason);

            return advice;
        }

        private void FillWeather(GeneralBriefingAdvice advice)
        {
            advice.WeatherLabel = GetWeatherLabel();
            advice.WeatherMovementMultiplier = GetWeatherMovementMultiplier();
        }

        private string GetWeatherLabel()
        {
            if (_seasonWeather != null)
                return _seasonWeather.CurrentWeatherKind.ToString();

            if (_weatherManager != null && _weatherManager.Database != null && _weatherManager.Database.entries != null)
            {
                var active = new List<string>();
                for (int i = 0; i < _weatherManager.Database.entries.Count; i++)
                {
                    WeatherEntry entry = _weatherManager.Database.entries[i];
                    if (entry != null && entry.isActive)
                        active.Add(entry.Type.ToString());
                }

                if (active.Count > 0)
                    return string.Join(", ", active);
            }

            return "Clear / unspecified";
        }

        private float GetWeatherMovementMultiplier()
        {
            if (_weatherAdapter != null)
                return _weatherAdapter.GetMovementMultiplier();
            if (_weatherManager != null)
                return _weatherManager.GetTravelMovementMultiplier();
            return 1f;
        }

        private float GetHealthDamageThreshold()
        {
            return _morale != null ? _morale.HealthDamageThreshold : 40f;
        }

        private void AppendTroopConditionRisk(List<AdviceFactor> factors)
        {
            if (_morale == null || factors == null)
                return;

            factors.Add(new AdviceFactor(
                AdviceFactorKind.Known,
                "Troop condition",
                $"Morale {_morale.Morale:0}, Health {_morale.Health:0}, Sleep {_morale.Sleep:0}, " +
                $"Food {_morale.Food:0}, Water {_morale.Water:0}"));
        }

        private void AppendRouteTerrainRisk(List<AdviceFactor> factors, NavigationRoute route)
        {
            string summary = SummarizeRouteTerrain(route);
            if (!string.IsNullOrEmpty(summary))
            {
                factors.Add(new AdviceFactor(
                    AdviceFactorKind.Known,
                    "Terrain along preview route",
                    summary));
            }
        }

        private string SummarizeRouteTerrain(NavigationRoute route)
        {
            if (route == null)
                return "n/a";

            var counts = new Dictionary<TravelSurfaceType, int>();
            if (_surfaceQuery != null && route.Waypoints != null && route.Waypoints.Count > 0)
            {
                for (int i = 0; i < route.Waypoints.Count; i++)
                {
                    TravelSurfaceSample sample = _surfaceQuery.Sample(route.Waypoints[i]);
                    TravelSurfaceType type = sample.SurfaceType;
                    if (!counts.ContainsKey(type))
                        counts[type] = 0;
                    counts[type]++;
                }
            }
            else if (_biomeMapData != null && _tgs != null && route.CellIndices != null)
            {
                for (int i = 0; i < route.CellIndices.Count; i++)
                {
                    int cellIndex = route.CellIndices[i];
                    if (cellIndex < 0 || cellIndex >= _tgs.cells.Count)
                        continue;
                    int territory = _tgs.cells[cellIndex].territoryIndex;
                    BiomeType biome = _biomeMapData.GetBiomeForTerritory(territory);
                    TravelSurfaceType type = BiomeToSurface(biome);
                    if (!counts.ContainsKey(type))
                        counts[type] = 0;
                    counts[type]++;
                }
            }

            if (counts.Count == 0)
                return "Terrain samples unavailable";

            var sb = new StringBuilder();
            bool first = true;
            foreach (var pair in counts)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(pair.Key).Append(' ').Append(pair.Value);
            }

            return sb.ToString();
        }

        private static TravelSurfaceType BiomeToSurface(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Lake: return TravelSurfaceType.Lake;
                case BiomeType.Mountain: return TravelSurfaceType.Mountain;
                case BiomeType.Forest: return TravelSurfaceType.Forest;
                default: return TravelSurfaceType.Grassland;
            }
        }

        private static void BuildBriefingBody(GeneralBriefingAdvice advice)
        {
            advice.BodyLines.Add($"Objective: {advice.ObjectiveLabel}");
            advice.BodyLines.Add($"Location: {advice.LocationLabel}");
            if (advice.HasRoute)
            {
                advice.BodyLines.Add(
                    $"Distance: ≈ {advice.ApproximateDistance.ToString("0.00", CultureInfo.InvariantCulture)}");
            }
            else if (!string.IsNullOrEmpty(advice.RouteFailureReason))
            {
                advice.BodyLines.Add($"Distance: route unavailable — {advice.RouteFailureReason}");
            }

            advice.BodyLines.Add($"Terrain: {advice.TerrainLabel}");
            advice.BodyLines.Add($"Weather: {advice.WeatherLabel}");
            advice.BodyLines.Add($"Reward: {advice.RewardNote}");

            bool wroteRiskHeader = false;
            for (int i = 0; i < advice.RiskFactors.Count; i++)
            {
                AdviceFactor factor = advice.RiskFactors[i];
                if (factor == null || factor.Kind == AdviceFactorKind.Unknown)
                    continue;
                if (!wroteRiskHeader)
                {
                    advice.BodyLines.Add("Risk factors:");
                    wroteRiskHeader = true;
                }

                advice.BodyLines.Add("• " + factor);
            }
        }
    }
}
