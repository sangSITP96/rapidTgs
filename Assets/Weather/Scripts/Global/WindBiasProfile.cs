using Unity.VisualScripting;
using UnityEngine;

namespace Game.Weather.Global
{
    [CreateAssetMenu(menuName = "Game/Weather/Wind Bias Profile", fileName = "WindBiasProfile")]
    public class WindBiasProfile : ScriptableObject
    {
        [Header("Bias Change Cadence")] 
        [Min(0.1f)] public float biasChangeHours = 3f;

        [Header("Base Direction Weights")] 
        [Range(0f, 1f)] public float WeightWestToEast = 0.60f;
        [Range(0f, 1f)] public float WeightWestToNorthEast = 0.15f;
        [Range(0f, 1f)] public float WeightWestToSouthEast = 0.15f;
        [Range(0f, 1f)] public float WeightSporadic = 0.10f;

        [Header("Sporadic Shift")] 
        [Min(1f)] public float SporadicMinMinutes = 10f;
        [Min(1f)] public float SporadicMaxMinutes = 45f;

        public bool AllowTempEast = true;
        public bool AllowTempNorth = true;
        public bool AllowTempSouth = true;

        [Header("Option Strength")] [Min(0f)] public float WinStrength = 1f;
    }
}