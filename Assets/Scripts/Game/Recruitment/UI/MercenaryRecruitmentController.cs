using Game.Currency;
using UnityEngine;

namespace Game.Recruitment.UI
{
    /// <summary>
    /// Wires Recruitment panel to <see cref="MercenaryHiringService"/> and existing Gold HUD events.
    /// </summary>
    public sealed class MercenaryRecruitmentController : MonoBehaviour
    {
        [SerializeField] private MercenaryHiringService hiringService;
        [SerializeField] private MercenaryRecruitmentPanelView panel;
        [SerializeField] private CurrencySystem currency;

        private bool _wired;

        private void Awake()
        {
            ResolveRefs();
        }

        private void OnEnable()
        {
            Wire(true);
        }

        private void OnDisable()
        {
            Wire(false);
        }

        public void ResolveRefs()
        {
            if (hiringService == null)
                hiringService = FindFirstObjectByType<MercenaryHiringService>();
            if (panel == null)
                panel = GetComponent<MercenaryRecruitmentPanelView>()
                        ?? FindFirstObjectByType<MercenaryRecruitmentPanelView>(FindObjectsInactive.Include);
            if (currency == null)
                currency = hiringService != null ? hiringService.Currency : FindFirstObjectByType<CurrencySystem>();
        }

        public void Show()
        {
            ResolveRefs();
            Wire(true);
            hiringService?.Pool?.EnsureInitialized();
            panel?.Show();
            RefreshAll();
        }

        public void Hide()
        {
            panel?.Hide();
        }

        public void RefreshAll()
        {
            ResolveRefs();
            if (panel == null)
                return;

            if (currency != null)
                panel.SetGold(currency.Gold);

            if (hiringService != null && hiringService.Pool != null)
                panel.BindOffers(hiringService.Pool.Offers);

            UpdateHireState();
        }

        private void Wire(bool bind)
        {
            if (panel == null)
                ResolveRefs();
            if (panel == null)
                return;

            if (!bind)
            {
                if (!_wired)
                    return;
                panel.CloseClicked -= Hide;
                panel.HireClicked -= OnHire;
                panel.QuantityChanged -= UpdateHireState;
                panel.SelectionChanged -= OnSelectionChanged;
                if (currency != null)
                    currency.BalanceChanged -= OnBalanceChanged;
                if (hiringService != null)
                {
                    hiringService.HiredSuccessfully -= OnHired;
                    hiringService.HireFailed -= OnHireFailed;
                    if (hiringService.Pool != null)
                        hiringService.Pool.Changed -= OnPoolChanged;
                }

                _wired = false;
                return;
            }

            if (_wired)
                return;

            panel.CloseClicked += Hide;
            panel.HireClicked += OnHire;
            panel.QuantityChanged += UpdateHireState;
            panel.SelectionChanged += OnSelectionChanged;
            if (currency != null)
                currency.BalanceChanged += OnBalanceChanged;
            if (hiringService != null)
            {
                hiringService.HiredSuccessfully += OnHired;
                hiringService.HireFailed += OnHireFailed;
                if (hiringService.Pool != null)
                    hiringService.Pool.Changed += OnPoolChanged;
            }

            _wired = true;
        }

        private void OnSelectionChanged(MercenaryOfferRuntimeState _)
        {
            UpdateHireState();
        }

        private void OnBalanceChanged(CurrencyType type, int _)
        {
            if (type != CurrencyType.Gold)
                return;
            if (currency != null)
                panel?.SetGold(currency.Gold);
            UpdateHireState();
        }

        private void OnPoolChanged()
        {
            panel?.RefreshSelectedOfferState();
            UpdateHireState();
        }

        private void OnHire()
        {
            if (hiringService == null || panel == null)
                return;

            MercenaryOfferRuntimeState offer = panel.SelectedOffer;
            if (offer == null)
            {
                panel.SetStatus("Select a mercenary.", true);
                return;
            }

            if (!hiringService.TryHire(offer.OfferId, panel.Quantity, out string error))
                panel.SetStatus(error ?? "Hire failed.", true);
        }

        private void OnHired()
        {
            RefreshAll();
            panel?.SetStatus("Hire successful.", false);
        }

        private void OnHireFailed(string error)
        {
            panel?.SetStatus(error ?? "Hire failed.", true);
            UpdateHireState();
        }

        private void UpdateHireState()
        {
            if (panel == null || hiringService == null)
                return;

            MercenaryOfferRuntimeState offer = panel.SelectedOffer;
            if (offer == null)
            {
                panel.SetHireInteractable(false);
                panel.SetStatus(string.Empty, false);
                return;
            }

            bool can = hiringService.CanHire(offer.OfferId, panel.Quantity, out string error, out _);
            panel.SetHireInteractable(can);
            // Clear stale errors when quantity/selection becomes valid again.
            if (can)
                panel.SetStatus(string.Empty, false);
            else if (!string.IsNullOrEmpty(error))
                panel.SetStatus(error, true);
        }
    }
}
