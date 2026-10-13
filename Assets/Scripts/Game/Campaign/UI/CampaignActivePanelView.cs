using System.Globalization;
using Game.Core.WorldTime;
using TMPro;
using UnityEngine;

namespace Game.Campaign.UI
{
    public sealed class CampaignActivePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _territoryText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _controllerText;
        [SerializeField] private TMP_Text _controlTimeText;
        [SerializeField] private TMP_Text _lastBattleText;
        [SerializeField] private TMP_Text _silverText;
        [SerializeField] private TMP_Text _troopsStatusText;

        public void Show()
        {
            gameObject.SetActive(true);
            if (_root != null && _root != gameObject)
                _root.SetActive(true);
            if (_titleText != null)
                _titleText.text = "CAMPAIGN";
        }

        public void Hide()
        {
            if (_root != null && _root != gameObject)
                _root.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Refresh(
            Campaign campaign,
            CampaignSettings settings,
            WorldTime worldTime,
            string territoryLabel,
            int projectedSilver,
            int marchingTroops,
            int inCampTroops)
        {
            if (campaign == null)
                return;

            if (_territoryText != null)
                _territoryText.text = territoryLabel ?? string.Empty;

            double elapsedHours = 0d;
            double durationHours = settings != null ? settings.CampaignDurationGameHours : 24f;
            if (worldTime != null && campaign.StartedAtTotalGameSeconds >= 0d)
                elapsedHours = (worldTime.TotalGameSeconds - campaign.StartedAtTotalGameSeconds) / 3600d;

            if (_timeText != null)
            {
                _timeText.text =
                    $"Time: {elapsedHours.ToString("0", CultureInfo.InvariantCulture)}h / " +
                    $"{durationHours.ToString("0", CultureInfo.InvariantCulture)}h";
            }

            if (_controllerText != null)
            {
                _controllerText.text = campaign.CurrentController == CampaignControllerSide.Commander
                    ? "Control: Commander"
                    : "Control: F.I.S.T.";
            }

            if (_controlTimeText != null)
            {
                double cmdH = campaign.CommanderControlDurationGameSeconds / 3600d;
                double fistH = campaign.FistControlDurationGameSeconds / 3600d;
                _controlTimeText.text =
                    "CONTROL TIME\n" +
                    $"Commander: {cmdH.ToString("0", CultureInfo.InvariantCulture)}h\n" +
                    $"F.I.S.T.:  {fistH.ToString("0", CultureInfo.InvariantCulture)}h";
            }

            if (_lastBattleText != null)
            {
                if (campaign.BattleHistory.Count == 0)
                {
                    _lastBattleText.text = "LAST BATTLE\n—";
                }
                else
                {
                    CampaignBattleRecord last = campaign.BattleHistory[campaign.BattleHistory.Count - 1];
                    string resultLabel = last.SideResult switch
                    {
                        CampaignBattleSideResult.CommanderWin => "Commander Won",
                        CampaignBattleSideResult.FistWin => "F.I.S.T. Won",
                        CampaignBattleSideResult.Draw => "Draw",
                        _ => "—"
                    };
                    _lastBattleText.text =
                        "LAST BATTLE\n" +
                        $"Commander: {(last.CommanderWinningPercentage * 100f).ToString("0", CultureInfo.InvariantCulture)}%\n" +
                        $"F.I.S.T.:  {(last.FistWinningPercentage * 100f).ToString("0", CultureInfo.InvariantCulture)}%\n" +
                        resultLabel;
                }
            }

            if (_silverText != null)
                _silverText.text = "Silver: " + projectedSilver;

            if (_troopsStatusText != null)
            {
                int supportInCamp = 0;
                if (campaign.Supports != null)
                {
                    for (int s = 0; s < campaign.Supports.Count; s++)
                    {
                        var support = campaign.Supports[s];
                        if (support == null || support.Response != CampaignResponse.Accepted)
                            continue;
                        for (int i = 0; i < support.Contributions.Count; i++)
                        {
                            var c = support.Contributions[i];
                            if (c != null && c.Status == TroopContributionStatus.InCamp)
                                supportInCamp += c.GetComposition().Total;
                        }
                    }
                }

                if (marchingTroops > 0 && inCampTroops <= 0)
                {
                    _troopsStatusText.text = $"Troops: Marching ({marchingTroops})";
                }
                else if (inCampTroops > 0)
                {
                    // inCampTroops already includes Support; show breakdown when present.
                    _troopsStatusText.text = supportInCamp > 0
                        ? $"Troops: At Camp ({inCampTroops})\nincl. Support {supportInCamp}"
                        : $"Troops: At Camp ({inCampTroops})";
                }
                else
                {
                    _troopsStatusText.text = "Troops: —";
                }
            }
        }

    }
}
