using System.Collections.Generic;
using Game.Core.WorldTime;
using Game.Weather.Convergence;
using UnityEngine;

namespace Game.Weather.Storm
{
    public class StormLifecycleManager : MonoBehaviour
    {
       [SerializeField] private WorldTime _worldTime;
       [SerializeField] private ConvergenceManager _convergenceManager;
       [SerializeField] private StormConfig _stormConfig;

       [Header("Limits")] 
       [SerializeField] private int _maxActiveStorms = 6;

       [SerializeField] private StormDebugView _stormDebugPrefab;

       public IReadOnlyList<Storm> ActiveStorm => _storms;
       private readonly List<Storm> _storms = new();

       private const double SecondsPerHour = 3600.0;

       private void OnEnable()
       {
           if (_worldTime == null)
           {
               _worldTime = FindFirstObjectByType<WorldTime>();
           }

           if (_worldTime == null || _convergenceManager == null || _stormConfig == null)
           {
               Debug.LogError("StormLifecycleManager not found");
               enabled = false;
               return;
           }
           
           _worldTime.OnTimeAdvanced += HandleTimeAdvanced;
       }

       private void OnDisable()
       {
           if (_worldTime != null)
           {
               _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
           }
       }

       private void HandleTimeAdvanced(double deltaGameSeconds, double now)
       {
           UpdateLifecycle(now);
           TrySpawnStorm(now);
       }

       private void UpdateLifecycle(double now)
       {
           for (int i = _storms.Count - 1; i >= 0; i--)
           {
               var storm = _storms[i];
               // Check expired
               if (storm.IsExpired(now))
               {
                   Debug.Log($"Destroy Storm with radius = {storm.Radius}  state = {storm.State}  expire = {storm.ExpireGameSeconds} start {storm.SpawnGameSeconds} and now is {now}");
                   
                   Debug.Log($"[Storm {i}] ✅ EXPIRED! Destroying..."); // ← ADD
                   DestroyStorm(storm, i);
                   continue;
               }
        
               if (storm.State == StormState.Exited)
               {
                   Debug.Log($"Destroy Storm with radius = {storm.Radius}  state = {storm.State}  expire = {storm.ExpireGameSeconds} start {storm.SpawnGameSeconds} and now is {now}");
                   Debug.Log($"[Storm {i}] ✅ EXITED! Destroying..."); // ← ADD
                   DestroyStorm(storm, i);
               }
           }
       }
       
       private void DestroyStorm(Storm storm, int index)
       {
           if (storm.view != null)
           {
               Destroy(storm.view.gameObject);
           }
           _storms.RemoveAt(index);
       }
       
       //

       private void TrySpawnStorm(double now)
       {
           if (_storms.Count >= _maxActiveStorms)
               return;

           var points = _convergenceManager.ActivePoints;
           if (points == null || points.Count == 0)
           {
               return;
           }

           if (Random.value > 0.02f)
               return;
           
           var cp = points[Random.Range(0, points.Count)];
           CreateStormAt(cp.Position, now);
       }

       private void CreateStormAt(Vector2 position, double now)
       {
           bool becomesActive = Random.value <= _stormConfig.ActiveChance;

           double durationHours = RollDurationHours();
           double durationSeconds = durationHours * SecondsPerHour;
           
           var storm = new Storm
           {
               Position = position,
               Radius = Random.Range(_stormConfig.MinRadius, _stormConfig.MaxRadius),
               State = becomesActive ? StormState.Active : StormState.Forming,
               IsActive = becomesActive,
               SpawnGameSeconds = now,
               ExpireGameSeconds = now + durationSeconds
           };
           var view = Instantiate(_stormDebugPrefab,
               new Vector3(position.x, 0.5f, position.y),
               Quaternion.identity);
           storm.view = view;
           
           _storms.Add(storm);
           //
           view.Initialize(storm.Radius);
           
           // Set difference color to Forming Storm
           if (storm.view != null)
           {
               storm.view.UpdateState(storm.State);
           }
       }

       private double RollDurationHours()
       {
           float rand = Random.value;
           if (rand < 0.33f)
               return Random.Range(_stormConfig.ShortMin, _stormConfig.ShortMax);
           if(rand < 0.66f)
               return Random.Range(_stormConfig.MediumMin, _stormConfig.MediumMax);
           return Random.Range(_stormConfig.LargeMin,  _stormConfig.LargeMax);
       }
       
       public void RemoveStormAt(int index)
       {
           if (index >= 0 && index < _storms.Count)
           {
               DestroyStorm(_storms[index], index);
           }
       }

       public List<Storm> GetStormList()
       {
           return _storms;
       }
    }
}