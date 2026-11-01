using System;
using System.Collections.Generic;
using Game.Persistence;
using UnityEngine;

namespace Game.Recruitment
{
    /// <summary>
    /// Shared local source of truth for mercenary availability (Phase 19 prototype).
    /// Not UI-bound; can later be fed by a backend without rewriting hiring UI.
    /// </summary>
    public sealed class MercenaryPool : MonoBehaviour
    {
        [SerializeField] private MercenaryRecruitmentSettings settings;

        private readonly List<MercenaryOfferRuntimeState> _offers = new List<MercenaryOfferRuntimeState>();
        private bool _initialized;

        public IReadOnlyList<MercenaryOfferRuntimeState> Offers => _offers;
        public MercenaryRecruitmentSettings Settings => settings;

        public event Action Changed;

        public void SetSettings(MercenaryRecruitmentSettings recruitmentSettings)
        {
            settings = recruitmentSettings;
        }

        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            RebuildFromSettings();
        }

        public void RebuildFromSettings()
        {
            _offers.Clear();
            if (settings != null && settings.Offers != null)
            {
                for (int i = 0; i < settings.Offers.Count; i++)
                {
                    MercenaryOfferDefinition def = settings.Offers[i];
                    if (def == null)
                        continue;
                    _offers.Add(new MercenaryOfferRuntimeState(def, def.InitialAvailable));
                }
            }

            _initialized = true;
            Changed?.Invoke();
        }

        public MercenaryOfferRuntimeState Find(string offerId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(offerId))
                return null;

            for (int i = 0; i < _offers.Count; i++)
            {
                if (_offers[i] != null && _offers[i].OfferId == offerId)
                    return _offers[i];
            }

            return null;
        }

        public bool TryGetAvailable(string offerId, out int available)
        {
            MercenaryOfferRuntimeState state = Find(offerId);
            if (state == null)
            {
                available = 0;
                return false;
            }

            available = state.Available;
            return true;
        }

        public bool TryConsume(string offerId, int quantity, out string error)
        {
            EnsureInitialized();
            if (quantity <= 0)
            {
                error = "Quantity must be greater than zero.";
                return false;
            }

            MercenaryOfferRuntimeState state = Find(offerId);
            if (state == null)
            {
                error = "Mercenary offer not found.";
                return false;
            }

            if (quantity > state.Available)
            {
                error = $"Only {state.Available} available.";
                return false;
            }

            if (!state.TryDecrease(quantity))
            {
                error = "Failed to update availability.";
                return false;
            }

            error = null;
            Changed?.Invoke();
            return true;
        }

        public void CaptureAvailability(List<MercenaryAvailabilitySaveEntry> destination)
        {
            EnsureInitialized();
            if (destination == null)
                return;

            destination.Clear();
            for (int i = 0; i < _offers.Count; i++)
            {
                MercenaryOfferRuntimeState state = _offers[i];
                if (state == null)
                    continue;

                destination.Add(new MercenaryAvailabilitySaveEntry
                {
                    offerId = state.OfferId,
                    available = state.Available
                });
            }
        }

        public void ApplyAvailability(IReadOnlyList<MercenaryAvailabilitySaveEntry> entries)
        {
            EnsureInitialized();
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                MercenaryAvailabilitySaveEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.offerId))
                    continue;

                MercenaryOfferRuntimeState state = Find(entry.offerId);
                if (state != null)
                    state.SetAvailable(entry.available);
            }

            Changed?.Invoke();
        }
    }
}
