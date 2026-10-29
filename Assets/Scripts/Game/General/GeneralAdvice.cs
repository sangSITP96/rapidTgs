using System;
using System.Collections.Generic;
using Game.Battle;
using Game.Navigation;
using Game.Travel;
using Game.Troops;
using UnityEngine;

namespace Game.General
{
    public enum AdviceFactorKind
    {
        Known = 0,
        Unknown = 1
    }

    [Serializable]
    public sealed class AdviceFactor
    {
        public AdviceFactorKind Kind;
        public string Label;
        public string Detail;

        public AdviceFactor(AdviceFactorKind kind, string label, string detail)
        {
            Kind = kind;
            Label = label ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Detail))
                return Label;
            return $"{Label} — {Detail}";
        }
    }

    [Serializable]
    public sealed class GeneralBriefingAdvice
    {
        public string GeneralMessage;
        public string ObjectiveLabel;
        public string LocationLabel;
        public int TerritoryIndex = -1;
        public Vector3 DestinationWorld;
        public bool HasDestination;

        public bool HasRoute;
        public float ApproximateDistance;
        public int RouteCellCount;
        public string RouteFailureReason;

        public string TerrainLabel;
        public string WeatherLabel;
        public float WeatherMovementMultiplier = 1f;

        public int BaseSilverReward;
        public string RewardNote;

        public string CampaignDurationNote;
        public string BattleScheduleNote;

        public readonly List<AdviceFactor> RiskFactors = new List<AdviceFactor>();
        public readonly List<string> BodyLines = new List<string>();
    }

    [Serializable]
    public sealed class AvailableTroopUnitSummary
    {
        public string Name;
        public TroopUnitType UnitType;
        public ArmyComposition Composition;
        public int Total;
        public int Capacity;

        public AvailableTroopUnitSummary(TroopUnit unit)
        {
            if (unit == null)
            {
                Name = "(null)";
                return;
            }

            Name = unit.Name;
            UnitType = unit.UnitType;
            Composition = unit.GetAggregateComposition();
            Total = Composition.Total;
            Capacity = unit.Capacity;
        }
    }

    [Serializable]
    public sealed class GeneralTroopAdvice
    {
        public string GeneralMessage;
        public string TroopConditionNote;
        public string UncertaintyNote;
        public ArmyComposition AvailableTotals;
        public readonly List<AvailableTroopUnitSummary> AvailableUnits = new List<AvailableTroopUnitSummary>();
        public readonly List<AdviceFactor> ContextFactors = new List<AdviceFactor>();
        public readonly List<string> BodyLines = new List<string>();
    }

    [Serializable]
    public sealed class GeneralRouteAdvice
    {
        public string GeneralMessage;
        public bool IsValid;
        public string FailureReason;
        public Vector3 DestinationWorld;
        public float ApproximateDistance;
        public int RouteCellCount;
        public string TerrainAlongRouteSummary;
        public string WeatherLabel;
        public float WeatherMovementMultiplier = 1f;
        public NavigationRoute Route;
        public readonly List<AdviceFactor> RiskFactors = new List<AdviceFactor>();
        public readonly List<string> BodyLines = new List<string>();
    }

    [Serializable]
    public sealed class GeneralMonitoringAdvice
    {
        public string GeneralMessage;
        public float Health;
        public float Morale;
        public float Sleep;
        public float Food;
        public float Water;
        public TravelStatus TravelStatus;
        public bool RecommendRest;
        public string RestRecommendationReason;
        public bool CanOfferRestAction;
        public bool CanOfferResumeAction;
        public readonly List<string> BodyLines = new List<string>();
    }
}
