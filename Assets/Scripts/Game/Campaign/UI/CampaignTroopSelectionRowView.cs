using System;
using Game.Battle;
using Game.Troops;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignTroopSelectionRowView : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _capacityText;
        [SerializeField] private TMP_Text _compositionText;

        private TroopUnit _unit;

        public TroopUnit Unit => _unit;
        public bool IsSelected => _toggle != null && _toggle.isOn;

        public event Action SelectionChanged;

        private void Awake()
        {
            if (_toggle != null)
                _toggle.onValueChanged.AddListener(_ => SelectionChanged?.Invoke());
        }

        public void Bind(TroopUnit unit)
        {
            _unit = unit;
            if (unit == null)
                return;

            ArmyComposition agg = unit.GetAggregateComposition();
            if (_nameText != null)
                _nameText.text = unit.Name;
            if (_capacityText != null)
                _capacityText.text = agg.Total.ToString();
            if (_compositionText != null)
                _compositionText.text = $"I:{agg.Infantry} C:{agg.Cavalry} A:{agg.Archers}";
            if (_toggle != null)
                _toggle.SetIsOnWithoutNotify(false);
        }

        public void SetSelected(bool selected)
        {
            if (_toggle != null)
                _toggle.SetIsOnWithoutNotify(selected);
        }
    }
}
