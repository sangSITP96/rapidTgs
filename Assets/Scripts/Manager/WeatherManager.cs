using UnityEngine;

public class WeatherManager : MonoBehaviour
{
   [SerializeField] private WeatherSettingsDatabase _database;

   public WeatherSettingsDatabase Database => _database;

   /// <summary>
   /// Combined active weather multiplier from the settings database.
   /// Convention for travel: value &lt; 1 slows movement.
   /// </summary>
   public float GetTotalWeatherMultiplier()
   {
      if (_database == null)
      {
         Debug.LogWarning("WeatherManager: WeatherSettingsDatabase is not assigned.");
         return 1f;
      }

      return _database.GetTotalMultiplier();
   }

   /// <summary>
   /// Movement multiplier for travel systems. &lt; 1 = slower.
   /// </summary>
   public float GetTravelMovementMultiplier()
   {
      return GetTotalWeatherMultiplier();
   }

   /// <summary>
   /// Needs consumption multiplier for travel systems. &gt; 1 = more consumption.
   /// Phase 12: defaults to 1 (weather affects movement first). Override later if needed.
   /// </summary>
   public float GetTravelNeedsMultiplier()
   {
      return 1f;
   }

   public void SetWeatherActive(WeatherType type, bool active)
   {
      if (_database == null) return;

      var entry = _database.GetEntry(type);
      if (entry != null)
      {
         entry.isActive = active;
      }
   }
}
