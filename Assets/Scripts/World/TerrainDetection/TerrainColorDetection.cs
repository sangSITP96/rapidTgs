using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared golden-pixel palette sampling and region bake for Lake / Forest / Mountain.
/// Overlap priority (highest first): Lake &gt; Mountain &gt; Forest.
/// </summary>
public static class TerrainColorDetection
{
    public static readonly TerrainFeatureType[] DefaultPriority =
    {
        TerrainFeatureType.Lake,
        TerrainFeatureType.Mountain,
        TerrainFeatureType.Forest
    };

    public struct DetectionSettings
    {
        public TerrainFeatureType FeatureType;
        public int MinRegionPixels;
        public int BigRegionPixelThreshold;
        public bool ConnectDiagonals;

        public static DetectionSettings DefaultFor(TerrainFeatureType type) => new DetectionSettings
        {
            FeatureType = type,
            MinRegionPixels = 64,
            BigRegionPixelThreshold = 400,
            ConnectDiagonals = false
        };
    }

    public struct DetectionResult
    {
        public BakedLakeChunkData Data;

        public int CandidateRegionCount;
        public int AcceptedRegionCount;
        public int RejectedSmallRegionCount;
        public int PotentialPixelCount;
    }

    public struct MultiDetectionResult
    {
        public DetectionResult Lake;
        public DetectionResult Mountain;
        public DetectionResult Forest;
        public int ClaimedPixelCount;
    }

