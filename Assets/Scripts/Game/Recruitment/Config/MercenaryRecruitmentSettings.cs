using System.Collections.Generic;
using Game.Seasons;
using Game.Troops;
using UnityEngine;

namespace Game.Recruitment
{
    [CreateAssetMenu(
        fileName = "MercenaryRecruitmentSettings",
        menuName = "Game/Recruitment/Mercenary Recruitment Settings")]
    public sealed class MercenaryRecruitmentSettings : ScriptableObject
    {
        [SerializeField] private List<MercenaryOfferDefinition> offers = new List<MercenaryOfferDefinition>
        {
            new MercenaryOfferDefinition("merc-company-autumn-atk", TroopUnitType.Company, 128, 0, 0, 820, 4, SeasonId.Autumn, MercenaryCombatAffinity.Attack),
            new MercenaryOfferDefinition("merc-platoon-winter-def", TroopUnitType.Platoon, 32, 0, 0, 210, 6, SeasonId.Winter, MercenaryCombatAffinity.Defense),
            new MercenaryOfferDefinition("merc-squad-spring-hp", TroopUnitType.Squad, 8, 0, 0, 55, 10, SeasonId.Spring, MercenaryCombatAffinity.Health),
            new MercenaryOfferDefinition("merc-battalion-summer-atk", TroopUnitType.Battalion, 512, 0, 0, 2900, 2, SeasonId.Summer, MercenaryCombatAffinity.Attack),
            new MercenaryOfferDefinition("merc-company-winter-def", TroopUnitType.Company, 64, 64, 0, 980, 3, SeasonId.Winter, MercenaryCombatAffinity.Defense),
            new MercenaryOfferDefinition("merc-platoon-autumn-atk", TroopUnitType.Platoon, 0, 32, 0, 240, 5, SeasonId.Autumn, MercenaryCombatAffinity.Attack),
            new MercenaryOfferDefinition("merc-squad-summer-atk", TroopUnitType.Squad, 0, 0, 8, 60, 8, SeasonId.Summer, MercenaryCombatAffinity.Attack),
            new MercenaryOfferDefinition("merc-platoon-spring-hp", TroopUnitType.Platoon, 16, 0, 16, 260, 4, SeasonId.Spring, MercenaryCombatAffinity.Health),
        };

        public IReadOnlyList<MercenaryOfferDefinition> Offers => offers;

        public MercenaryOfferDefinition FindOffer(string offerId)
        {
            if (offers == null || string.IsNullOrWhiteSpace(offerId))
                return null;

            for (int i = 0; i < offers.Count; i++)
            {
                MercenaryOfferDefinition offer = offers[i];
                if (offer != null && offer.OfferId == offerId)
                    return offer;
            }

            return null;
        }
    }
}
