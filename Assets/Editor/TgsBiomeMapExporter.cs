#if UNITY_EDITOR
using System.IO;
using TGS;
using UnityEditor;
using UnityEngine;

public class TgsBiomeMapExporter : EditorWindow
{
   [SerializeField] private TerrainGridSystem _tgs;
   [SerializeField] private TgsBiomeMapData _mapData;
   [SerializeField] private MapChunkData _chunkData;
   [SerializeField] private InfiniteMapStreamer _streamer;
   [SerializeField] private int _width = 1024;
   [SerializeField] private int _height = 1024;

   [MenuItem("Tools/Map/TGS Biome Map Export")]
   public static void ShowWindow()
   {
      GetWindow<TgsBiomeMapExporter>("TGS Biome Export");
   }

   private void OnGUI()
   {
      _tgs = (TerrainGridSystem)EditorGUILayout.ObjectField("TGS", _tgs, typeof(TerrainGridSystem), true);
      _mapData = (TgsBiomeMapData)EditorGUILayout.ObjectField("Map Data", _mapData, typeof(TgsBiomeMapData), false);
      _chunkData = (MapChunkData)EditorGUILayout.ObjectField("Map Chunk Data", _chunkData, typeof(MapChunkData), false);
      _streamer = (InfiniteMapStreamer)EditorGUILayout.ObjectField("Streamer (optional)", _streamer, typeof(InfiniteMapStreamer), false);
      _width = EditorGUILayout.IntField("Width", _width);
      _height = EditorGUILayout.IntField("Height", _height);

      if (GUILayout.Button("Export Biome Base Map"))
         Export();
   }

   private void Export()
   {
      if(_tgs == null || _mapData == null) return;

      _width = Mathf.Max(64, _width);
      _height = Mathf.Max(64, _height);

      var tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
      tex.filterMode = FilterMode.Point;
      tex.wrapMode = TextureWrapMode.Clamp;
      
      float minX, maxX, minZ, maxZ;

      if (_streamer != null && _streamer.TryGetWorldBounds(out minX, out maxX, out minZ, out maxZ))
      {
      }
      else
      {
         Bounds b = _tgs.bounds;
         minX = b.min.x;
         maxX = b.max.x;
         minZ = b.min.z;
         maxZ = b.max.z;
      }
      
      var pixels = new Color32[_width * _height];

      for (int y = 0; y < _height; y++)
      {
         float v = (y + 0.5f) / _width;
         float wz = Mathf.Lerp(minZ, maxZ, v);

         for (int x = 0; x < _width; x++)
         {
            float u = (x + 0.5f) / _width;
            float wx = Mathf.Lerp(minX, maxX, u);

            var worldPos = new Vector3(wx, 0f, wz);
            Cell cell = _tgs.CellGetAtWorldPosition(worldPos);

            Color32 color = BiomePalette.Grassland;

            if (cell != null && cell.territoryIndex >= 0)
            {
               BiomeType biome = _mapData.GetBiomeForTerritory(cell.territoryIndex);
               color = BiomePalette.GetColor(biome);
            }
            
            pixels[y * _width + x] = color;
         }
      }
      
      tex.SetPixels32(pixels);
      tex.Apply(false, false);

      string folder = "Assets/BiomeBaseMaps";
      if(!AssetDatabase.IsValidFolder(folder))
         AssetDatabase.CreateFolder("Assets", "BiomeBaseMaps");
      
      string fileName = $"BiomeBaseMap_{_width}x_{_height}.png";
      string assetPath = $"{folder}/{fileName}";
      File.WriteAllBytes(assetPath, tex.EncodeToPNG());
      AssetDatabase.ImportAsset(assetPath);
      
      var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
      importer.textureType = TextureImporterType.Default;
      importer.sRGBTexture = true;
      importer.alphaIsTransparency = false;
      importer.mipmapEnabled = false;
      importer.filterMode = FilterMode.Point;
      importer.textureCompression = TextureImporterCompression.Uncompressed;
      importer.isReadable = true;
      importer.npotScale = TextureImporterNPOTScale.None;
      importer.SaveAndReimport();
      
      var savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

      if (_chunkData != null)
      {
         _chunkData.BiomeBaseMap = savedTex;
         EditorUtility.SetDirty(_chunkData);
         AssetDatabase.SaveAssets();
      }
      
      Debug.Log($"[TGS Biome] Exported {assetPath}");
   }
}
#endif
