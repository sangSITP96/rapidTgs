using System;
using UnityEngine;
using Game.Core.WorldTime;

namespace Game.Travel
{
    [Serializable]
    public sealed class TravelRuntimeState
    {
        public TravelStatus Status = TravelStatus.Idle;
        public Vector3 Origin;
        public Vector3 Destination;
        public Vector3 CurrentPosition;
        public float RemainingDistance;
        public float CurrentSpeed;
        public float EffectiveMovementModifier = 1f;
        public float CurrentSurfaceNeedsModifier = 1f;
        public float WeatherMovementMultiplier = 1f;
        public float WeatherNeedsMultiplier = 1f;
        public TravelSurfaceType CurrentSurface = TravelSurfaceType.Grassland;
        public bool IsPassable = true;
        public bool IsOnTrail;
        public DayPhase CurrentDayPhase = DayPhase.Day;
        public bool IsNight;
        public double MarchElapsedGameSeconds;
        public double RestElapsedGameSeconds;
        public double CampElapsedGameSeconds;
        public bool HasDestination;
        public string StatusLabel => Status.ToString();
    }
}
