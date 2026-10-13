using Game.Battle;

namespace Game.Campaign
{
    /// <summary>
    /// Builds F.I.S.T. force independently of Commander composition details.
    /// Equal-total Alpha behavior is optional via CreateEqualTotal.
    /// </summary>
    public interface IFistForceSelector
    {
        ArmyComposition CreateForce(int totalTroops, int seed);
    }
}
