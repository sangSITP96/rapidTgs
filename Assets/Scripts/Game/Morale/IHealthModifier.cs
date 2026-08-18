using UnityEngine;

namespace Game.Morale
{
    public interface IHealthModifier
    {
        float GetHealthChangePerHour(TroopMoraleState state);
    }
}

