using UnityEngine;

public class ColonyEventLogBootstrap : MonoBehaviour
{
    [SerializeField] private bool _seedExamplesOnStart = true;

    private void Start()
    {
        if (!_seedExamplesOnStart)
        {
            return;
        }

        var log = ColonyEventLogService.Instance;

        if (log == null || log.Events.Count > 0)
        {
            return;
        }
        log.AddSimple(EventCategory.Weather, "Storm Forming", "A Storm is forming near the colony.");
        log.AddSimple(EventCategory.Weather, "Mighty Storm Detected", "An unusually strong storm has been detectd.");
        log.AddSimple(EventCategory.Military, "Military Detected", "Summary of Military.");
    }
}
