using System;
using UnityEngine;

namespace Game.Battle
{
    public enum MatchupRelation
    {
        Neutral = 0,
        Counters = 1,
        Countered = 2
    }

    public static class BattleMath
    {
        public static readonly TroopType[] AllTypes =
        {
            TroopType.Infantry,
            TroopType.Cavalry,
            TroopType.Archers
        };

        public static MatchupRelation GetRelation(TroopType self, TroopType enemy)
        {
            if (self == enemy)
                return MatchupRelation.Neutral;

            if ((self == TroopType.Infantry && enemy == TroopType.Cavalry) ||
                (self == TroopType.Cavalry && enemy == TroopType.Archers) ||
                (self == TroopType.Archers && enemy == TroopType.Infantry))
            {
                return MatchupRelation.Counters;
            }

            return MatchupRelation.Countered;
        }

        public static float AttackerWinWeight(
            TroopType attackerType,
            TroopType defenderType,
            float alpha,
            float defenderNeutralAdvantage)
        {
            switch (GetRelation(attackerType, defenderType))
            {
                case MatchupRelation.Counters:
                    return alpha;
                case MatchupRelation.Countered:
                    return 1f - alpha;
                default:
                    return 0.5f - defenderNeutralAdvantage;
            }
        }

        public static float CasualtyProbability(
            TroopType selfType,
            TroopType enemyType,
            float alpha,
            float defenderNeutralAdvantage,
            bool selfIsAttacker)
        {
            switch (GetRelation(selfType, enemyType))
            {
                case MatchupRelation.Counters:
                    return 1f - alpha;
                case MatchupRelation.Countered:
                    return alpha;
                default:
                    return selfIsAttacker
                        ? 0.5f + defenderNeutralAdvantage
                        : 0.5f - defenderNeutralAdvantage;
            }
        }

        public static float CompositionWeightedAttackerWinProbability(
            ArmyComposition attacker,
            ArmyComposition defender,
            float alpha,
            float defenderNeutralAdvantage)
        {
            float sum = 0f;
            for (int i = 0; i < AllTypes.Length; i++)
            {
                TroopType aType = AllTypes[i];
                float fA = attacker.Fraction(aType);
                if (fA <= 0f)
                    continue;

                for (int j = 0; j < AllTypes.Length; j++)
                {
                    TroopType dType = AllTypes[j];
                    float fB = defender.Fraction(dType);
                    if (fB <= 0f)
                        continue;

                    sum += fA * fB * AttackerWinWeight(aType, dType, alpha, defenderNeutralAdvantage);
                }
            }

            return Mathf.Clamp01(sum);
        }

        public static float CompositionWeightedCasualtyProbability(
            ArmyComposition selfArmy,
            TroopType selfType,
            ArmyComposition enemyArmy,
            float alpha,
            float defenderNeutralAdvantage,
            bool selfIsAttacker)
        {
            if (selfArmy.Get(selfType) <= 0 || enemyArmy.IsEmpty)
                return 0f;

            float sum = 0f;
            for (int j = 0; j < AllTypes.Length; j++)
            {
                TroopType enemyType = AllTypes[j];
                float f = enemyArmy.Fraction(enemyType);
                if (f <= 0f)
                    continue;

                sum += f * CasualtyProbability(
                    selfType,
                    enemyType,
                    alpha,
                    defenderNeutralAdvantage,
                    selfIsAttacker);
            }

            return Mathf.Clamp01(sum);
        }

        public static float ForceRatioFactor(float attackerTotal, float defenderTotal, float strength)
        {
            if (attackerTotal <= 0f && defenderTotal <= 0f)
                return 1f;
            if (defenderTotal <= 0f)
                return Mathf.Exp(strength);
            if (attackerTotal <= 0f)
                return Mathf.Exp(-strength);

            float r = attackerTotal / defenderTotal;
            float t = (r - 1f) / (r + 1f);
            return Mathf.Exp(strength * t);
        }

        public static float ApplyForceRatioToAttackerWinProbability(float rawAttackerWin, float forceFactor)
        {
            rawAttackerWin = Mathf.Clamp01(rawAttackerWin);
            if (rawAttackerWin <= 0f)
                return 0f;
            if (rawAttackerWin >= 1f)
                return 1f;

            float odds = (rawAttackerWin / (1f - rawAttackerWin)) * Mathf.Max(1e-6f, forceFactor);
            return odds / (1f + odds);
        }

        public static float AdjustCasualtyForForceRatio(float rawCasualty, float forceFactor, bool selfIsAttacker)
        {
            rawCasualty = Mathf.Clamp01(rawCasualty);
            float factor = Mathf.Max(1e-6f, forceFactor);
            float adjusted = selfIsAttacker
                ? rawCasualty / factor
                : rawCasualty * factor;
            return Mathf.Clamp01(adjusted);
        }

        public static BattleResolveMode SelectMode(
            int attackerTotal,
            int defenderTotal,
            int threshold,
            MixedThresholdPolicy mixedPolicy)
        {
            bool attackerSmall = attackerTotal < threshold;
            bool defenderSmall = defenderTotal < threshold;

            if (attackerSmall && defenderSmall)
                return BattleResolveMode.SmallWinProbability;

            if (!attackerSmall && !defenderSmall)
                return BattleResolveMode.LargeCasualties;

            return mixedPolicy == MixedThresholdPolicy.PreferSmall
                ? BattleResolveMode.SmallWinProbability
                : BattleResolveMode.LargeCasualties;
        }

        public static int SampleBinomial(int n, float p, IBattleRng rng)
        {
            if (n <= 0 || p <= 0f)
                return 0;
            if (p >= 1f)
                return n;

            if (n >= 10000)
            {
                double mean = n * (double)p;
                double variance = n * (double)p * (1.0 - p);
                double std = Math.Sqrt(Math.Max(0.0, variance));
                double sample = mean + std * SampleStandardNormal(rng);
                int deaths = (int)Math.Round(sample);
                if (deaths < 0) return 0;
                if (deaths > n) return n;
                return deaths;
            }

            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (rng.NextFloat() < p)
                    count++;
            }

            return count;
        }

        private static double SampleStandardNormal(IBattleRng rng)
        {
            double u1 = Math.Max(1e-12, rng.NextFloat());
            double u2 = rng.NextFloat();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
