using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Campaign.UI
{
    public sealed class CampaignResultPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _outcomeText;
        [SerializeField] private TMP_Text _controlTimeText;
        [SerializeField] private TMP_Text _silverText;
        [SerializeField] private Button _okButton;

        public event Action OkClicked;

        private void Awake()
        {
            if (_okButton != null)
                _okButton.onClick.AddListener(() => OkClicked?.Invoke());
            // Prefab starts inactive; CampaignFlowController owns visibility.
        }

        public void Show(Campaign campaign, int silverAmount)
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);

            if (_titleText != null)
                _titleText.text = "CAMPAIGN COMPLETE";

            if (_outcomeText != null)
            {
                _outcomeText.text = campaign.Outcome switch
                {
                    CampaignOutcome.Won => "COMMANDER WON",
                    CampaignOutcome.Lost => "F.I.S.T. WON",
                    CampaignOutcome.Draw => "DRAW / UNRESOLVED",
                    _ => "—"
                };
            }

            if (_controlTimeText != null && campaign != null)
            {
                double cmdH = campaign.CommanderControlDurationGameSeconds / 3600d;
                double fistH = campaign.FistControlDurationGameSeconds / 3600d;
                _controlTimeText.text =
                    "Control Time\n" +
                    $"Commander: {cmdH.ToString("0", CultureInfo.InvariantCulture)}h\n" +
                    $"F.I.S.T.: {fistH.ToString("0", CultureInfo.InvariantCulture)}h";
            }

            if (_silverText != null)
            {
                string sign = silverAmount >= 0 ? "+" : string.Empty;
                _silverText.text = $"Silver: {sign}{silverAmount}";
            }

        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
