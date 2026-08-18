using System;
using UnityEngine;

namespace Game.Morale
{
    [Serializable]
    public sealed class TroopMoraleState
    {
        [SerializeField, Range(0f, 100f)] 
        private float health = 100f;
        
        [SerializeField, Range(0f, 100f)] 
        private float sleep = 100f;

        [SerializeField, Range(0f, 100f)] 
        private float water = 100f;
        
        [SerializeField, Range(0f, 100f)]
        private float food = 100f;
        
        public float Health => health;
        public float Sleep => sleep;
        public float Water => water;
        public float Food => food;

        public float Morale => (health + sleep + water + food) * 0.25f;

        public float GetValue(MoraleStatType type)
        {
            switch (type)
            {
                case MoraleStatType.Health:
                    return health;
                case MoraleStatType.Sleep:
                    return sleep;
                case MoraleStatType.Water:
                    return water;
                case MoraleStatType.Food:
                    return food;
                
                default: return 0f;
            }
        }

        internal bool SetValue(MoraleStatType type, float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            switch (type)
            {
                case MoraleStatType.Health:
                    return SetIfChanged(ref health, value);
                
                case MoraleStatType.Sleep:
                    return SetIfChanged(ref sleep, value);
                
                case MoraleStatType.Water:
                    return SetIfChanged(ref water, value);
                
                case MoraleStatType.Food:
                    return SetIfChanged(ref food, value);
                
                default: return false;
            }
        }

        internal bool ModifyValue(MoraleStatType type, float amount)
        {
            return SetValue(type, GetValue(type) + amount);
        }

        internal void Reset(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            
            health = value;
            sleep = value;
            water = value;
            food = value;
        }

        private static bool SetIfChanged(ref float current, float value)
        {
            if(Mathf.Approximately(current, value))
                return false;
            
            current = value;
            return true;
        }
    }
}