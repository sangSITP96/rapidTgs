using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Recruitment.UI
{
    public sealed class MercenaryRecruitmentRowView : MonoBehaviour
    {
        [SerializeField] private Button _selectButton;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _availabilityText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private TMP_Text _compositionText;

        private MercenaryOfferRuntimeState _offer;
        private Color _normalColor = new Color(0.12f, 0.14f, 0.18f, 0.9f);
        private Color _selectedColor = new Color(0.18f, 0.32f, 0.28f, 0.95f);

        public MercenaryOfferRuntimeState Offer => _offer;

        public event Action<MercenaryRecruitmentRowView> Selected;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(() => Selected?.Invoke(this));
        }

        public void Bind(MercenaryOfferRuntimeState offer)
        {
            _offer = offer;
            Refresh();
        }

        public void Refresh()
        {
            if (_offer == null)
                return;

            if (_nameText != null)
                _nameText.text = _offer.DisplayName;

            if (_availabilityText != null)
            {
                _availabilityText.text = _offer.IsSoldOut
                    ? "Availability: SOLD OUT"
                    : $"Availability: {_offer.Available}";
            }

            if (_priceText != null)
                _priceText.text = $"Price: {_offer.GoldPrice} Gold";

            if (_compositionText != null)
            {
                _compositionText.text =
                    $"{_offer.UnitType}  I:{_offer.Infantry} C:{_offer.Cavalry} A:{_offer.Archers}";
            }
        }

        public void SetSelected(bool selected)
        {
            if (_background != null)
                _background.color = selected ? _selectedColor : _normalColor;
        }
    }
}
