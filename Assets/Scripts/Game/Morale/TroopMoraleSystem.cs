using System;
using System.Collections.Generic;
using UnityEngine;

namespace  Game.Morale
{
    public sealed class TroopMoraleSystem : MonoBehaviour
    {
        [Header("Configuration")] 
        [SerializeField]
        private MoraleSettings settings;
        
        [Header("Runtime State")]
        [SerializeField]
        private TroopMoraleState state = new TroopMoraleState();
        
        private readonly List<IHealthModifier> _healthModifiers = new();
        
        public TroopMoraleState State  => state;
        
        public float Health => state.Health;
        public float Sleep => state.Sleep;
        public float Water => state.Water;
        public float Food => state.Food;
        public float Morale => state.Morale;
        
        public event Action StateChanged;

        private void Awake()
        {
            RegisterDefaultModifiers();
        }

        private void RegisterDefaultModifiers()
        {
            _healthModifiers.Clear();
            
            if(settings != null)
                _healthModifiers.Add(new NeedsHealthModifier(settings));
        }

        public void SetHealth(float value)
        {
            SetStat(MoraleStatType.Health, value);
        }

        public void SetSleep(float value)
        {
            SetStat(MoraleStatType.Sleep, value);
        }

        public void SetWater(float value)
        {
            SetStat(MoraleStatType.Water, value);
        }

        public void SetFood(float value)
        {
            SetStat(MoraleStatType.Food, value);   
        }

        public void ModifyHealth(float amount)
        {
            ModifyStat(MoraleStatType.Health, amount);
        }

        public void ModifySleep(float amount)
        {
            ModifyStat(MoraleStatType.Sleep, amount);
        }

        public void ModifyWater(float amount)
        {
            ModifyStat(MoraleStatType.Water, amount);
        }

        public void ModifyFood(float amount)
        {
            ModifyStat(MoraleStatType.Food, amount);
        }

        public void ConsumeSleep(float amount)
        {
            ModifySleep(-Mathf.Abs(amount));
        }

        public void ConsumeWater(float amount)
        {
            ModifyWater(-Mathf.Abs(amount));
        }

        public void ConsumeFood(float amount)
        {
            ModifyFood(-Mathf.Abs(amount));
        }

        public void RestoreSleep(float amount)
        {
            ModifySleep(Mathf.Abs(amount));
        }
        
        public void RestoreWater(float amount)
        {
            ModifyWater(Mathf.Abs(amount));
        }
        
        public void RestoreFood(float amount)
        {
            ModifyFood(Mathf.Abs(amount));
        }

        public void SimulateHours(float hours)
        {
            if(hours <= 0f || state.Health <= 0)
                return;

            float healthChangePerHour = 0f;

            for (int i = 0; i < _healthModifiers.Count; i++)
            {
                IHealthModifier modifier = _healthModifiers[i];

                if (modifier != null)
                {
                    healthChangePerHour += modifier.GetHealthChangePerHour(state);
                }
            }
            
            if(Mathf.Approximately(healthChangePerHour, 0f))
                return;

            bool changed = state.ModifyValue(
                MoraleStatType.Health,
                healthChangePerHour * hours);

            if (changed)
                NotifyStateChanged();
        }

        public void AddHealthModifier(IHealthModifier modifier)
        {
            if (modifier == null || _healthModifiers.Contains(modifier))
            {
                return;
            }
            
            _healthModifiers.Add(modifier);
        }

        public void RemoveHealthModifier(IHealthModifier modifier)
        {
            if(modifier == null)
                return;

            _healthModifiers.Remove(modifier);
        }

        public void ResetState()
        {
            float initialValue = settings != null ? settings.InitialValue : 100f;
            
            state.Reset(initialValue);
            NotifyStateChanged();
        }

        private void SetStat(
            MoraleStatType type,
            float value)
        {
            if (state.SetValue(type, value))
                NotifyStateChanged();
        }

        private void ModifyStat(
            MoraleStatType type,
            float amount)
        {
            if(Mathf.Approximately(amount, 0f))
                return;

            if (state.ModifyValue(type, amount))
                NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}

