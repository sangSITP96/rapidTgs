using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Recruitment.UI
{
    public sealed class MercenaryRecruitmentPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private MercenaryRecruitmentRowView _rowPrefab;
        [SerializeField] private TMP_Text _selectedNameText;
        [SerializeField] private TMP_Text _quantityText;
        [SerializeField] private TMP_Text _totalText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _minusButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _hireButton;
        [SerializeField] private Button _closeButton;

        private readonly List<MercenaryRecruitmentRowView> _rows = new List<MercenaryRecruitmentRowView>();
        private MercenaryOfferRuntimeState _selected;
        private int _quantity = 1;

        public event Action CloseClicked;
        public event Action HireClicked;
        public event Action QuantityChanged;
        public event Action<MercenaryOfferRuntimeState> SelectionChanged;

        public MercenaryOfferRuntimeState SelectedOffer => _selected;
        public int Quantity => _quantity;

        private void Awake()
        {
            if (_minusButton != null)
                _minusButton.onClick.AddListener(OnMinus);
            if (_plusButton != null)
                _plusButton.onClick.AddListener(OnPlus);
            if (_hireButton != null)
                _hireButton.onClick.AddListener(() => HireClicked?.Invoke());
            if (_closeButton != null)
                _closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "MERCENARY RECRUITMENT";
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
                _goldText.text = $"Gold: {gold}";
        }

        public void SetStatus(string message, bool isError = false)
        {
            if (_statusText == null)
                return;

            _statusText.text = message ?? string.Empty;
            _statusText.color = isError
                ? new Color(1f, 0.45f, 0.4f)
                : new Color(0.7f, 0.9f, 0.7f);
        }

        public void BindOffers(IReadOnlyList<MercenaryOfferRuntimeState> offers)
        {
            string keepId = _selected != null ? _selected.OfferId : null;
            ClearRows();

            if (offers == null || _rowPrefab == null || _listRoot == null)
            {
                _selected = null;
                RefreshSelectionUi();
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
                _rows.Add(row);

                if (keepId != null && offer.OfferId == keepId)
                    keepRow = row;
            }

            if (keepRow != null)
                SelectRow(keepRow);
            else if (_rows.Count > 0)
                SelectRow(_rows[0]);
            else
            {
                _selected = null;
                RefreshSelectionUi();
            }
        }

        public void RefreshSelectedOfferState()
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i]?.Refresh();

            if (_selected != null)
            {
                // Re-bind selected pointer from refreshed rows.
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

            ClampQuantity();
            RefreshSelectionUi();
        }

        public void SetHireInteractable(bool interactable)
        {
            if (_hireButton != null)
                _hireButton.interactable = interactable;
        }

        private void OnRowSelected(MercenaryRecruitmentRowView row)
        {
            SelectRow(row);
        }

        private void SelectRow(MercenaryRecruitmentRowView row)
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i]?.SetSelected(_rows[i] == row);

            _selected = row != null ? row.Offer : null;
            _quantity = 1;
            ClampQuantity();
            RefreshSelectionUi();
            SelectionChanged?.Invoke(_selected);
        }

        private void OnMinus()
        {
            _quantity = Mathf.Max(1, _quantity - 1);
            ClampQuantity();
            RefreshSelectionUi();
            QuantityChanged?.Invoke();
        }

        private void OnPlus()
        {
            _quantity++;
            ClampQuantity();
            RefreshSelectionUi();
            QuantityChanged?.Invoke();
        }

        private void ClampQuantity()
        {
            int max = _selected != null ? Mathf.Max(0, _selected.Available) : 0;
            if (max <= 0)
            {
                _quantity = 0;
                return;
            }

            if (_quantity < 1)
                _quantity = 1;
            if (_quantity > max)
                _quantity = max;
        }

        private void RefreshSelectionUi()
        {
            if (_selectedNameText != null)
            {
                _selectedNameText.text = _selected != null
                    ? $"Selected: {_selected.DisplayName}"
                    : "Selected: —";
            }

            if (_quantityText != null)
                _quantityText.text = _quantity.ToString();

            int total = _selected != null ? _selected.GoldPrice * Mathf.Max(0, _quantity) : 0;
            if (_totalText != null)
                _totalText.text = $"Total: {total} Gold";

            bool canAdjust = _selected != null && _selected.Available > 0;
            if (_minusButton != null)
                _minusButton.interactable = canAdjust && _quantity > 1;
            if (_plusButton != null)
                _plusButton.interactable = canAdjust && _quantity < _selected.Available;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] == null)
                    continue;
                _rows[i].Selected -= OnRowSelected;
                Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }
    }
}
