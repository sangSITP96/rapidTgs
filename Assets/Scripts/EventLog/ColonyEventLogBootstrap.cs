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
    }
}
