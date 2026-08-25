using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMenuController : MonoBehaviour
{
    public void LoadAllStacked()
    {
        WeatherPresetSelection.CurrentPreset = WeatherTestPreset.AllStacked;
        LoadGamePlayScene();
    }

    public void LoadSnowOnly()
    {
        WeatherPresetSelection.CurrentPreset = WeatherTestPreset.SnowOnly;
        LoadGamePlayScene();
    }
    
    public void LoadRainOnly()
    {
        WeatherPresetSelection.CurrentPreset = WeatherTestPreset.RainOnly;
        LoadGamePlayScene();
    }

    public void LoadHeatOnly()
    {
        WeatherPresetSelection.CurrentPreset = WeatherTestPreset.HeatOnly;
        LoadGamePlayScene();
    }

    public void LoadGroundSnowOnly()
    {
        WeatherPresetSelection.CurrentPreset = WeatherTestPreset.GroundSnowOnly;
        LoadGamePlayScene();
    }

    private void LoadGamePlayScene()
    {
        SceneManager.LoadScene("RapidTgsPrototype_main");
    }
}
