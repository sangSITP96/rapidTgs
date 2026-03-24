using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapChunkDataAutoBuilderWindow : EditorWindow
{
    private InfiniteMapStreamer _streamer;

    private DefaultAsset _visualFolder;
    private DefaultAsset _heightFolder;
    private DefaultAsset _smallLakeFolder;
    private DefaultAsset _bigLakeFolder;
    private DefaultAsset _forestFolder;

    private DefaultAsset _outputDataFolder;
    private int _columns = 3;
    private int _rows = 4;

    private bool _writePredefinedToStreamer = true;

    [MenuItem("Tools/Map/Build MapChunkData + Predefined")]
    public static void ShowWindow()
    {
        GetWindow<MapChunkDataAutoBuilderWindow>("ChunkData Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Build MapChunkData + Streamer Predefined", EditorStyles.boldLabel);

        _streamer = (InfiniteMapStreamer)EditorGUILayout.ObjectField("InfiniteMapStreamer", _streamer, typeof(InfiniteMapStreamer), true);

        EditorGUILayout.Space();
        _columns = EditorGUILayout.IntField("Columns (N)", _columns);
        _rows = EditorGUILayout.IntField("Rows (M)", _rows);

        EditorGUILayout.Space();
        _visualFolder = (DefaultAsset)EditorGUILayout.ObjectField("Visual Folder", _visualFolder, typeof(DefaultAsset), false);
        _heightFolder = (DefaultAsset)EditorGUILayout.ObjectField("Height Folder", _heightFolder, typeof(DefaultAsset), false);
        _smallLakeFolder = (DefaultAsset)EditorGUILayout.ObjectField("SmallLake Folder", _smallLakeFolder, typeof(DefaultAsset), false);
        _bigLakeFolder = (DefaultAsset)EditorGUILayout.ObjectField("BigLake Folder", _bigLakeFolder, typeof(DefaultAsset), false);
        _forestFolder = (DefaultAsset)EditorGUILayout.ObjectField("Forest Folder", _forestFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();
        _outputDataFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Data Folder (Assets/...)", _outputDataFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();
        _writePredefinedToStreamer = EditorGUILayout.Toggle("Auto-fill Streamer.Predefined", _writePredefinedToStreamer);

        if (GUILayout.Button("Build"))
        {
            Build();
        }
    }

    private void Build()
    {
        if (_visualFolder == null || _outputDataFolder == null)
        {
            Debug.LogError("[ChunkDataBuilder] VisualFolder and OutputDataFolder are required.");
            return;
        }

        string visualPath = AssetDatabase.GetAssetPath(_visualFolder);
        string heightPath = _heightFolder ? AssetDatabase.GetAssetPath(_heightFolder) : null;
        string smallPath = _smallLakeFolder ? AssetDatabase.GetAssetPath(_smallLakeFolder) : null;
        string bigPath = _bigLakeFolder ? AssetDatabase.GetAssetPath(_bigLakeFolder) : null;
        string forestPath = _forestFolder ? AssetDatabase.GetAssetPath(_forestFolder) : null;

        string dataOutPath = AssetDatabase.GetAssetPath(_outputDataFolder);
        if (!AssetDatabase.IsValidFolder(dataOutPath))
        {
            Debug.LogError("[ChunkDataBuilder] OutputDataFolder must be a valid folder under Assets.");
            return;
        }

        var entries = new List<(Vector2Int coord, MapChunkData data)>();

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _columns; x++)
            {
                string name = $"MapChunkData_y{y}_x{x}";
                string assetPath = $"{dataOutPath}/{name}.asset";

                MapChunkData data = AssetDatabase.LoadAssetAtPath<MapChunkData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<MapChunkData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.Visual = LoadTile(visualPath, y, x);
                data.Height = LoadTile(heightPath, y, x);
                data.SmallLake = LoadTile(smallPath, y, x);
                data.BigLake = LoadTile(bigPath, y, x);
                data.Forest = LoadTile(forestPath, y, x);

                EditorUtility.SetDirty(data);

                entries.Add((new Vector2Int(x, y), data));
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (_writePredefinedToStreamer)
        {
            if (_streamer == null)
            {
                Debug.LogWarning("[ChunkDataBuilder] Streamer is null, skipping Predefined fill.");
                return;
            }

            // Fill private field `_predefined` via SerializedObject
            SerializedObject so = new SerializedObject(_streamer);
            SerializedProperty predefined = so.FindProperty("_predefined");
            if (predefined == null || !predefined.isArray)
            {
                Debug.LogError("[ChunkDataBuilder] Could not find _predefined on streamer.");
                return;
            }

            predefined.ClearArray();
            predefined.arraySize = entries.Count;

            for (int i = 0; i < entries.Count; i++)
            {
                var (coord, data) = entries[i];
                SerializedProperty elem = predefined.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("coord").vector2IntValue = coord;
                elem.FindPropertyRelative("data").objectReferenceValue = data;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(_streamer);

            Debug.Log($"[ChunkDataBuilder] Created/updated {entries.Count} MapChunkData and filled Streamer.Predefined.");
        }
        else
        {
            Debug.Log($"[ChunkDataBuilder] Created/updated {entries.Count} MapChunkData (Predefined not modified).");
        }
    }

    private static Texture2D LoadTile(string folderPath, int y, int x)
    {
        if (string.IsNullOrEmpty(folderPath)) return null;
        string fileName = $"Tile_y{y}_x{x}.png";
        string path = $"{folderPath}/{fileName}";
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
