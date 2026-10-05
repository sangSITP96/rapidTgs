using System;
using UnityEngine;

namespace Game.Battle
{
    [Serializable]
    public sealed class BattleResult
    {
        [SerializeField] private BattleResolveMode mode;
        [SerializeField] private int seedUsed;
        [SerializeField] private float sampledCounterAdvantage;

        [SerializeField] private ArmyComposition attackerStarting;
        [SerializeField] private ArmyComposition defenderStarting;

        [SerializeField] private float attackerWinProbability;
        [SerializeField] private float defenderWinProbability;

        [SerializeField] private ArmyComposition attackerDeaths;
        [SerializeField] private ArmyComposition defenderDeaths;
        [SerializeField] private ArmyComposition attackerRemaining;
        [SerializeField] private ArmyComposition defenderRemaining;

        [SerializeField] private float attackerInfantryCasualtyProbability;
        [SerializeField] private float attackerCavalryCasualtyProbability;
        [SerializeField] private float attackerArchersCasualtyProbability;
        [SerializeField] private float defenderInfantryCasualtyProbability;
        [SerializeField] private float defenderCavalryCasualtyProbability;
        [SerializeField] private float defenderArchersCasualtyProbability;

        public BattleResolveMode Mode => mode;
        public int SeedUsed => seedUsed;
        public float SampledCounterAdvantage => sampledCounterAdvantage;

        public ArmyComposition AttackerStarting => attackerStarting;
        public ArmyComposition DefenderStarting => defenderStarting;

        public float AttackerWinProbability => attackerWinProbability;
        public float DefenderWinProbability => defenderWinProbability;

        public ArmyComposition AttackerDeaths => attackerDeaths;
        public ArmyComposition DefenderDeaths => defenderDeaths;
        public ArmyComposition AttackerRemaining => attackerRemaining;
        public ArmyComposition DefenderRemaining => defenderRemaining;

        public float GetAttackerCasualtyProbability(TroopType type)
        {
            switch (type)
            {
                case TroopType.Infantry: return attackerInfantryCasualtyProbability;
                case TroopType.Cavalry: return attackerCavalryCasualtyProbability;
                case TroopType.Archers: return attackerArchersCasualtyProbability;
                default: return 0f;
            }
        }

        public float GetDefenderCasualtyProbability(TroopType type)
        {
            switch (type)
            {
                case TroopType.Infantry: return defenderInfantryCasualtyProbability;
                case TroopType.Cavalry: return defenderCavalryCasualtyProbability;
                case TroopType.Archers: return defenderArchersCasualtyProbability;
                default: return 0f;
            }
        }

        public static BattleResult CreateSmall(
            int seedUsed,
            float alpha,
            ArmyComposition attackerStarting,
            ArmyComposition defenderStarting,
            float attackerWinProbability,
            float defenderWinProbability)
        {
            return new BattleResult
            {
                mode = BattleResolveMode.SmallWinProbability,
                seedUsed = seedUsed,
                sampledCounterAdvantage = alpha,
                attackerStarting = attackerStarting,
                defenderStarting = defenderStarting,
                attackerWinProbability = attackerWinProbability,
                defenderWinProbability = defenderWinProbability
            };
        }

        public static BattleResult CreateLarge(
            int seedUsed,
            float alpha,
            ArmyComposition attackerStarting,
            ArmyComposition defenderStarting,
            ArmyComposition attackerDeaths,
            ArmyComposition defenderDeaths,
            ArmyComposition attackerRemaining,
            ArmyComposition defenderRemaining,
            float[] attackerCasualtyProbabilities,
            float[] defenderCasualtyProbabilities)
        {
            return new BattleResult
            {
                mode = BattleResolveMode.LargeCasualties,
                seedUsed = seedUsed,
                sampledCounterAdvantage = alpha,
                attackerStarting = attackerStarting,
                defenderStarting = defenderStarting,
                attackerDeaths = attackerDeaths,
                defenderDeaths = defenderDeaths,
                attackerRemaining = attackerRemaining,
                defenderRemaining = defenderRemaining,
                attackerInfantryCasualtyProbability = attackerCasualtyProbabilities[0],
                attackerCavalryCasualtyProbability = attackerCasualtyProbabilities[1],
                attackerArchersCasualtyProbability = attackerCasualtyProbabilities[2],
                defenderInfantryCasualtyProbability = defenderCasualtyProbabilities[0],
                defenderCavalryCasualtyProbability = defenderCasualtyProbabilities[1],
                defenderArchersCasualtyProbability = defenderCasualtyProbabilities[2]
            };
        }
    }
}
