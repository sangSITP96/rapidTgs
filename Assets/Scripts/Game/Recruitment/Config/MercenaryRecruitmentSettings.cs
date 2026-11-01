using System.Collections.Generic;
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
            new MercenaryOfferDefinition(
                "merc-infantry-squad",
                "Mercenary Infantry Squad",
                TroopUnitType.Squad,
                infantry: 8,
                cavalry: 0,
                archers: 0,
                goldPrice: 40,
                initialAvailable: 10),
            new MercenaryOfferDefinition(
                "merc-mixed-platoon",
                "Mercenary Mixed Platoon",
                TroopUnitType.Platoon,
                infantry: 16,
                cavalry: 8,
                archers: 8,
                goldPrice: 120,
                initialAvailable: 5),
            new MercenaryOfferDefinition(
                "merc-archer-squad",
                "Mercenary Archer Squad",
                TroopUnitType.Squad,
                infantry: 0,
                cavalry: 0,
                archers: 8,
                goldPrice: 55,
                initialAvailable: 8)
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
