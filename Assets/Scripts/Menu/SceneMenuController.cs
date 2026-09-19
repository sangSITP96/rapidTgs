using UnityEngine;

public class SceneMenuController : MonoBehaviour
{
    [SerializeField] private WeatherPresetApplier _presetApplier;

    private void Awake()
    {
        if (_presetApplier == null)
            _presetApplier = FindFirstObjectByType<WeatherPresetApplier>();
    }

    public void LoadAllStacked()
    {
        ApplyPreset(WeatherTestPreset.AllStacked);
    }

    public void LoadSnowOnly()
    {
        ApplyPreset(WeatherTestPreset.SnowOnly);
    }

    public void LoadRainOnly()
    {
        ApplyPreset(WeatherTestPreset.RainOnly);
    }

    public void LoadHeatOnly()
    {
        ApplyPreset(WeatherTestPreset.HeatOnly);
    }

    public void LoadGroundSnowOnly()
    {
        ApplyPreset(WeatherTestPreset.GroundSnowOnly);
    }

    public void LoadFlexible()
    {
        ApplyPreset(WeatherTestPreset.Flexible);
    }

    private void ApplyPreset(WeatherTestPreset preset)
    {
        WeatherPresetSelection.CurrentPreset = preset;

        if (_presetApplier == null)
            _presetApplier = FindFirstObjectByType<WeatherPresetApplier>();

        if (_presetApplier != null)
            _presetApplier.ApplyPresetNow(preset);
        else
            Debug.LogWarning($"{nameof(SceneMenuController)}: WeatherPresetApplier not found.");
    }
}