    public static TerrainColorPalette SamplePaletteFromReference(
        Texture2D reference,
        Color32 goldenMarkerColor,
        int goldenTolerance,
        int sampleRadius,
        float colorDistanceThreshold)
    {
        if (reference == null)
            throw new ArgumentNullException(nameof(reference));

        if (!reference.isReadable)
        {
            throw new InvalidOperationException(
                $"Texture '{reference.name}' is not readable.");
        }

        Color32[] pixels = reference.GetPixels32();
        int width = reference.width;
        int height = reference.height;

        var counts = new Dictionary<int, int>();
        int goldenHits = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 color = pixels[y * width + x];

                if (!TerrainColorPalette.IsNearGolden(
                        color,
                        goldenMarkerColor,
                        goldenTolerance))
                {
                    continue;
                }

                goldenHits++;

                AccumulateAround(
                    pixels,
                    width,
                    height,
                    x,
                    y,
                    sampleRadius,
                    goldenMarkerColor,
                    goldenTolerance,
                    counts);
            }
        }

        if (goldenHits == 0)
        {
            throw new InvalidOperationException(
                "No golden pixels found in reference texture.");
        }

        var samples = new List<TerrainColorSample>(counts.Count);

        foreach (KeyValuePair<int, int> pair in counts)
        {
            samples.Add(new TerrainColorSample(UnpackColor(pair.Key), pair.Value));
        }

        samples.Sort((a, b) => b.Count.CompareTo(a.Count));

        return new TerrainColorPalette
        {
            GoldenMarkerColor = goldenMarkerColor,
            GoldenMarkerTolerance = goldenTolerance,
            SampleRadius = sampleRadius,
            ColorDistanceThreshold = colorDistanceThreshold,
            Samples = samples.ToArray()
        };
    }

    public static void SamplePaletteIntoConfig(TerrainDetectionConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.ReferenceTexture == null)
            throw new InvalidOperationException(
                $"Config '{config.name}' has no Reference Texture.");

        config.ApplyPaletteSettingsToPalette();

        TerrainColorPalette sampled = SamplePaletteFromReference(
            config.ReferenceTexture,
            (Color32)config.GoldenMarkerColor,
            config.GoldenTolerance,
            config.SampleRadius,
            config.ColorDistanceThreshold);

        config.Palette = sampled;
        config.ApplyPaletteSettingsToPalette();
    }

    /// <summary>
    /// Bake Lake, Mountain, Forest in priority order so each pixel belongs to at most one type.
    /// When a config is omitted, existing baked occupancy (if any) is still claimed so lower-priority
    /// types cannot steal those pixels.
    /// </summary>
    public static MultiDetectionResult DetectAndBakeAll(
        Texture2D visual,
        TerrainDetectionConfig lakeConfig,
        TerrainDetectionConfig mountainConfig,
        TerrainDetectionConfig forestConfig,
        BakedLakeChunkData existingLake = null,
        BakedLakeChunkData existingMountain = null,
        BakedLakeChunkData existingForest = null)
    {
        if (visual == null)
            throw new ArgumentNullException(nameof(visual));

        if (!visual.isReadable)
        {
            throw new InvalidOperationException(
                $"Texture '{visual.name}' is not readable.");
        }

        int width = visual.width;
        int height = visual.height;
        Color32[] pixels = visual.GetPixels32();
        var claimed = new bool[width * height];

        var multi = new MultiDetectionResult();

        multi.Lake = ResolvePass(
            pixels,
            width,
            height,
            lakeConfig,
            existingLake,
            TerrainFeatureType.Lake,
            claimed);

        multi.Mountain = ResolvePass(
            pixels,
            width,
            height,
            mountainConfig,
            existingMountain,
            TerrainFeatureType.Mountain,
            claimed);

        multi.Forest = ResolvePass(
            pixels,
            width,
            height,
            forestConfig,
            existingForest,
            TerrainFeatureType.Forest,
            claimed);

        int claimedCount = 0;
        for (int i = 0; i < claimed.Length; i++)
        {
            if (claimed[i])
                claimedCount++;
        }

        multi.ClaimedPixelCount = claimedCount;
        return multi;
    }

    private static DetectionResult ResolvePass(
        Color32[] pixels,
        int width,
        int height,
        TerrainDetectionConfig config,
        BakedLakeChunkData existing,
        TerrainFeatureType type,
        bool[] claimed)
    {
        if (config != null && config.HasPalette)
            return DetectAndBakeOne(pixels, width, height, config, claimed);

        ClaimFromExisting(existing, width, height, claimed);

        return new DetectionResult
        {
            Data = existing ?? new BakedLakeChunkData
            {
                FeatureType = type,
                Regions = new List<BakedLakeRegion>()
            }
        };
    }

    private static void ClaimFromExisting(
        BakedLakeChunkData existing,
        int width,
        int height,
        bool[] claimed)
    {
        if (existing == null || !existing.HasMask || existing.Regions == null)
            return;

        if (existing.TextureWidth != width || existing.TextureHeight != height)
            return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (existing.IsBlockedPixel(x, y))
                    claimed[y * width + x] = true;
            }
        }
    }

    public static DetectionResult DetectAndBake(
        Texture2D visual,
        TerrainColorPalette palette,
        DetectionSettings settings,
        bool[] claimedPixels = null)
    {
        if (visual == null)
            throw new ArgumentNullException(nameof(visual));

        if (palette == null || !palette.HasSamples)
        {
            throw new InvalidOperationException(
                "Terrain color palette has no samples. Sample a reference first.");
        }

        if (!visual.isReadable)
        {
            throw new InvalidOperationException(
                $"Texture '{visual.name}' is not readable.");
        }

        Color32[] pixels = visual.GetPixels32();
        return DetectAndBakeCore(
            pixels,
            visual.width,
            visual.height,
            palette,
            settings,
            claimedPixels);
    }

    private static DetectionResult DetectAndBakeOne(
        Color32[] pixels,
        int width,
        int height,
        TerrainDetectionConfig config,
        bool[] claimed)
    {
        config.ApplyPaletteSettingsToPalette();
        return DetectAndBakeCore(
            pixels,
            width,
            height,
            config.Palette,
            config.ToDetectionSettings(),
            claimed);
    }

    private static DetectionResult DetectAndBakeCore(
        Color32[] pixels,
        int width,
        int height,
        TerrainColorPalette palette,
        DetectionSettings settings,
        bool[] claimedPixels)
    {
        bool[] potential = new bool[width * height];
        int potentialCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (claimedPixels != null && claimedPixels[i])
                continue;

            Color32 color = pixels[i];

            if (TerrainColorPalette.IsNearGolden(
                    color,
                    palette.GoldenMarkerColor,
                    palette.GoldenMarkerTolerance))
            {
                continue;
            }

            if (palette.Matches(color))
            {
                potential[i] = true;
                potentialCount++;
            }
        }

        bool[] visited = new bool[width * height];
        var acceptedRegions = new List<BakedLakeRegion>();

        int candidateCount = 0;
        int rejectedSmallCount = 0;
        int regionId = 0;

        int minPixels = Mathf.Max(1, settings.MinRegionPixels);
        int bigThreshold = Mathf.Max(minPixels, settings.BigRegionPixelThreshold);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int startIndex = y * width + x;

                if (!potential[startIndex] || visited[startIndex])
                    continue;

                candidateCount++;

                List<int> regionPixels = FloodFill(
                    potential,
                    visited,
                    width,
                    height,
                    x,
                    y,
                    settings.ConnectDiagonals);

                if (regionPixels.Count < minPixels)
                {
                    rejectedSmallCount++;
                    continue;
                }

                if (claimedPixels != null)
                {
                    for (int i = 0; i < regionPixels.Count; i++)
                        claimedPixels[regionPixels[i]] = true;
                }

                int minX = int.MaxValue;
                int minY = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                long sumX = 0;
                long sumY = 0;

                for (int i = 0; i < regionPixels.Count; i++)
                {
                    int index = regionPixels[i];
                    int pixelX = index % width;
                    int pixelY = index / width;

                    sumX += pixelX;
                    sumY += pixelY;

                    if (pixelX < minX) minX = pixelX;
                    if (pixelY < minY) minY = pixelY;
                    if (pixelX > maxX) maxX = pixelX;
                    if (pixelY > maxY) maxY = pixelY;
                }

                float centerU = (sumX / (float)regionPixels.Count + 0.5f) / width;
                float centerV = (sumY / (float)regionPixels.Count + 0.5f) / height;

                var bounds = new RectInt(
                    minX,
                    minY,
                    maxX - minX + 1,
                    maxY - minY + 1);

                bool isBig = regionPixels.Count >= bigThreshold;

                acceptedRegions.Add(new BakedLakeRegion(
                    regionId++,
                    regionPixels.Count,
                    bounds,
                    new Vector2(centerU, centerV),
                    isBig,
                    BuildRowSpans(regionPixels, width),
                    TraceOuterPerimeterOrderedUVs(regionPixels, width, height)));
            }
        }

        var bakedData = new BakedLakeChunkData
        {
            IsLocked = true,
            FeatureType = settings.FeatureType,
            TextureWidth = width,
            TextureHeight = height,
            BakedUtcTicks = DateTime.UtcNow.Ticks,
            Regions = acceptedRegions
        };

        return new DetectionResult
        {
            Data = bakedData,
            CandidateRegionCount = candidateCount,
            AcceptedRegionCount = acceptedRegions.Count,
            RejectedSmallRegionCount = rejectedSmallCount,
            PotentialPixelCount = potentialCount
        };
    }

    public static Texture2D BuildPreviewMask(
        BakedLakeChunkData data,
        Color featureColor,
        Color emptyColor)
    {
        if (data == null || !data.HasMask)
            return null;

        var tex = new Texture2D(
            data.TextureWidth,
            data.TextureHeight,
            TextureFormat.RGBA32,
            false);

        var colors = new Color32[data.TextureWidth * data.TextureHeight];
        Color32 feature = featureColor;
        Color32 empty = emptyColor;

        for (int y = 0; y < data.TextureHeight; y++)
        {
            for (int x = 0; x < data.TextureWidth; x++)
            {
                int index = y * data.TextureWidth + x;
                colors[index] = data.IsBlockedPixel(x, y) ? feature : empty;
            }
        }

        tex.SetPixels32(colors);
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;
    }

    /// <summary>
    /// Moore-neighborhood outer contour (Jacob's stopping criterion), ordered clockwise, as UV centers.
    /// Consumers treat the polyline as a closed loop (last connects back to first).
    /// </summary>
    public static List<Vector2> TraceOuterPerimeterOrderedUVs(
        List<int> regionPixels,
        int width,
        int height)
    {
        var perimeter = new List<Vector2>();

        if (regionPixels == null || regionPixels.Count == 0 || width <= 0 || height <= 0)
            return perimeter;

        var inside = new HashSet<int>(regionPixels);
        var boundary = new HashSet<int>();

        int startX = int.MaxValue;
        int startY = int.MaxValue;

        for (int i = 0; i < regionPixels.Count; i++)
        {
            int index = regionPixels[i];
            int x = index % width;
            int y = index / width;

            if (!IsBoundaryPixel(inside, width, height, x, y))
                continue;

            boundary.Add(index);

            if (y < startY || (y == startY && x < startX))
            {
                startX = x;
                startY = y;
            }
        }

        if (boundary.Count == 0)
            return perimeter;

        // Clockwise from East: E, NE, N, NW, W, SW, S, SE.
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

        int cx = startX;
        int cy = startY;
        // Arrive from the west (outside / non-boundary).
        int bx = startX - 1;
        int by = startY;
        const int maxSteps = 1000000;

        for (int step = 0; step < maxSteps; step++)
        {
            perimeter.Add(PixelCenterUV(cx, cy, width, height));

            int backDir = NeighborDirection(cx, cy, bx, by, dx, dy);
            int searchStart = (backDir + 1) % 8;

            int nextX = cx;
            int nextY = cy;
            int newBx = bx;
            int newBy = by;
            bool moved = false;

            for (int i = 0; i < 8; i++)
            {
                int d = (searchStart + i) % 8;
                int nx = cx + dx[d];
                int ny = cy + dy[d];

                if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                {
                    newBx = nx;
                    newBy = ny;
                    continue;
                }

                int nIndex = ny * width + nx;

                if (!boundary.Contains(nIndex))
                {
                    newBx = nx;
                    newBy = ny;
                    continue;
                }

                nextX = nx;
                nextY = ny;
                // Backtrack point is the previous non-boundary neighbor inspected.
                moved = true;
                break;
            }

            if (!moved)
                break;

            bx = newBx;
            by = newBy;
            cx = nextX;
            cy = nextY;

            if (cx == startX && cy == startY && perimeter.Count > 1)
                break;

            if (perimeter.Count > boundary.Count * 2 + 8)
                break;
        }

        return DeduplicateConsecutive(perimeter);
    }

    private static int NeighborDirection(
        int fromX,
        int fromY,
        int toX,
        int toY,
        int[] dx,
        int[] dy)
    {
        int ox = toX - fromX;
        int oy = toY - fromY;

        for (int d = 0; d < 8; d++)
        {
            if (dx[d] == ox && dy[d] == oy)
                return d;
        }

        return 4; // default West
    }

    private static List<Vector2> DeduplicateConsecutive(List<Vector2> points)
    {
        if (points.Count <= 1)
            return points;

        var result = new List<Vector2>(points.Count) { points[0] };

        for (int i = 1; i < points.Count; i++)
        {
            if ((points[i] - result[result.Count - 1]).sqrMagnitude > 1e-12f)
                result.Add(points[i]);
        }

        if (result.Count > 1 &&
            (result[0] - result[result.Count - 1]).sqrMagnitude <= 1e-12f)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result;
    }

    private static Vector2 PixelCenterUV(int x, int y, int width, int height)
    {
        return new Vector2(
            (x + 0.5f) / width,
            (y + 0.5f) / height);
    }

    private static bool IsInside(HashSet<int> inside, int width, int height, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return false;

        return inside.Contains(y * width + x);
    }

    private static bool IsBoundaryPixel(HashSet<int> inside, int width, int height, int x, int y)
    {
        if (!IsInside(inside, width, height, x, y))
            return false;

        return Is4NeighborOutside(inside, width, height, x, y);
    }

    private static bool Is4NeighborOutside(HashSet<int> inside, int width, int height, int x, int y)
    {
        return !IsInside(inside, width, height, x + 1, y) ||
               !IsInside(inside, width, height, x - 1, y) ||
               !IsInside(inside, width, height, x, y + 1) ||
               !IsInside(inside, width, height, x, y - 1);
    }

    private static List<BakedLakeRowSpan> BuildRowSpans(List<int> regionPixels, int width)
    {
        regionPixels.Sort();

        var spans = new List<BakedLakeRowSpan>();

        if (regionPixels.Count == 0)
            return spans;

        int currentY = regionPixels[0] / width;
        int spanStartX = regionPixels[0] % width;
        int previousX = spanStartX;

        for (int i = 1; i < regionPixels.Count; i++)
        {
            int index = regionPixels[i];
            int x = index % width;
            int y = index / width;

            if (y == currentY && x == previousX + 1)
            {
                previousX = x;
                continue;
            }

            spans.Add(new BakedLakeRowSpan(currentY, spanStartX, previousX));
            currentY = y;
            spanStartX = x;
            previousX = x;
        }

        spans.Add(new BakedLakeRowSpan(currentY, spanStartX, previousX));
        return spans;
    }

    private static void AccumulateAround(
        Color32[] pixels,
        int width,
        int height,
        int centerX,
        int centerY,
        int radius,
        Color32 golden,
        int goldenTolerance,
        Dictionary<int, int> counts)
    {
        int radiusSquared = radius * radius;

        int minX = Mathf.Max(0, centerX - radius);
        int maxX = Mathf.Min(width - 1, centerX + radius);
        int minY = Mathf.Max(0, centerY - radius);
        int maxY = Mathf.Min(height - 1, centerY + radius);

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - centerY;

            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - centerX;

                if (dx * dx + dy * dy > radiusSquared)
                    continue;

                Color32 color = pixels[y * width + x];

                if (TerrainColorPalette.IsNearGolden(color, golden, goldenTolerance))
                    continue;

                int key = PackColor(color);
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }
        }
    }

    private static List<int> FloodFill(
        bool[] potential,
        bool[] visited,
        int width,
        int height,
        int startX,
        int startY,
        bool diagonals)
    {
        var result = new List<int>();
        var queue = new Queue<int>();

        int startIndex = startY * width + startX;
        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        int[] dx4 = { 1, -1, 0, 0 };
        int[] dy4 = { 0, 0, 1, -1 };
        int[] dx8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy8 = { 0, 0, 1, -1, 1, -1, 1, -1 };

        int[] dx = diagonals ? dx8 : dx4;
        int[] dy = diagonals ? dy8 : dy4;

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            result.Add(index);

            int x = index % width;
            int y = index / width;

            for (int i = 0; i < dx.Length; i++)
            {
                int nextX = x + dx[i];
                int nextY = y + dy[i];

                if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                    continue;

                int nextIndex = nextY * width + nextX;

                if (visited[nextIndex] || !potential[nextIndex])
                    continue;

                visited[nextIndex] = true;
                queue.Enqueue(nextIndex);
            }
        }

        return result;
    }

    private static int PackColor(Color32 color)
    {
        return (color.r << 16) | (color.g << 8) | color.b;
    }

    private static Color32 UnpackColor(int key)
    {
        return new Color32(
            (byte)((key >> 16) & 0xFF),
            (byte)((key >> 8) & 0xFF),
            (byte)(key & 0xFF),
            255);
    }
}
