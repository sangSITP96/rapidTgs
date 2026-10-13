using System;
using System.Collections.Generic;
using Game.Troops;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignTroopSelectionPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _selectedTotalText;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private CampaignTroopSelectionRowView _rowPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _marchButton;

        private readonly List<CampaignTroopSelectionRowView> _rows = new List<CampaignTroopSelectionRowView>();

        public event Action BackClicked;
        public event Action MarchClicked;

        private void Awake()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(() => BackClicked?.Invoke());
            if (_marchButton != null)
                _marchButton.onClick.AddListener(() => MarchClicked?.Invoke());
            // Prefab starts inactive; CampaignFlowController owns visibility.
        }

        public void Show(IReadOnlyList<TroopUnit> availableUnits)
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "SELECT TROOPS";

            ClearRows();
            if (availableUnits != null && _rowPrefab != null && _listRoot != null)
            {
                for (int i = 0; i < availableUnits.Count; i++)
                {
                    CampaignTroopSelectionRowView row = Instantiate(_rowPrefab, _listRoot);
                    row.gameObject.SetActive(true);
                    row.Bind(availableUnits[i]);
                    row.SelectionChanged += RefreshSelectedTotal;
                    _rows.Add(row);
                }
            }

            RefreshSelectedTotal();
        }

        public void Hide()
        {
            ClearRows();
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        public List<TroopUnit> GetSelectedUnits()
        {
            var selected = new List<TroopUnit>();
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].IsSelected && _rows[i].Unit != null)
                    selected.Add(_rows[i].Unit);
            }

            return selected;
        }

        private void RefreshSelectedTotal()
        {
            int total = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null && _rows[i].IsSelected && _rows[i].Unit != null)
                    total += _rows[i].Unit.GetAggregateTotal();
            }

            if (_selectedTotalText != null)
                _selectedTotalText.text = "Selected: " + total;

            if (_marchButton != null)
                _marchButton.interactable = total > 0;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }

            _rows.Clear();
        }

    }
}
