using System;
using System.Text;
using UnityEngine;

namespace Game.Core.WorldTime
{
    public class WorldTick : MonoBehaviour
    {
       public enum TickMode
       {
           FixedGameSeconds,
           FixedGameMinutes,
           FixedGameHours,
       }

       [SerializeField] private WorldTime _worldTime;
       
       [Header("Tick Rule")]
       [SerializeField] private TickMode _mode = TickMode.FixedGameMinutes;

       [SerializeField, Min(1)] private int _interval = 10;

       [SerializeField] private bool _tickOnStart = false;

       public event Action<long, double> OnTick;
       
       private double _nextTickAtGameSeconds;
       private long _tickIndex;

       private void OnEnable()
       {
           if (_worldTime == null)
           {
               _worldTime = FindFirstObjectByType<WorldTime>();
           }

           if (_worldTime == null)
           {
               Debug.LogError("WorldTime is null");
               enabled = false;
               return;
           }

           _worldTime.OnTimeAdvanced += HandleTimeAdvanced;

           _tickIndex = 0;

           double now = _worldTime.TotalGameSeconds;
           _nextTickAtGameSeconds = _tickOnStart ? now : now + GetIntervalGameSeconds();
       }

       private void OnDisable()
       {
           if (_worldTime != null)
           {
               _worldTime.OnTimeAdvanced -= HandleTimeAdvanced;
           }
       }

       private void HandleTimeAdvanced(double deltaGameSeconds, double totalGameSeconds)
       {
           while (totalGameSeconds >= _nextTickAtGameSeconds)
           {
               _tickIndex++;
               OnTick?.Invoke(_tickIndex, totalGameSeconds);

               _nextTickAtGameSeconds += GetIntervalGameSeconds();
           }
       }

       private double GetIntervalGameSeconds()
       {
           switch (_mode)
           {
               case TickMode.FixedGameSeconds:
                   return _interval;
               case TickMode.FixedGameMinutes:
                   return _interval * 60.0;
               case TickMode.FixedGameHours:
                   return _interval * 3600.0;
               default:
                   return _interval * 60.0;
           }
       }
    }
}