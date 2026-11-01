using System;
using Game.Currency;
using Game.Persistence;
using Game.Troops;
using UnityEngine;

namespace Game.Recruitment
{
    /// <summary>
    /// Atomic hire transaction: validate → spend Gold → consume pool → add to existing roster → save.
    /// </summary>
    public sealed class MercenaryHiringService : MonoBehaviour
    {
        [SerializeField] private MercenaryPool pool;
        [SerializeField] private CurrencySystem currency;
        [SerializeField] private TemporaryAlphaTroopRoster roster;
        [SerializeField] private CommanderProgressPersistence persistence;

        public event Action HiredSuccessfully;
        public event Action<string> HireFailed;

        public MercenaryPool Pool => pool;
        public CurrencySystem Currency => currency;
        public TemporaryAlphaTroopRoster Roster => roster;

        private void Awake()
        {
            ResolveRefs();
        }

        public void ResolveRefs()
        {
            if (pool == null)
                pool = GetComponent<MercenaryPool>() ?? FindFirstObjectByType<MercenaryPool>();
            if (currency == null)
                currency = FindFirstObjectByType<CurrencySystem>();
            if (roster == null)
                roster = FindFirstObjectByType<TemporaryAlphaTroopRoster>();
            if (persistence == null)
                persistence = GetComponent<CommanderProgressPersistence>()
                              ?? FindFirstObjectByType<CommanderProgressPersistence>();
        }

        public bool CanHire(string offerId, int quantity, out string error, out int totalGold)
        {
            ResolveRefs();
            totalGold = 0;
            error = null;

            if (pool == null)
            {
                error = "Mercenary pool missing.";
                return false;
            }

            if (currency == null)
            {
                error = "Currency system missing.";
                return false;
            }

            if (roster == null)
            {
                error = "Commander roster missing.";
                return false;
            }

            if (quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }

            pool.EnsureInitialized();
            MercenaryOfferRuntimeState offer = pool.Find(offerId);
            if (offer == null)
            {
                error = "Mercenary offer not found.";
                return false;
            }

            if (offer.Available <= 0)
            {
                error = "Sold out.";
                return false;
            }

            if (quantity > offer.Available)
            {
                error = $"Only {offer.Available} available.";
                return false;
            }

            totalGold = offer.GoldPrice * quantity;
            if (totalGold < 0)
            {
                error = "Invalid price.";
                return false;
            }

            if (!currency.HasEnoughGold(totalGold))
            {
                error = $"Need {totalGold} Gold (have {currency.Gold}).";
                return false;
            }

            // Roster append is always possible for flat units in TemporaryAlphaTroopRoster.
            return true;
        }

        public bool TryHire(string offerId, int quantity, out string error)
        {
            if (!CanHire(offerId, quantity, out error, out int totalGold))
            {
                HireFailed?.Invoke(error);
                return false;
            }

            MercenaryOfferRuntimeState offer = pool.Find(offerId);
            if (offer == null)
            {
                error = "Mercenary offer not found.";
                HireFailed?.Invoke(error);
                return false;
            }

            // Atomic: all validations passed — spend, then consume, then add.
            if (totalGold > 0 && !currency.TrySpendGold(totalGold, $"Hire:{offer.DisplayName} x{quantity}"))
            {
                error = "Gold spend failed.";
                HireFailed?.Invoke(error);
                return false;
            }

            if (!pool.TryConsume(offerId, quantity, out error))
            {
                // Roll back gold if pool consume somehow fails after spend.
                if (totalGold > 0)
                    currency.AddGold(totalGold, $"HireRollback:{offer.DisplayName}");
                HireFailed?.Invoke(error);
                return false;
            }

            for (int i = 0; i < quantity; i++)
            {
                // Use offer display name as-is (no #index — roster already tracks distinct TroopUnit instances).
                TroopUnit unit = roster.CreateHiredUnit(
                    offer.DisplayName,
                    offer.UnitType,
                    offer.Infantry,
                    offer.Cavalry,
                    offer.Archers);

                if (unit == null)
                {
                    error = "Failed to add unit to commander army.";
                    HireFailed?.Invoke(error);
                    // Do not attempt partial rollback of prior units in prototype — log loudly.
                    Debug.LogError(
                        $"[Recruitment] Hire partially applied after unit {i + 1}/{quantity}. " +
                        "Gold and pool already updated.");
                    return false;
                }
            }

            persistence?.SaveNow();
            error = null;
            HiredSuccessfully?.Invoke();
            Debug.Log(
                $"[Recruitment] Hired {quantity}x {offer.DisplayName} for {totalGold} Gold. " +
                $"Pool left={offer.Available}, Gold={currency.Gold}, ArmyUnits={roster.OwnedUnitCount}");
            return true;
        }
    }
}
