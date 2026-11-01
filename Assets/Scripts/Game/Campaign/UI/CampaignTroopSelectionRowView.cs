using System;
using Game.Battle;
using Game.Troops;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignTroopSelectionRowView : MonoBehaviour, IPointerClickHandler
    {
        private const float NameFontMax = 18f;
        private const float NameFontMin = 10f;

        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _capacityText;
        [SerializeField] private TMP_Text _compositionText;

        private TroopUnit _unit;
        private bool _layoutConfigured;

        public TroopUnit Unit => _unit;
        public bool IsSelected => _toggle != null && _toggle.isOn;

        public event Action SelectionChanged;

        private void Awake()
        {
            if (_toggle != null)
                _toggle.onValueChanged.AddListener(_ => SelectionChanged?.Invoke());

            ConfigureLayout();
        }

        public void Bind(TroopUnit unit)
        {
            ConfigureLayout();

            _unit = unit;
            if (unit == null)
                return;

            ArmyComposition agg = unit.GetAggregateComposition();
            if (_nameText != null)
            {
                _nameText.text = unit.Name;
                // Re-run auto-size after text change / font scale from parent.
                _nameText.ForceMeshUpdate();
            }

            if (_capacityText != null)
                _capacityText.text = agg.Total.ToString();
            if (_compositionText != null)
                _compositionText.text = $"I:{agg.Infantry} C:{agg.Cavalry} A:{agg.Archers}";
            if (_toggle != null)
            {
                _toggle.interactable = true;
                _toggle.SetIsOnWithoutNotify(false);
            }
        }

        public void SetSelected(bool selected)
        {
            if (_toggle != null)
                _toggle.SetIsOnWithoutNotify(selected);
        }

        /// <summary>
        /// Whole-row click toggles selection (checkbox is only 28px and easy to miss).
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_toggle == null || !_toggle.interactable)
                return;

            // Ignore if the click already landed on the toggle control itself.
            if (eventData.pointerPress != null &&
                eventData.pointerPress.transform.IsChildOf(_toggle.transform))
                return;

            _toggle.isOn = !_toggle.isOn;
        }

        /// <summary>
        /// Fit full name inside the row via TMP auto-size (shrink font, no ellipsis).
        /// </summary>
        private void ConfigureLayout()
        {
            if (_layoutConfigured)
                return;

            _layoutConfigured = true;

            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();

            var bg = GetComponent<Image>();
            if (bg != null)
                bg.raycastTarget = true;

            if (_nameText != null)
            {
                _nameText.raycastTarget = false;
                _nameText.enableWordWrapping = false;
                _nameText.overflowMode = TextOverflowModes.Overflow;
                _nameText.alignment = TextAlignmentOptions.MidlineLeft;
                _nameText.enableAutoSizing = true;
                _nameText.fontSizeMin = NameFontMin;
                _nameText.fontSizeMax = NameFontMax;

                RectTransform nameRt = _nameText.rectTransform;
                nameRt.anchorMin = new Vector2(0f, 0.48f);
                nameRt.anchorMax = new Vector2(1f, 0.95f);
                nameRt.pivot = new Vector2(0f, 1f);
                // Leave space for toggle (left) and capacity number (right).
                nameRt.offsetMin = new Vector2(48f, 0f);
                nameRt.offsetMax = new Vector2(-68f, 0f);
            }

            if (_capacityText != null)
            {
                _capacityText.raycastTarget = false;
                _capacityText.enableWordWrapping = false;
                _capacityText.enableAutoSizing = false;
                _capacityText.alignment = TextAlignmentOptions.Right;

                RectTransform capRt = _capacityText.rectTransform;
                capRt.anchorMin = new Vector2(1f, 0.48f);
                capRt.anchorMax = new Vector2(1f, 0.95f);
                capRt.pivot = new Vector2(1f, 1f);
                capRt.anchoredPosition = new Vector2(-10f, 0f);
                capRt.sizeDelta = new Vector2(56f, 28f);
            }

            if (_compositionText != null)
            {
                _compositionText.raycastTarget = false;
                _compositionText.enableWordWrapping = false;
                _compositionText.enableAutoSizing = true;
                _compositionText.fontSizeMin = 10f;
                _compositionText.fontSizeMax = 14f;
                _compositionText.overflowMode = TextOverflowModes.Overflow;

                RectTransform compRt = _compositionText.rectTransform;
                compRt.anchorMin = new Vector2(0f, 0.08f);
                compRt.anchorMax = new Vector2(1f, 0.48f);
                compRt.offsetMin = new Vector2(48f, 0f);
                compRt.offsetMax = new Vector2(-12f, 0f);
            }
        }
    }
}
