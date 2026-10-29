using System;
using System.Collections.Generic;
using Game.General.UI;
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
        [SerializeField] private TMP_Text _generalAdviceText;
        [SerializeField] private TMP_Text _selectedTotalText;
        [SerializeField] private Transform _listRoot;
        [SerializeField] private CampaignTroopSelectionRowView _rowPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _marchButton;

        private readonly List<CampaignTroopSelectionRowView> _rows = new List<CampaignTroopSelectionRowView>();

        public event Action BackClicked;

        /// <summary>
        /// Fired when Commander confirms selection and wants to review the march route.
        /// Does not start March by itself — CampaignFlowController handles route advice first.
        /// </summary>
        public event Action MarchClicked;

        private bool _fontsScaled;

        private void Awake()
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            if (_backButton != null)
                _backButton.onClick.AddListener(() => BackClicked?.Invoke());
            if (_marchButton != null)
                _marchButton.onClick.AddListener(() => MarchClicked?.Invoke());
            // Prefab starts inactive; CampaignFlowController owns visibility.
        }

        public void Show(IReadOnlyList<TroopUnit> availableUnits, string generalAdvice = null)
        {
            GeneralPanelTextScale.ApplyOnce(this, ref _fontsScaled);
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "MERCENARY TERMINAL";

            if (_generalAdviceText != null)
            {
                _generalAdviceText.text = string.Empty;
                _generalAdviceText.gameObject.SetActive(false);
            }

            if (_marchButton != null)
            {
                var label = _marchButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = "Review Route";
            }

            ClearRows();
            if (availableUnits != null && _rowPrefab != null && _listRoot != null)
            {
                for (int i = 0; i < availableUnits.Count; i++)
                {
                    CampaignTroopSelectionRowView row = Instantiate(_rowPrefab, _listRoot);
                    row.gameObject.SetActive(true);
                    ScaleRowFonts(row);
                    row.Bind(availableUnits[i]);
                    row.SelectionChanged += RefreshSelectedTotal;
                    _rows.Add(row);
                }
            }

            RefreshSelectedTotal();
        }

        private static void ScaleRowFonts(CampaignTroopSelectionRowView row)
        {
            if (row == null)
                return;

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                    texts[i].fontSize += GeneralPanelTextScale.ExtraSize;
            }
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
