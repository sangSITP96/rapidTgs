using System.Collections.Generic;

namespace Game.Troops
{
    /// <summary>
    /// Lightweight source of selectable TroopUnits for a Commander.
    /// Temporary Alpha uses a seed implementation — Phase 19 hires append into the same roster.
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

        /// <summary>
        /// Appends a unit to the commander's army (e.g. hired mercenary).
        /// </summary>
        bool TryAddUnit(TroopUnit unit);
    }
}
