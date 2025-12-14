using UnityEngine;

public enum WeatherTestPreset
{
    Flexible,
    AllStacked,
    SnowOnly,
    RainOnly,
    HeatOnly,
    GroundSnowOnly
}

public enum WeatherGroup
{
    Snow,
    Rain,
    Heat,
    GroundSnow
}

public static class WeatherGroupUtils
{
    public static bool IsInGroup(WeatherType type, WeatherGroup group)
    {
        switch (group)
        {
            case WeatherGroup.Snow:
                return type == WeatherType.Blizzard || type == WeatherType.HeavySnow ||
                       type == WeatherType.ModerateSnow || type == WeatherType.LightSnow;
            case WeatherGroup.Rain:
                return type == WeatherType.BrutalRain || type == WeatherType.Thunderstorm ||
                       type == WeatherType.HeavyRain || type == WeatherType.ModerateRain ||
                       type == WeatherType.LightRain;
            case WeatherGroup.Heat:
                return type == WeatherType.ExtremeHeat || type == WeatherType.SevereHeat ||
                       type == WeatherType.Hot;
            case WeatherGroup.GroundSnow:
                return type == WeatherType.DeepGroundSnow || type == WeatherType.ShallowGroundSnow;
        }

        return false;
    }
}

public class WeatherPresetApplier : MonoBehaviour
{
    public WeatherManager WeatherManager;
    public WeatherTestPreset Preset;

    private void Start()
    {
        ApplyPreset(WeatherPresetSelection.CurrentPreset);
    }

    private void ApplyPreset(WeatherTestPreset preset)
    {
        var db = WeatherManager.Database;
        if (db == null) return;

        if (preset != WeatherTestPreset.Flexible)
        {
            foreach (var entry in db.entries)
            {
                entry.isActive = false;
            }
        }

        switch (preset)
        {
            case WeatherTestPreset.Flexible:
                break;
            case WeatherTestPreset.AllStacked:
                foreach (var entry in db.entries)
                {
                    entry.isActive = true;
                }
                break;
            case WeatherTestPreset.SnowOnly:
                ActiveGroup(db, WeatherGroup.Snow);
                break;
            case WeatherTestPreset.RainOnly:
                ActiveGroup(db, WeatherGroup.Rain);
                break;
            case WeatherTestPreset.HeatOnly:
                ActiveGroup(db, WeatherGroup.Heat);
                break;
            case WeatherTestPreset.GroundSnowOnly:
                ActiveGroup(db, WeatherGroup.GroundSnow);
                break;
        }
    }

    private void ActiveGroup(WeatherSettingsDatabase db, WeatherGroup group)
    {
        foreach (var entry in db.entries)
        {
            if (WeatherGroupUtils.IsInGroup(entry.Type, group))
                entry.isActive = true;
        }
    }
}