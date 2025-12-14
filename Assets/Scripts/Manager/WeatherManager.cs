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
