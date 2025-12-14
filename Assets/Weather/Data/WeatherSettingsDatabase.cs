using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeatherEntry
{
    public WeatherType Type;

    [Tooltip("If this weather condition is currently applied in the simulation.")]
    public bool isActive = false;

    [Tooltip("Low value for this weather multiplier.")] 
    [Min(0f)]
    public float lowValue = 1f;

    [Tooltip("High value for this weather multiplier.")] 
    [Min(0f)]
    public float highValue = 1f;

    [Tooltip("Design reference: minimum value from the design doc.")]
    public float referenceMin = 1f;
    
    [Tooltip("Design reference: maximum value from the design doc.")]
    public float referenceMax = 1f;

    public float CurrentValue => lowValue;

    public void ClampToReferenceRange()
    {
#if UNITY_EDITOR
        if (referenceMin <= referenceMax)
        {
            lowValue = Mathf.Clamp(lowValue, referenceMin, referenceMax);
            highValue = Mathf.Clamp(highValue, referenceMin, referenceMax);

            if (lowValue > highValue)
            {
                lowValue = highValue;
            }
        }
#endif
    }

}

[CreateAssetMenu(menuName = "Movement/Weather Settings Database")]
public class WeatherSettingsDatabase : ScriptableObject
{
    public List<WeatherEntry> entries = new List<WeatherEntry>();

    public WeatherEntry GetEntry(WeatherType type)
    {
        return entries.Find(e => e.Type == type);
    }

    public float GetTotalMultiplier()
    {
        float total = 1f;

        foreach (var entry in entries)
        {
            if (!entry.isActive) continue;
            total *= entry.CurrentValue;
        }
        
        return total;
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var e in entries)
        {
            e.ClampToReferenceRange();
        }
    }
    #endif
}
