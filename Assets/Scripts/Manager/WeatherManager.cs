using UnityEngine;

public class WeatherManager : MonoBehaviour
{
   [SerializeField] private WeatherSettingsDatabase _database;

   public WeatherSettingsDatabase Database => _database;

   public float GetTotalWeatherMultiplier()
   {
      if (_database == null)
      {
         Debug.LogWarning("WeatherManager: WeatherSettingsDatabase is not assigned.");
         return 1f;
      }

      return _database.GetTotalMultiplier();
   }

   public float GetTravelMovementMultiplier()
   {
      return GetTotalWeatherMultiplier();
   }

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

   public void ClearAllWeather()
   {
      if (_database == null || _database.entries == null)
         return;

      for (int i = 0; i < _database.entries.Count; i++)
      {
         if (_database.entries[i] != null)
            _database.entries[i].isActive = false;
      }
   }

   public void SetExclusiveWeather(params WeatherType[] types)
   {
      ClearAllWeather();
      if (types == null)
         return;

      for (int i = 0; i < types.Length; i++)
         SetWeatherActive(types[i], true);
   }

   public bool IsWeatherActive(WeatherType type)
   {
      if (_database == null)
         return false;

      var entry = _database.GetEntry(type);
      return entry != null && entry.isActive;
   }
}
