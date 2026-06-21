using System;
using UnityEngine;
using UnityEngine.UI;

public class ColonyEventLogDetailView : MonoBehaviour
{
    [SerializeField] Text titleTxt;
    [SerializeField] Text categoryText;
    [SerializeField] Text timeStampText;

    [SerializeField] Text desText;

    [SerializeField] GameObject content;

    private void Start()
    {
        content.SetActive(false);
    }
    public void ShowColonyEventDetail(ColonyEventEntry colonyData)
    {
        content.SetActive(true);
        titleTxt.text = colonyData.Title;
        categoryText.text = colonyData.Category.ToString();

        DateTimeOffset time = DateTimeOffset
                .FromUnixTimeMilliseconds(colonyData.TimestampUnixMsUtc)
                .ToLocalTime();
        timeStampText.text = $"{time:yyyy-MM-dd HH:mm} UTC";
        desText.text = colonyData.Summary;

    }

    private void OnDisable()
    {
        content.SetActive(false);
    }
}
