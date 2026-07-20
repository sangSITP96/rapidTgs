#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class LakeDetectionBakeWindow : EditorWindow
{
    private enum BakeSourceMode
    {
        MapChunkDataAssets,
        VisualTileFolder
    }

    [SerializeField] private Texture2D _referenceLake;
    [SerializeField] private Color _goldenMarkerColor = new Color(1f, 0.843f, 0f, 1f);
    [SerializeField] private int _goldenTolerance = 20;
    [SerializeField] private int _sampleRadius = 15;
    [SerializeField] private float _colorDistanceThreshold = 28f;

    [SerializeField] private LakeColorPalette _palette;

    [SerializeField] private BakeSourceMode _sourceMode = BakeSourceMode.MapChunkDataAssets;
    [SerializeField] private DefaultAsset _mapChunkDataFolder;
    [SerializeField] private DefaultAsset _visualTileFolder;
    [SerializeField] private DefaultAsset _outputDataFolder;
    [SerializeField] private int _columns = 3;
    [SerializeField] private int _rows = 4;

    [SerializeField] private int _minLakePixels = 64;
    [SerializeField] private int _bigLakePixelThreshold = 400;
    [SerializeField] private bool _connectDiagonals;
    [SerializeField] private bool _skipLockedChunks = true;
    [SerializeField] private bool _forceRebake;
    [SerializeField] private bool _preserveLockedLakesOnVisualChange = true;
    [SerializeField] private bool _exportPreviewMasks;
    [SerializeField] private DefaultAsset _previewMaskFolder;
    [SerializeField] private bool _clearLegacyGrayscaleMasks = true;

    private Vector2 _scroll;
    private string _lastLog = string.Empty;
    private Texture2D _palettePreview;

    [MenuItem("Tools/Map/Lake Detection & Collider Bake")]
    public static void ShowWindow()
    {
        var window = GetWindow<LakeDetectionBakeWindow>("Lake Bake Tool");
        window.minSize = new Vector2(420, 560);
    }

    private void OnDisable()
    {
        DestroyPreview(_palettePreview);
        _palettePreview = null;
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawReferenceSection();

        EditorGUILayout.Space(8);

        DrawPaletteSection();

        EditorGUILayout.Space(8);
        DrawSourceSection();

        EditorGUILayout.Space(8);

        DrawDetectionSection();

        EditorGUILayout.Space(8);
        DrawActions();

        EditorGUILayout.Space(8);
        DrawLog();

        EditorGUILayout.EndScrollView();
    }

    private void DrawReferenceSection()
    {
        EditorGUILayout.LabelField("1. Reference Lake / Golden Pixel", EditorStyles.boldLabel);

        _referenceLake = (Texture2D)EditorGUILayout.ObjectField(
            "Reference Lake",
            _referenceLake,
            typeof(Texture2D),
            false);

        _goldenMarkerColor = EditorGUILayout.ColorField("Golden Marker", _goldenMarkerColor);
        _goldenTolerance = EditorGUILayout.IntSlider("Golden Tolerance", _goldenTolerance, 0, 60);
        _sampleRadius = EditorGUILayout.IntSlider("SAmple Radius (px)", _sampleRadius, 1, 64);
        _colorDistanceThreshold = EditorGUILayout.Slider("Match Threshold", _colorDistanceThreshold, 1f, 80f);

        using (new EditorGUI.DisabledScope(_referenceLake == null))
        {
            if (GUILayout.Button("Sample Lake Color Palette", GUILayout.Height(28)))
                SamplePalette();
        }
    }

    private void DrawPaletteSection()
    {
        EditorGUILayout.LabelField("Lake Color Palette", EditorStyles.boldLabel);

        if (_palette == null || !_palette.HasSamples)
        {
            EditorGUILayout.HelpBox("No palette yet. Sample from reference lake first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Samples: {_palette.Samples.Length}");

        if (_palettePreview != null)
        {
            Rect r = GUILayoutUtility.GetRect(256, 48, GUILayout.ExpandWidth(true));
            EditorGUI.DrawPreviewTexture(r, _palettePreview, null, ScaleMode.StretchToFill);
        }

        int show = Mathf.Min(8, _palette.Samples.Length);

        for (int i = 0; i < show; i++)
        {
            var s = _palette.Samples[i];

            Rect swatch = EditorGUILayout.GetControlRect(false, 18);

            EditorGUI.DrawRect(
                new Rect(swatch.x, swatch.y, 18, 18),
                s.Color);

            EditorGUI.LabelField(
                new Rect(swatch.x + 24, swatch.y, swatch.width - 24, 18),
                $"RGB({s.Color.r},{s.Color.g},{s.Color.b}) x{s.Color}");
        }

        if (_palette.Samples.Length > show)
            EditorGUILayout.LabelField($"... and {_palette.Samples.Length - show} more");
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

            EditorGUILayout.HelpBox("Scans Visual on each MapChunkData asset", MessageType.None);
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
                typeof(DefaultAsset), false);

            _columns = EditorGUILayout.IntField("Columns", _columns);
            _rows = EditorGUILayout.IntField("Rows", _rows);
        }
    }

    private void DrawDetectionSection()
    {
        EditorGUILayout.LabelField("3. Detection & Bake Settings", EditorStyles.boldLabel);

        _minLakePixels = EditorGUILayout.IntField("Min Lake Pixels", _minLakePixels);
        _bigLakePixelThreshold = EditorGUILayout.IntField("Big Lake Threshold", _bigLakePixelThreshold);
        _connectDiagonals = EditorGUILayout.Toggle("8-Connected Regions", _connectDiagonals);
        _skipLockedChunks = EditorGUILayout.Toggle("Skip Locked Chunks", _skipLockedChunks);
        _forceRebake = EditorGUILayout.Toggle("Force Rebake Locked", _forceRebake);
        _preserveLockedLakesOnVisualChange = EditorGUILayout.Toggle(
            "Preserve Locked Lakes On Visual Change",
            _preserveLockedLakesOnVisualChange);

        EditorGUILayout.HelpBox(
            "When enabled, changing a chunk Visual updates its texture reference but keeps existing locked lake data. " +
            "This prevents armies, units, props, or VFX with lake-like colors from becoming lakes. " +
            "Force Rebake Locked always overrides this option.",
            MessageType.Info);

        _clearLegacyGrayscaleMasks = EditorGUILayout.Toggle("Clear Legacy Small/BigLake Masks", _clearLegacyGrayscaleMasks);
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

        using (new EditorGUI.DisabledScope(_palette == null || !_palette.HasSamples))
        {
            if (GUILayout.Button("Bake All Chunks", GUILayout.Height(34)))
                BakeAll();
        }

        if (GUILayout.Button("Unlock All Chunks In Folder"))
            UnlockAllInFolder();
    }

    private void DrawLog()
    {
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(_lastLog, GUILayout.MinHeight(120));
    }

    private void SamplePalette()
    {
        try
        {
            _referenceLake = EnsureTextureReadyForPixelExactRead(_referenceLake);
            Color32 golden = (Color32)_goldenMarkerColor;

            _palette = LakeColorDetection.SamplePaletteFromReference(
                _referenceLake,
                golden,
                _goldenTolerance,
                _sampleRadius,
                _colorDistanceThreshold);

            RebuildPalettePreview();

            _lastLog =
                $"Sampled {_palette.Samples.Length} lake colors from '{_referenceLake.name}'" +
                $"(golden=RGB{golden.r}, {golden.g}, {golden.b}), radius = {_sampleRadius}, " +
                $"threshold={_colorDistanceThreshold}).";
        }
        catch (Exception ex)
        {
            _lastLog = "Palette sample failed: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    private void RebuildPalettePreview()
    {
        DestroyPreview(_palettePreview);
        _palettePreview = null;

        if (_palette == null || !_palette.HasSamples)
            return;

        var n = Mathf.Min(32, _palette.Samples.Length);

        _palettePreview = new Texture2D(n, 1, TextureFormat.RGBA32, false);

        var cols = new Color32[n];

        for (int i = 0; i < n; i++)
            cols[i] = _palette.Samples[i].Color;

        _palettePreview.SetPixels32(cols);
        _palettePreview.Apply(false, false);
        _palettePreview.filterMode = FilterMode.Point;
        _palettePreview.wrapMode = TextureWrapMode.Clamp;
    }

    private void BakeAll()
    {
        if (_palette == null || !_palette.HasSamples)
        {
            _lastLog = "Sample a palette first.";
            return;
        }

        try
        {
            var settings = new LakeColorDetection.DetectionSettings
            {
                MinLakePixels = Mathf.Max(1, _minLakePixels),
                BigLakePixelThreshold = Mathf.Max(1, _bigLakePixelThreshold),
                ConnectDiagonals = _connectDiagonals
            };

            var targets = CollectTargets();

            if (targets.Count == 0)
            {
                _lastLog = "No chunk targets found.";
                return;
            }

            var log = new StringBuilder();

            var baked = 0;
            var skipped = 0;
            var failed = 0;
            var totalRegions = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                EditorUtility.DisplayProgressBar(
                    "Lake Detection Bake",
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
                    bool lakeDataLocked =
                        data != null &&
                        data.BakedLakes != null &&
                        data.BakedLakes.IsLocked;

                    visual = EnsureTextureReadyForPixelExactRead(visual);

                    if (lakeDataLocked && !_forceRebake)
                    {
                        if (visualChanged && _preserveLockedLakesOnVisualChange)
                        {
                            data.Visual = visual;
                            EditorUtility.SetDirty(data);
                            skipped++;
                            log.AppendLine(
                                $"KEEP locked lakes {target.Label}: Visual updated without rebaking lake data");
                            continue;
                        }

                        if (!visualChanged && _skipLockedChunks)
                        {
                            skipped++;
                            log.AppendLine($"SKIP locked {target.Label}");
                            continue;
                        }
                    }

                    var result = LakeColorDetection.DetectAndBake(
                        visual,
                        _palette,
                        settings);

                    if (data == null)
                        data = CreateOrLoadChunkData(target.OutputAssetPath, target.Coord);

                    // Always update the asset reference when a bake is performed.
                    data.Visual = visual;
                    data.BakedLakes = result.Data;

                    if (_clearLegacyGrayscaleMasks)
                    {
                        data.SmallLake = null;
                        data.BigLake = null;
                    }

                    EditorUtility.SetDirty(data);

                    if (_exportPreviewMasks && _previewMaskFolder != null)
                    {
                        ExportPreviewMask(result.Data, target.Label);
                    }

                    baked++;
                    totalRegions += result.AcceptedRegionCount;

                    log.AppendLine(
                        $"OK {target.Label}: lakes={result.AcceptedRegionCount}, " +
                        $"pixels={result.PotentialLakePixelCount}, rejectedSmall={result.RejectedSmallRegionCount}");
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

    private void UnlockAllInFolder()
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

        var unlocked = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<MapChunkData>(path);

            if (data == null || data.BakedLakes == null)
                continue;

            data.BakedLakes.Clear();

            EditorUtility.SetDirty(data);
            unlocked++;
        }

        AssetDatabase.SaveAssets();

        _lastLog = $"Unlocked {unlocked} MapChunkData assets under {folder}";
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

    private void ExportPreviewMask(BakedLakeChunkData baked, string label)
    {
        string folder = AssetDatabase.GetAssetPath(_previewMaskFolder);

        if (!AssetDatabase.IsValidFolder(folder))
            return;

        Texture2D preview = LakeColorDetection.BuildPreviewMask(
            baked,
            new Color(1f, 1f, 1f, 1f),
            new Color(0f, 0f, 0f, 1f));

        if (preview == null)
            return;

        string safe = label.Replace('/', '_').Replace('\\', '_');

        string full = Path.Combine(
            Application.dataPath,
            folder.Substring("Assets/".Length),
            $"LakePreview_{safe}.png"
            );

        Directory.CreateDirectory(Path.GetDirectoryName(full) ?? folder);
        File.WriteAllBytes(full, preview.EncodeToPNG());

        DestroyPreview(preview);
    }

    private static string FindTileTexture(string folder, string tileName)
    {
        string[] guids = AssetDatabase.FindAssets(
            tileName + " t:Texture2D",
            new[] { folder});

        foreach (string guid in guids) 
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);

            if(string.Equals(file, tileName, StringComparison.OrdinalIgnoreCase))
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

        if(yi >= 0 && xi > yi)
        {
            int.TryParse(name.Substring(yi + 2, xi - (yi + 2)), out y);
            int.TryParse(name.Substring(xi + 2), out x);
        }

        return new Vector2Int(x, y);
    }

    private static Texture2D EnsureTextureReadyForPixelExactRead(Texture2D tex)
    {
        if(tex == null)
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

        var dirty =
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
                $"Failed to load readable uncmpressed texture: {path}");
        }

        return tex;
    }

    private static void DestroyPreview(Texture2D tex)
    {
        if (tex == null)
            return;

        DestroyImmediate(tex);
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