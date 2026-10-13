using Game.Battle;
using UnityEngine;

namespace Game.Campaign
{
    /// <summary>
    /// Simple Alpha F.I.S.T. selector: random Inf/Cav/Archers split for a target total.
    /// Does not read Commander composition mix — only uses total when equal-size Alpha is enabled.
    /// </summary>
    public sealed class RandomEqualFistForceSelector : IFistForceSelector
    {
        public ArmyComposition CreateForce(int totalTroops, int seed)
        {
            totalTroops = Mathf.Max(0, totalTroops);
            if (totalTroops == 0)
                return new ArmyComposition(0, 0, 0);

            var rng = new System.Random(seed);
            int infantry = rng.Next(0, totalTroops + 1);
            int remaining = totalTroops - infantry;
            int cavalry = rng.Next(0, remaining + 1);
            int archers = remaining - cavalry;
            return new ArmyComposition(infantry, cavalry, archers);
        }
    }
}
