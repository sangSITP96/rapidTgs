using UnityEngine;

namespace Game.Battle
{
    [CreateAssetMenu(fileName = "TroopDefinition", menuName = "Game/Battle/Troop Definition")]
    public sealed class TroopDefinition : ScriptableObject
    {
        [SerializeField] private TroopType troopType;
        [SerializeField] private string displayName;

        [Header("Combat Stats")]
        [SerializeField, Min(0)] private int attack = 1;
        [SerializeField, Min(0)] private int defense = 1;
        [SerializeField, Min(0)] private int health = 1;

        public TroopType TroopType => troopType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? troopType.ToString() : displayName;
        public int Attack => attack;
        public int Defense => defense;
        public int Health => health;

#if UNITY_EDITOR
        private void OnValidate()
        {
            attack = Mathf.Max(0, attack);
            defense = Mathf.Max(0, defense);
            health = Mathf.Max(0, health);
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = troopType.ToString();
        }
#endif
    }
}
