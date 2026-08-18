using UnityEngine;

namespace Game.Morale
{
    [CreateAssetMenu(fileName = "MoraleSettings", menuName = "Game/Morale/Morale Settings")]
    public sealed class MoraleSettings : ScriptableObject
    {
        [Header("General")] [SerializeField, Range(0f, 100f)]
        private float initialValue = 100f;

        [Header("Health From Needs")] [SerializeField, Range(0f, 100f)]
        private float healthDamageThreshold = 40f;

        [SerializeField, Min(0f)] private float maxHealthLossPerHourPerNeed = 5f;

        [Header("HUD Colors")] [SerializeField, Range(0f, 100f)]
        private float healthyThreshold = 70f;

        [SerializeField, Range(0f, 100f)] private float warningThreshold = 40f;
        
        [SerializeField, Range(0f, 100f)]
        private float criticalThreshold = 20f;
        
        [SerializeField]
        private Color healthyColor = Color.green;
        
        [SerializeField]
        private Color warningColor = Color.yellow;
        
        [SerializeField]
        private Color lowColor = new  Color(1f, 0.5f, 0f);
        
        [SerializeField]
        private Color criticalColor = Color.red;
        
        public float InitialValue => initialValue;
        public float HealthDamageThreshold => healthDamageThreshold;
        public float MaxHealthLossPerHourPerNeed => maxHealthLossPerHourPerNeed;

        public Color GetMeterColor(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            if (value >= healthyThreshold)
                return healthyColor;
            
            if(value >= warningThreshold)
                return warningColor;

            if (value >= criticalThreshold)
                return lowColor;
            
            return criticalColor;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            healthyThreshold = Mathf.Clamp(healthyThreshold, 0f, 100f);
            warningThreshold = Mathf.Clamp(warningThreshold, 0f, healthyThreshold);
            criticalThreshold = Mathf.Clamp(criticalThreshold, 0f, warningThreshold);  
        }
#endif
    }
}