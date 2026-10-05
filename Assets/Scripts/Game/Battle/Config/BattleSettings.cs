using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(fileName = "BattleSettings", menuName = "Game/Battle/Battle Settings")]
    public sealed class BattleSettings : ScriptableObject
    {
        [Header("Counter Advantage Anchor")]
        [SerializeField, Range(0f, 1f)] private float counterAdvantageMin = 0.60f;
        [SerializeField, Range(0f, 1f)] private float counterAdvantageMax = 0.70f;

        [Header("Neutral / Defender")]
        [SerializeField, Range(0f, 0.45f)] private float defenderNeutralAdvantage = 0.05f;

        [Header("Numerical Superiority")]
        [SerializeField, Min(0f)] private float numericalSuperiorityStrength = 0.35f;

        [Header("Threshold")]
        [SerializeField, Min(1)] private int smallBattleThreshold = 10;
        [SerializeField] private MixedThresholdPolicy mixedThresholdPolicy = MixedThresholdPolicy.PreferLarge;

        public float CounterAdvantageMin => counterAdvantageMin;
        public float CounterAdvantageMax => counterAdvantageMax;
        public float DefenderNeutralAdvantage => defenderNeutralAdvantage;
        public float NumericalSuperiorityStrength => numericalSuperiorityStrength;
        public int SmallBattleThreshold => smallBattleThreshold;
        public MixedThresholdPolicy MixedThresholdPolicy => mixedThresholdPolicy;

#if UNITY_EDITOR
        private void OnValidate()
        {
            counterAdvantageMin = Mathf.Clamp01(counterAdvantageMin);
            counterAdvantageMax = Mathf.Clamp01(counterAdvantageMax);
            if (counterAdvantageMax < counterAdvantageMin)
                counterAdvantageMax = counterAdvantageMin;

            defenderNeutralAdvantage = Mathf.Clamp(defenderNeutralAdvantage, 0f, 0.45f);
            numericalSuperiorityStrength = Mathf.Max(0f, numericalSuperiorityStrength);
            smallBattleThreshold = Mathf.Max(1, smallBattleThreshold);
        }
#endif
    }
}
