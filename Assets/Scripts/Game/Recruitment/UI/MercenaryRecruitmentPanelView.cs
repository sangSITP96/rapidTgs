using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Recruitment.UI
{
    /// <summary>
    /// Mercenary Terminal ledger UI — table of offers with per-row Hire.
    /// </summary>
    public sealed class MercenaryRecruitmentPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Transform _headerRoot;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private MercenaryRecruitmentRowView _rowPrefab;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ScrollRect _scrollRect;

        private readonly List<MercenaryRecruitmentRowView> _rows = new List<MercenaryRecruitmentRowView>();
        private MercenaryOfferRuntimeState _selected;

        public event Action CloseClicked;
        public event Action<MercenaryOfferRuntimeState> HireClicked;
        public event Action<MercenaryOfferRuntimeState> SelectionChanged;

        public MercenaryOfferRuntimeState SelectedOffer => _selected;

        /// <summary>Always 1 — terminal hires one contract per Hire click (client mock).</summary>
        public int Quantity => 1;

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "MERCENARY TERMINAL";
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        public void SetGold(int gold)
        {
            if (_goldText != null)
            {
                _goldText.text = "Gold: " + gold.ToString("N0", CultureInfo.GetCultureInfo("en-US"));
            }
        }

        public void SetStatus(string message, bool isError = false)
        {
            if (_statusText == null)
                return;

            _statusText.text = message ?? string.Empty;
            _statusText.color = isError
                ? new Color(0.55f, 0.15f, 0.12f, 1f)
                : new Color(0.20f, 0.35f, 0.18f, 1f);
        }

        public void BindOffers(IReadOnlyList<MercenaryOfferRuntimeState> offers)
        {
            string keepId = _selected != null ? _selected.OfferId : null;
            ClearRows();

            if (offers == null || _rowPrefab == null || _listRoot == null)
            {
                _selected = null;
                return;
            }

            MercenaryRecruitmentRowView keepRow = null;
            for (int i = 0; i < offers.Count; i++)
            {
                MercenaryOfferRuntimeState offer = offers[i];
                if (offer == null)
                    continue;

                MercenaryRecruitmentRowView row = Instantiate(_rowPrefab, _listRoot);
                row.gameObject.SetActive(true);
                row.Bind(offer);
                row.Selected += OnRowSelected;
                row.HireClicked += OnRowHireClicked;
                _rows.Add(row);

                if (keepId != null && offer.OfferId == keepId)
                    keepRow = row;
            }

            if (keepRow != null)
                SelectRow(keepRow, notify: false);
            else if (_rows.Count > 0)
                SelectRow(_rows[0], notify: false);
            else
                _selected = null;

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void RefreshSelectedOfferState()
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i]?.Refresh();

            if (_selected == null)
                return;

            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].Offer != null &&
                    _rows[i].Offer.OfferId == _selected.OfferId)
                {
                    _selected = _rows[i].Offer;
                    break;
                }
            }
        }

        public void SetRowHireEnabled(string offerId, bool enabled)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                MercenaryRecruitmentRowView row = _rows[i];
                if (row == null || row.Offer == null)
                    continue;
                if (row.Offer.OfferId == offerId)
                    row.SetHireEnabled(enabled);
            }
        }

        public void RefreshAllHireButtons(Func<MercenaryOfferRuntimeState, bool> canHire)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                MercenaryRecruitmentRowView row = _rows[i];
                if (row == null || row.Offer == null)
                    continue;
                bool ok = canHire != null && canHire(row.Offer);
                row.SetHireEnabled(ok);
            }
        }

        /// <summary>Legacy no-op — Hire is per-row now.</summary>
        public void SetHireInteractable(bool interactable)
        {
            // Kept for controller compatibility; per-row buttons are updated via RefreshAllHireButtons.
        }

        private void OnRowSelected(MercenaryRecruitmentRowView row)
        {
            SelectRow(row, notify: true);
        }

        private void OnRowHireClicked(MercenaryRecruitmentRowView row)
        {
            if (row == null || row.Offer == null)
                return;

            SelectRow(row, notify: true);
            HireClicked?.Invoke(row.Offer);
        }

        private void SelectRow(MercenaryRecruitmentRowView row, bool notify)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null)
                    continue;
                // Avoid re-entrancy via SetSelected → Selected event.
                bool isTarget = _rows[i] == row;
                _rows[i].SetSelected(isTarget);
            }

            _selected = row != null ? row.Offer : null;
            if (notify)
                SelectionChanged?.Invoke(_selected);
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null)
                    continue;
                _rows[i].Selected -= OnRowSelected;
                _rows[i].HireClicked -= OnRowHireClicked;
                Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }
    }
}
