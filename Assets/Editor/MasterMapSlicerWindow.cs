using UnityEditor;
using UnityEngine;

public class MasterMapSlicerWindow : EditorWindow
{
    private Texture2D _masterMap;
    private int _columns = 7;
    private int _rows = 7;
    private string _outputFolder = "Assets/SlicedMaps";

    [MenuItem("Tools/Map/Slice Master Map")]
    public static void ShowWindow()
    {
    }
}
