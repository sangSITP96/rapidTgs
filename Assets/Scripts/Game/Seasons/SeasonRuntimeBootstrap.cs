using Game.Core.WorldTime;
using UnityEngine;

namespace Game.Seasons
{
    /// <summary>
    /// Ensures SeasonSystem + SeasonWeatherDirector exist at runtime for Alpha scenes.
    /// Attach next to WorldTime, or rely on AfterSceneLoad auto-bootstrap when WorldTime is present.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class SeasonRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private WorldTime _worldTime;
        [SerializeField] private SeasonCalendarConfig _calendar;
        [SerializeField] private SeasonWeatherMapping _mapping;
        [SerializeField] private bool _addDebugController = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (FindFirstObjectByType<SeasonSystem>() != null)
                return;
            if (FindFirstObjectByType<WorldTime>() == null)
                return;

            GameObject go = new GameObject("SeasonSystem");
            go.AddComponent<SeasonRuntimeBootstrap>();
        }

        private void Awake()
        {
            if (_worldTime == null)
                _worldTime = FindFirstObjectByType<WorldTime>();

            SeasonSystem seasons = FindFirstObjectByType<SeasonSystem>();
            if (seasons == null)
                seasons = gameObject.GetComponent<SeasonSystem>() ?? gameObject.AddComponent<SeasonSystem>();

            if (_calendar != null)
                seasons.SetCalendar(_calendar);

            if (FindFirstObjectByType<SeasonWeatherDirector>() == null &&
                gameObject.GetComponent<SeasonWeatherDirector>() == null)
            {
                gameObject.AddComponent<SeasonWeatherDirector>();
            }

            if (_addDebugController && FindFirstObjectByType<DebugTools.SeasonDebugController>() == null)
                gameObject.AddComponent<DebugTools.SeasonDebugController>();
        }
    }
}
