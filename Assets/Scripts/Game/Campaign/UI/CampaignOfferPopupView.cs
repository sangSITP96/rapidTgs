using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignOfferPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _territoryText;
        [SerializeField] private TMP_Text _objectiveText;
        [SerializeField] private Button _declineButton;
        [SerializeField] private Button _acceptButton;

        public event Action AcceptClicked;
        public event Action DeclineClicked;

        private void Awake()
        {
            if (_declineButton != null)
                _declineButton.onClick.AddListener(() => DeclineClicked?.Invoke());
            if (_acceptButton != null)
                _acceptButton.onClick.AddListener(() => AcceptClicked?.Invoke());
            // Prefab starts inactive; CampaignFlowController owns visibility.
        }

        public void Show(string territoryLabel, string objectiveLabel)
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "CAMPAIGN";
            if (_territoryText != null)
                _territoryText.text = territoryLabel ?? string.Empty;
            if (_objectiveText != null)
                _objectiveText.text = "Objective: " + (objectiveLabel ?? "Capture Territory");
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
