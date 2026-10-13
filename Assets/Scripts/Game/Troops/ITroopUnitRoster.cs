using System.Collections.Generic;

namespace Game.Troops
{
    /// <summary>
    /// Lightweight source of selectable TroopUnits for a Commander.
    /// Temporary Alpha uses a seed implementation — replace later with real roster/inventory.
    /// </summary>
    public interface ITroopUnitRoster
    {
        string CommanderId { get; }

        IReadOnlyList<TroopUnit> GetAvailableUnits();

        /// <summary>
        /// Marks a unit as no longer available for selection (e.g. sent to Campaign).
        /// </summary>
        bool TryMarkDeployed(TroopUnit unit);

        void ResetDeployedForTesting();
    }
}
