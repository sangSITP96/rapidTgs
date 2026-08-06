#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Multi-type terrain detection bake (Lake / Mountain / Forest).
/// Priority when claiming pixels: Lake &gt; Mountain &gt; Forest.
/// </summary>
public class TerrainDetectionBakeWindow : EditorWindow
{
    private enum BakeSourceMode
    {
        MapChunkDataAssets,
        VisualTileFolder
    }

    [SerializeField] private TerrainDetectionConfig _lakeConfig;
    [SerializeField] private TerrainDetectionConfig _mountainConfig;
    [SerializeField] private TerrainDetectionConfig _forestConfig;

    [SerializeField] private bool _bakeLake = true;
    [SerializeField] private bool _bakeMountain = true;
    [SerializeField] private bool _bakeForest = true;

    [SerializeField] private BakeSourceMode _sourceMode = BakeSourceMode.MapChunkDataAssets;
    [SerializeField] private DefaultAsset _mapChunkDataFolder;
    [SerializeField] private DefaultAsset _visualTileFolder;
    [SerializeField] private DefaultAsset _outputDataFolder;
    [SerializeField] private int _columns = 3;
    [SerializeField] private int _rows = 4;

    [SerializeField] private bool _skipLockedChunks = true;
    [SerializeField] private bool _forceRebake;
    [SerializeField] private bool _preserveLockedOnVisualChange = true;
    [SerializeField] private bool _exportPreviewMasks;
    [SerializeField] private DefaultAsset _previewMaskFolder;
    [SerializeField] private bool _clearLegacyGrayscaleMasks = true;

    private Vector2 _scroll;
    private string _lastLog = string.Empty;

    [MenuItem("Tools/Map/Terrain Detection Bake")]
    public static void ShowWindow()
    {
        var window = GetWindow<TerrainDetectionBakeWindow>("Terrain Bake");
        window.minSize = new Vector2(440, 620);
    }

    [MenuItem("Tools/Map/Lake Detection & Collider Bake")]
    public static void ShowLegacyLakeWindow()
    {
        ShowWindow();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawConfigSection();
        EditorGUILayout.Space(8);
        DrawSourceSection();
        EditorGUILayout.Space(8);
        DrawBakeOptions();
        EditorGUILayout.Space(8);
        DrawActions();
        EditorGUILayout.Space(8);
        DrawLog();

        EditorGUILayout.EndScrollView();
    }

    private void DrawConfigSection()
    {
        EditorGUILayout.LabelField("1. Terrain Detection Configs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Priority (each pixel claimed once): Lake > Mountain > Forest.\n" +
            "Create assets via Create > World > Terrain Detection Config.",
            MessageType.Info);

        DrawConfigRow("Lake", ref _lakeConfig, ref _bakeLake, TerrainFeatureType.Lake);
        DrawConfigRow("Mountain", ref _mountainConfig, ref _bakeMountain, TerrainFeatureType.Mountain);
        DrawConfigRow("Forest", ref _forestConfig, ref _bakeForest, TerrainFeatureType.Forest);
    }

    private void DrawConfigRow(
        string label,
        ref TerrainDetectionConfig config,
        ref bool enabled,
        TerrainFeatureType expectedType)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        enabled = EditorGUILayout.ToggleLeft(label, enabled, EditorStyles.boldLabel, GUILayout.Width(100));
        config = (TerrainDetectionConfig)EditorGUILayout.ObjectField(
            config,
            typeof(TerrainDetectionConfig),
            false);
        EditorGUILayout.EndHorizontal();

