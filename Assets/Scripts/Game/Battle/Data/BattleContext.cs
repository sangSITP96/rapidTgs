using System;
using UnityEngine;

namespace Game.Battle
{
    [Serializable]
    public struct BattleContext
    {
        [SerializeField] private bool applyDefenderAdvantage;
        [SerializeField] private bool hasSeed;
        [SerializeField] private int seed;

        public bool ApplyDefenderAdvantage => applyDefenderAdvantage;

        public int? Seed => hasSeed ? seed : (int?)null;

        public BattleContext(bool applyDefenderAdvantage, int? seed = null)
        {
            this.applyDefenderAdvantage = applyDefenderAdvantage;
            hasSeed = seed.HasValue;
            this.seed = seed.GetValueOrDefault();
        }

        public static BattleContext Default => new BattleContext(applyDefenderAdvantage: true, seed: null);
    }
}
