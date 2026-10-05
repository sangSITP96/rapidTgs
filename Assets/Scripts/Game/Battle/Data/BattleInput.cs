using System;
using UnityEngine;

namespace Game.Battle
{
    [Serializable]
    public struct BattleInput
    {
        [SerializeField] private ArmyComposition attacker;
        [SerializeField] private ArmyComposition defender;

        public ArmyComposition Attacker => attacker;
        public ArmyComposition Defender => defender;

        public BattleInput(ArmyComposition attacker, ArmyComposition defender)
        {
            this.attacker = attacker;
            this.defender = defender;
        }
    }
}