        if (config != null)
        {
            if (config.FeatureType != expectedType)
            {
                EditorGUILayout.HelpBox(
                    $"Config FeatureType is {config.FeatureType}, expected {expectedType}.",
                    MessageType.Warning);
            }

            EditorGUILayout.LabelField(
                config.HasPalette
                    ? $"Palette samples: {config.Palette.Samples.Length}, minPixels={config.MinRegionPixels}, threshold={config.ColorDistanceThreshold}"
                    : "No palette yet — sample from Reference Texture.");

            using (new EditorGUI.DisabledScope(config.ReferenceTexture == null))
            {
                if (GUILayout.Button($"Sample {label} Palette From Reference"))
                    SampleConfigPalette(config);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSourceSection()
    {
        EditorGUILayout.LabelField("2. Chunk Source", EditorStyles.boldLabel);

        _sourceMode = (BakeSourceMode)EditorGUILayout.EnumPopup("Source Mode", _sourceMode);

        if (_sourceMode == BakeSourceMode.MapChunkDataAssets)
        {
            _mapChunkDataFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "MapChunkData Folder",
                _mapChunkDataFolder,
                typeof(DefaultAsset),
                false);
        }
        else
        {
            _visualTileFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Visual Tile Folder",
                _visualTileFolder,
                typeof(DefaultAsset),
                false);

            _outputDataFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Output MapChunkData Folder",
                _outputDataFolder,
                typeof(DefaultAsset),
                false);

            _columns = EditorGUILayout.IntField("Columns", _columns);
            _rows = EditorGUILayout.IntField("Rows", _rows);
        }
    }

    private void DrawBakeOptions()
    {
        EditorGUILayout.LabelField("3. Bake Options", EditorStyles.boldLabel);

        _skipLockedChunks = EditorGUILayout.Toggle("Skip Locked Chunks", _skipLockedChunks);
        _forceRebake = EditorGUILayout.Toggle("Force Rebake Locked", _forceRebake);
        _preserveLockedOnVisualChange = EditorGUILayout.Toggle(
            "Preserve Locked On Visual Change",
            _preserveLockedOnVisualChange);

        EditorGUILayout.HelpBox(
            "When enabled, changing a chunk Visual updates its texture reference but keeps existing locked feature data. " +
            "Force Rebake Locked always overrides this option.",
            MessageType.Info);

        _clearLegacyGrayscaleMasks = EditorGUILayout.Toggle(
            "Clear Legacy Small/BigLake Masks",
            _clearLegacyGrayscaleMasks);

        _exportPreviewMasks = EditorGUILayout.Toggle("Export Preview Mask PNGs", _exportPreviewMasks);

        using (new EditorGUI.DisabledScope(!_exportPreviewMasks))
        {
            _previewMaskFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Preview Mask Folder",
                _previewMaskFolder,
                typeof(DefaultAsset),
                false);
        }
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("4. Actions", EditorStyles.boldLabel);

        bool canBake =
            (_bakeLake && HasReadyConfig(_lakeConfig)) ||
            (_bakeMountain && HasReadyConfig(_mountainConfig)) ||
            (_bakeForest && HasReadyConfig(_forestConfig));

        using (new EditorGUI.DisabledScope(!canBake))
        {
            if (GUILayout.Button("Bake All Chunks", GUILayout.Height(34)))
                BakeAll();
        }

        if (GUILayout.Button("Create Default Config Assets"))
            CreateDefaultConfigs();

        if (GUILayout.Button("Unlock Selected Types In Folder"))
            UnlockSelectedInFolder();
    }

    private void DrawLog()
    {
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(_lastLog, GUILayout.MinHeight(140));
    }

    private static bool HasReadyConfig(TerrainDetectionConfig config)
    {
        return config != null && config.HasPalette;
    }

    private void SampleConfigPalette(TerrainDetectionConfig config)
    {
        try
        {
            config.ReferenceTexture = EnsureTextureReadyForPixelExactRead(config.ReferenceTexture);
            TerrainColorDetection.SamplePaletteIntoConfig(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            _lastLog =
                $"Sampled {config.Palette.Samples.Length} colors for {config.FeatureType} " +
                $"from '{config.ReferenceTexture.name}' " +
                $"(threshold={config.ColorDistanceThreshold}, radius={config.SampleRadius}).";
        }
        catch (Exception ex)
        {
            _lastLog = "Palette sample failed: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private void BakeAll()
    {
        try
        {
            var targets = CollectTargets();

            if (targets.Count == 0)
            {
                _lastLog = "No chunk targets found.";
                return;
            }

            TerrainDetectionConfig lake = _bakeLake ? _lakeConfig : null;
            TerrainDetectionConfig mountain = _bakeMountain ? _mountainConfig : null;
            TerrainDetectionConfig forest = _bakeForest ? _forestConfig : null;

            if (lake != null && !lake.HasPalette) lake = null;
            if (mountain != null && !mountain.HasPalette) mountain = null;
            if (forest != null && !forest.HasPalette) forest = null;

            if (lake == null && mountain == null && forest == null)
            {
                _lastLog = "Enable at least one config with a sampled palette.";
                return;
            }

            var log = new StringBuilder();
            int baked = 0;
            int skipped = 0;
            int failed = 0;
            int totalRegions = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                BakeTarget target = targets[i];

                EditorUtility.DisplayProgressBar(
                    "Terrain Detection Bake",
                    $"{target.Label} ({i + 1}/{targets.Count})",
                    (i + 1f) / targets.Count);

                try
                {
                    Texture2D visual = target.Visual;

                    if (visual == null)
                    {
                        failed++;
                        log.AppendLine($"FAIL {target.Label}: missing Visual");
                        continue;
                    }

                    MapChunkData data = target.Data;
                    bool visualChanged = data != null && data.Visual != visual;
                    bool anyLocked =
                        data != null &&
                        ((lake != null && data.BakedLakes != null && data.BakedLakes.IsLocked) ||
                         (mountain != null && data.BakedMountains != null && data.BakedMountains.IsLocked) ||
                         (forest != null && data.BakedForests != null && data.BakedForests.IsLocked));

                    visual = EnsureTextureReadyForPixelExactRead(visual);

                    if (anyLocked && !_forceRebake)
                    {
                        if (visualChanged && _preserveLockedOnVisualChange)
                        {
                            data.Visual = visual;
                            EditorUtility.SetDirty(data);
                            skipped++;
                            log.AppendLine(
                                $"KEEP locked features {target.Label}: Visual updated without rebaking");
                            continue;
                        }

                        if (!visualChanged && _skipLockedChunks)
                        {
                            skipped++;
                            log.AppendLine($"SKIP locked {target.Label}");
                            continue;
                        }
                    }

                    // Always run priority chain so claiming stays consistent.
                    // Types without config produce empty unlocked stubs that we ignore when writing.
                    TerrainColorDetection.MultiDetectionResult result =
                        TerrainColorDetection.DetectAndBakeAll(
                            visual,
                            lake,
                            mountain,
                            forest,
                            data != null ? data.BakedLakes : null,
                            data != null ? data.BakedMountains : null,
                            data != null ? data.BakedForests : null);

                    if (data == null)
                        data = CreateOrLoadChunkData(target.OutputAssetPath, target.Coord);

                    data.Visual = visual;

                    if (lake != null)
                    {
                        data.BakedLakes = result.Lake.Data;
                        totalRegions += result.Lake.AcceptedRegionCount;
                    }

                    if (mountain != null)
                    {
                        data.BakedMountains = result.Mountain.Data;
                        totalRegions += result.Mountain.AcceptedRegionCount;
                    }

                    if (forest != null)
                    {
                        data.BakedForests = result.Forest.Data;
                        totalRegions += result.Forest.AcceptedRegionCount;
                    }

                    if (_clearLegacyGrayscaleMasks)
                    {
                        data.SmallLake = null;
                        data.BigLake = null;
                    }

                    EditorUtility.SetDirty(data);

                    if (_exportPreviewMasks && _previewMaskFolder != null)
                    {
                        if (lake != null)
                            ExportPreviewMask(result.Lake.Data, target.Label, "Lake", Color.cyan);
                        if (mountain != null)
                            ExportPreviewMask(result.Mountain.Data, target.Label, "Mountain", new Color(0.55f, 0.35f, 0.2f));
                        if (forest != null)
                            ExportPreviewMask(result.Forest.Data, target.Label, "Forest", Color.green);
                    }

                    baked++;

                    var parts = new List<string>();
                    if (lake != null)
                        parts.Add($"lake={result.Lake.AcceptedRegionCount}");
                    if (mountain != null)
                        parts.Add($"mountain={result.Mountain.AcceptedRegionCount}");
                    if (forest != null)
                        parts.Add($"forest={result.Forest.AcceptedRegionCount}");
                    parts.Add($"claimed={result.ClaimedPixelCount}");

                    log.AppendLine($"OK {target.Label}: {string.Join(", ", parts)}");
                }
                catch (Exception ex)
                {
                    failed++;
                    log.AppendLine($"FAIL {target.Label}: {ex.Message}");
                    Debug.LogException(ex);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.Insert(
                0,
                $"Bake finished. bake={baked}, skipped={skipped}, failed={failed}, regions={totalRegions}\n\n");
            _lastLog = log.ToString();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private void CreateDefaultConfigs()
    {
        const string folder = "Assets/Scripts/World/TerrainDetection/Configs";

        if (!AssetDatabase.IsValidFolder("Assets/Scripts/World/TerrainDetection"))
            AssetDatabase.CreateFolder("Assets/Scripts/World", "TerrainDetection");

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Scripts/World/TerrainDetection", "Configs");

        _lakeConfig = CreateConfigAsset(
            folder,
            "LakeDetectionConfig",
            TerrainFeatureType.Lake,
            minPixels: 64,
            threshold: 28f);

        _mountainConfig = CreateConfigAsset(
            folder,
            "MountainDetectionConfig",
            TerrainFeatureType.Mountain,
            minPixels: 48,
            threshold: 26f);

        _forestConfig = CreateConfigAsset(
            folder,
            "ForestDetectionConfig",
            TerrainFeatureType.Forest,
            minPixels: 48,
            threshold: 26f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _lastLog =
            $"Created/loaded default configs under {folder}.\n" +
            "Assign Reference Texture on each config, then Sample Palette.";
    }

    private static TerrainDetectionConfig CreateConfigAsset(
        string folder,
        string assetName,
        TerrainFeatureType type,
        int minPixels,
        float threshold)
    {
        string path = $"{folder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TerrainDetectionConfig>(path);

        if (existing != null)
            return existing;

        var config = ScriptableObject.CreateInstance<TerrainDetectionConfig>();
        config.FeatureType = type;
        config.GoldenMarkerColor = new Color(1f, 0.843f, 0f, 1f);
        config.GoldenTolerance = 20;
        config.SampleRadius = 15;
        config.ColorDistanceThreshold = threshold;
        config.MinRegionPixels = minPixels;
        config.BigRegionPixelThreshold = Mathf.Max(minPixels, 400);
        config.Palette = new TerrainColorPalette
        {
            GoldenMarkerColor = (Color32)config.GoldenMarkerColor,
            GoldenMarkerTolerance = config.GoldenTolerance,
            SampleRadius = config.SampleRadius,
            ColorDistanceThreshold = threshold
        };

        AssetDatabase.CreateAsset(config, path);
        return config;
    }

    private void UnlockSelectedInFolder()
    {
        string folder = null;

        if (_sourceMode == BakeSourceMode.MapChunkDataAssets && _mapChunkDataFolder != null)
            folder = AssetDatabase.GetAssetPath(_mapChunkDataFolder);
        else if (_outputDataFolder != null)
            folder = AssetDatabase.GetAssetPath(_outputDataFolder);

        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        {
            _lastLog = "Assign a valid MapChunkData / output folder first.";
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:MapChunkData", new[] { folder });
        int unlocked = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<MapChunkData>(path);

            if (data == null)
                continue;

            bool dirty = false;

            if (_bakeLake && data.BakedLakes != null)
            {
                data.BakedLakes.Clear();
                dirty = true;
            }

            if (_bakeMountain && data.BakedMountains != null)
            {
                data.BakedMountains.Clear();
                dirty = true;
            }

            if (_bakeForest && data.BakedForests != null)
            {
                data.BakedForests.Clear();
                dirty = true;
            }

            if (!dirty)
                continue;

            EditorUtility.SetDirty(data);
            unlocked++;
        }

        AssetDatabase.SaveAssets();
        _lastLog = $"Unlocked selected types on {unlocked} MapChunkData assets under {folder}";
    }

    private List<BakeTarget> CollectTargets()
    {
        var list = new List<BakeTarget>();

        if (_sourceMode == BakeSourceMode.MapChunkDataAssets)
        {
            if (_mapChunkDataFolder == null)
                throw new InvalidOperationException("MapChunkData folder is required.");

            string folder = AssetDatabase.GetAssetPath(_mapChunkDataFolder);

            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("MapChunkData folder path is invalid.");

            string[] guids = AssetDatabase.FindAssets("t:MapChunkData", new[] { folder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<MapChunkData>(path);

                if (data == null)
                    continue;

                list.Add(new BakeTarget
                {
                    Label = data.name,
                    Data = data,
                    Visual = data.Visual,
                    OutputAssetPath = path,
                    Coord = ParseCoordFromName(data.name)
                });
            }
        }
        else
        {
            if (_visualTileFolder == null || _outputDataFolder == null)
            {
                throw new InvalidOperationException(
                    "Visual folder and output MapChunkData folder are required.");
            }

            string visualPath = AssetDatabase.GetAssetPath(_visualTileFolder);
            string dataOutPath = AssetDatabase.GetAssetPath(_outputDataFolder);

            if (!AssetDatabase.IsValidFolder(visualPath) ||
                !AssetDatabase.IsValidFolder(dataOutPath))
            {
                throw new InvalidOperationException("Folders must be under Assets.");
            }

            for (int y = 0; y < _rows; y++)
            {
                for (int x = 0; x < _columns; x++)
                {
                    string tileName = $"Tile_y{y}_x{x}";
                    string texPath = FindTileTexture(visualPath, tileName);

                    Texture2D visual = string.IsNullOrEmpty(texPath)
                        ? null
                        : AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

                    string assetPath = $"{dataOutPath}/MapChunkData_y{y}_x{x}.asset";
                    var data = AssetDatabase.LoadAssetAtPath<MapChunkData>(assetPath);

                    list.Add(new BakeTarget
                    {
                        Label = $"y{y}_x{x}",
                        Coord = new Vector2Int(x, y),
                        Visual = visual,
                        Data = data,
                        OutputAssetPath = assetPath
                    });
                }
            }
        }

        return list;
    }

    private static MapChunkData CreateOrLoadChunkData(string assetPath, Vector2Int coord)
    {
        var data = AssetDatabase.LoadAssetAtPath<MapChunkData>(assetPath);

        if (data != null)
            return data;

        data = ScriptableObject.CreateInstance<MapChunkData>();
        AssetDatabase.CreateAsset(data, assetPath);
        return data;
    }

    private void ExportPreviewMask(
        BakedLakeChunkData baked,
        string label,
        string typeName,
        Color color)
    {
        string folder = AssetDatabase.GetAssetPath(_previewMaskFolder);

        if (!AssetDatabase.IsValidFolder(folder) || baked == null || !baked.HasMask)
            return;

        Texture2D preview = TerrainColorDetection.BuildPreviewMask(
            baked,
            color,
            Color.black);

        if (preview == null)
            return;

        string safe = label.Replace('/', '_').Replace('\\', '_');
        string full = Path.Combine(
            Application.dataPath,
            folder.Substring("Assets/".Length),
            $"{typeName}Preview_{safe}.png");

        Directory.CreateDirectory(Path.GetDirectoryName(full) ?? folder);
        File.WriteAllBytes(full, preview.EncodeToPNG());
        DestroyImmediate(preview);
    }

    private static string FindTileTexture(string folder, string tileName)
    {
        string[] guids = AssetDatabase.FindAssets(
            tileName + " t:Texture2D",
            new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);

            if (string.Equals(file, tileName, StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    private static Vector2Int ParseCoordFromName(string name)
    {
        int y = 0;
        int x = 0;

        if (string.IsNullOrEmpty(name))
            return Vector2Int.zero;

        int yi = name.IndexOf("_y", StringComparison.OrdinalIgnoreCase);
        int xi = name.IndexOf("_x", StringComparison.OrdinalIgnoreCase);

        if (yi >= 0 && xi > yi)
        {
            int.TryParse(name.Substring(yi + 2, xi - (yi + 2)), out y);
            int.TryParse(name.Substring(xi + 2), out x);
        }

        return new Vector2Int(x, y);
    }

    private static Texture2D EnsureTextureReadyForPixelExactRead(Texture2D tex)
    {
        if (tex == null)
            throw new ArgumentNullException(nameof(tex));

        string path = AssetDatabase.GetAssetPath(tex);

        if (string.IsNullOrEmpty(path))
        {
            if (!tex.isReadable)
            {
                throw new InvalidOperationException(
                    $"Texture '{tex.name}' is not readable and has no asset path.");
            }

            return tex;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            throw new InvalidOperationException($"Cannot get TextureImporter for '{path}'.");

        bool dirty =
            !importer.isReadable ||
            importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.filterMode != FilterMode.Bilinear;

        if (dirty)
        {
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }

        tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (tex == null || !tex.isReadable)
        {
            throw new InvalidOperationException(
                $"Failed to load readable uncompressed texture: {path}");
        }

        return tex;
    }

    private sealed class BakeTarget
    {
        public string Label;
        public Vector2Int Coord;
        public Texture2D Visual;
        public MapChunkData Data;
        public string OutputAssetPath;
    }
}
#endif
