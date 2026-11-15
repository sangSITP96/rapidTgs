using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Recruitment.UI
{
    /// <summary>
    /// One ledger row in Mercenary Terminal:
    /// UNIT | TROOPS | SEASON AFFINITY | TYPE | COMBAT AFFINITY | GOLD | HIRE
    /// </summary>
    public sealed class MercenaryRecruitmentRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _unitText;
        [SerializeField] private TMP_Text _troopsText;
        [SerializeField] private TMP_Text _seasonText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _combatText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Button _hireButton;
        [SerializeField] private TMP_Text _hireButtonLabel;

        private MercenaryOfferRuntimeState _offer;
        private bool _hovered;
        private bool _selected;
        private bool _hireEnabled = true;

        private static readonly Color NormalColor = new Color(0.90f, 0.86f, 0.78f, 0.35f);
        private static readonly Color HoverColor = new Color(0.78f, 0.70f, 0.55f, 0.75f);
        private static readonly Color SelectedColor = new Color(0.72f, 0.62f, 0.48f, 0.90f);
        private static readonly Color SoldOutColor = new Color(0.85f, 0.82f, 0.78f, 0.25f);
        private static readonly Color InkColor = new Color(0.18f, 0.16f, 0.14f, 1f);

        public MercenaryOfferRuntimeState Offer => _offer;

        public event Action<MercenaryRecruitmentRowView> Selected;
        public event Action<MercenaryRecruitmentRowView> HireClicked;

        private void Awake()
        {
            if (_hireButton != null)
                _hireButton.onClick.AddListener(() => HireClicked?.Invoke(this));

            ApplyTextStyle(_unitText);
            ApplyTextStyle(_troopsText);
            ApplyTextStyle(_seasonText);
            ApplyTextStyle(_typeText);
            ApplyTextStyle(_combatText);
            ApplyTextStyle(_goldText);
            if (_hireButtonLabel != null)
            {
                _hireButtonLabel.raycastTarget = false;
                _hireButtonLabel.color = InkColor;
            }
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

            if (_unitText != null)
                _unitText.text = _offer.UnitColumnLabel;
            if (_troopsText != null)
                _troopsText.text = _offer.TroopCount.ToString("N0", CultureInfo.InvariantCulture);
            if (_seasonText != null)
                _seasonText.text = _offer.SeasonAffinityLabel;
            if (_typeText != null)
                _typeText.text = _offer.TypeLabel;
            if (_combatText != null)
                _combatText.text = _offer.CombatAffinityLabel;
            if (_goldText != null)
                _goldText.text = _offer.GoldPrice.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

            if (_hireButtonLabel != null)
                _hireButtonLabel.text = _offer.IsSoldOut ? "SOLD" : "Hire";

            RefreshHireInteractable();
            RefreshBackground();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshBackground();
        }

        public void SetHireEnabled(bool enabled)
        {
            _hireEnabled = enabled;
            RefreshHireInteractable();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            RefreshBackground();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            RefreshBackground();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Don't treat Hire button presses as row-select only — HireClicked already handles that.
            if (_hireButton != null &&
                eventData.pointerPress != null &&
                eventData.pointerPress.transform.IsChildOf(_hireButton.transform))
                return;

            Selected?.Invoke(this);
        }

        private void RefreshHireInteractable()
        {
            if (_hireButton == null)
                return;

            bool can = _hireEnabled && _offer != null && !_offer.IsSoldOut;
            _hireButton.interactable = can;
        }

        private void RefreshBackground()
        {
            if (_background == null)
                return;

            if (_offer != null && _offer.IsSoldOut)
                _background.color = SoldOutColor;
            else if (_selected)
                _background.color = SelectedColor;
            else if (_hovered)
                _background.color = HoverColor;
            else
                _background.color = NormalColor;
        }

        private static void ApplyTextStyle(TMP_Text text)
        {
            if (text == null)
                return;

            text.raycastTarget = false;
            text.color = InkColor;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 18f;
        }
    }
}
