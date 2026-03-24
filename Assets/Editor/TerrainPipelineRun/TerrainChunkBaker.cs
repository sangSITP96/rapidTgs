using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TerrainChunkBaker
{
   public static void BakeMapChunkDataAndFillStreamer(
       InfiniteMapStreamer streamer,
       int columns,
       int rows,
       string tilesRootAssetPath,
       string mapChunkDataFolderAssetPath)
    {
        string visualDir = $"{tilesRootAssetPath}/Visual";
        string heightDir = $"{tilesRootAssetPath}/Height";
        string forestDir = $"{tilesRootAssetPath}/Forest";
        string smallLakeDir = $"{tilesRootAssetPath}/SmallLake";
        string bigLakeDir = $"{tilesRootAssetPath}/BigLake";

        var entries = new List<(Vector2Int coord, MapChunkData data)>();

        for(int y=0;y<rows;y++)
        {
            for(int x =0;x<columns;x++)
            {
                string tileName = $"Tile_y{y}_x{x}.png";

                string visPath = $"{visualDir}/{tileName}";
                string hPath = $"{heightDir}/{tileName}";
                string fPath = $"{forestDir}/{tileName}";
                string sPath = $"{smallLakeDir}/{tileName}";
                string bPath = $"{bigLakeDir}/{tileName}";

                Texture2D vis = AssetDatabase.LoadAssetAtPath<Texture2D>(visPath);
                Texture2D h = AssetDatabase.LoadAssetAtPath<Texture2D>(hPath);
                Texture2D f = AssetDatabase.LoadAssetAtPath<Texture2D>(fPath);
                Texture2D s = AssetDatabase.LoadAssetAtPath<Texture2D>(sPath);
                Texture2D b = AssetDatabase.LoadAssetAtPath<Texture2D>(bPath);

                if(vis == null || h == null || f == null || s == null || b == null)
                {
                    Debug.LogError($"Missing tile textures for ({x}, {y}). Check: {tileName}");
                    return;
                }

                string dataName = $"MapChunkData_y{y}_x{x}";
                string assetPath = $"{mapChunkDataFolderAssetPath}/{dataName}.asset";

                MapChunkData data = AssetDatabase.LoadAssetAtPath<MapChunkData>(assetPath);
                if(data == null)
                {
                    data = ScriptableObject.CreateInstance<MapChunkData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.Visual = vis;
                data.Height = h;
                data.Forest = f;
                data.SmallLake = s;
                data.BigLake = b;

                EditorUtility.SetDirty(data);

                entries.Add((new Vector2Int(x, y), data));
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SerializedObject so = new SerializedObject(streamer);
        SerializedProperty predefined = so.FindProperty("predefined");

        if(predefined == null || !predefined.isArray)
        {
            Debug.LogError("Missing '_predefined' field on InfiniteStreamer.");
            return;
        }

        predefined.ClearArray();
        predefined.arraySize = entries.Count;

        for(int i = 0; i< entries.Count; i++)
        {
            var entry = entries[i];

            SerializedProperty elem = predefined.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("coord").vector2IntValue = entry.coord;
            elem.FindPropertyRelative("data").objectReferenceValue = entry.data;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(streamer);

        Debug.Log($"Baked {entries.Count} MapChunkData + filled streamer");
    }
}
