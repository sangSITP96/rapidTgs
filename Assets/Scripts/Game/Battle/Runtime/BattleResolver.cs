using UnityEngine;

namespace Game.Battle
{
    public static class BattleResolver
    {
        public static BattleResult Resolve(
            BattleInput input,
            BattleContext context,
            BattleSettings settings,
            IBattleRng rng = null)
        {
            if (settings == null)
                throw new System.ArgumentNullException(nameof(settings));

            ArmyComposition attacker = ArmyComposition.ClampNonNegative(
                input.Attacker.Infantry,
                input.Attacker.Cavalry,
                input.Attacker.Archers);
            ArmyComposition defender = ArmyComposition.ClampNonNegative(
                input.Defender.Infantry,
                input.Defender.Cavalry,
                input.Defender.Archers);

            int seed = context.Seed ?? unchecked(System.Environment.TickCount ^ (attacker.Total * 397) ^ defender.Total);
            if (rng == null)
                rng = new SystemRandomBattleRng(seed);

            float alphaMin = settings.CounterAdvantageMin;
            float alphaMax = settings.CounterAdvantageMax;
            if (alphaMax < alphaMin)
                alphaMax = alphaMin;

            float alpha = rng.NextRange(alphaMin, alphaMax);
            float defenderAdvantage = context.ApplyDefenderAdvantage
                ? Mathf.Clamp(settings.DefenderNeutralAdvantage, 0f, 0.45f)
                : 0f;

            BattleResolveMode mode = BattleMath.SelectMode(
                attacker.Total,
                defender.Total,
                settings.SmallBattleThreshold,
                settings.MixedThresholdPolicy);

            if (mode == BattleResolveMode.SmallWinProbability)
                return ResolveSmall(seed, alpha, attacker, defender, defenderAdvantage, settings.NumericalSuperiorityStrength);

            return ResolveLarge(seed, alpha, attacker, defender, defenderAdvantage, settings.NumericalSuperiorityStrength, rng);
        }

        private static BattleResult ResolveSmall(
            int seed,
            float alpha,
            ArmyComposition attacker,
            ArmyComposition defender,
            float defenderAdvantage,
            float numericalStrength)
        {
            float attackerWin;
            float defenderWin;

            if (attacker.IsEmpty && defender.IsEmpty)
            {
                attackerWin = 0.5f;
                defenderWin = 0.5f;
            }
            else if (attacker.IsEmpty)
            {
                attackerWin = 0f;
                defenderWin = 1f;
            }
            else if (defender.IsEmpty)
            {
                attackerWin = 1f;
                defenderWin = 0f;
            }
            else
            {
                float raw = BattleMath.CompositionWeightedAttackerWinProbability(
                    attacker,
                    defender,
                    alpha,
                    defenderAdvantage);

                float forceFactor = BattleMath.ForceRatioFactor(
                    attacker.Total,
                    defender.Total,
                    numericalStrength);

                attackerWin = BattleMath.ApplyForceRatioToAttackerWinProbability(raw, forceFactor);
                defenderWin = 1f - attackerWin;
            }

            return BattleResult.CreateSmall(
                seed,
                alpha,
                attacker,
                defender,
                attackerWin,
                defenderWin);
        }

        private static BattleResult ResolveLarge(
            int seed,
            float alpha,
            ArmyComposition attacker,
            ArmyComposition defender,
            float defenderAdvantage,
            float numericalStrength,
            IBattleRng rng)
        {
            float forceFactor = BattleMath.ForceRatioFactor(
                attacker.Total,
                defender.Total,
                numericalStrength);

            var attackerP = new float[3];
            var defenderP = new float[3];

            for (int i = 0; i < BattleMath.AllTypes.Length; i++)
            {
                TroopType type = BattleMath.AllTypes[i];
                float rawA = BattleMath.CompositionWeightedCasualtyProbability(
                    attacker, type, defender, alpha, defenderAdvantage, selfIsAttacker: true);
                float rawB = BattleMath.CompositionWeightedCasualtyProbability(
                    defender, type, attacker, alpha, defenderAdvantage, selfIsAttacker: false);

                attackerP[i] = BattleMath.AdjustCasualtyForForceRatio(rawA, forceFactor, selfIsAttacker: true);
                defenderP[i] = BattleMath.AdjustCasualtyForForceRatio(rawB, forceFactor, selfIsAttacker: false);
            }

            int aInfDeaths = BattleMath.SampleBinomial(attacker.Infantry, attackerP[0], rng);
            int aCavDeaths = BattleMath.SampleBinomial(attacker.Cavalry, attackerP[1], rng);
            int aArchDeaths = BattleMath.SampleBinomial(attacker.Archers, attackerP[2], rng);

            int dInfDeaths = BattleMath.SampleBinomial(defender.Infantry, defenderP[0], rng);
            int dCavDeaths = BattleMath.SampleBinomial(defender.Cavalry, defenderP[1], rng);
            int dArchDeaths = BattleMath.SampleBinomial(defender.Archers, defenderP[2], rng);

            var attackerDeaths = new ArmyComposition(aInfDeaths, aCavDeaths, aArchDeaths);
            var defenderDeaths = new ArmyComposition(dInfDeaths, dCavDeaths, dArchDeaths);

            var attackerRemaining = new ArmyComposition(
                attacker.Infantry - aInfDeaths,
                attacker.Cavalry - aCavDeaths,
                attacker.Archers - aArchDeaths);
            var defenderRemaining = new ArmyComposition(
                defender.Infantry - dInfDeaths,
                defender.Cavalry - dCavDeaths,
                defender.Archers - dArchDeaths);

            return BattleResult.CreateLarge(
                seed,
                alpha,
                attacker,
                defender,
                attackerDeaths,
                defenderDeaths,
                attackerRemaining,
                defenderRemaining,
                attackerP,
                defenderP);
        }
    }
}
