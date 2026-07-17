using System.Collections.Generic;
using UnityEngine;

public static class LakeColorDetection
{
    public struct DetectionSettings
    {
        public int MinLakePixels;
        public int BigLakePixelThreshold;
        public bool ConnectDiagonals;

        public static DetectionSettings Default => new DetectionSettings
        {
            MinLakePixels = 64,
            BigLakePixelThreshold = 400,
            ConnectDiagonals = false
        };
    }

    public struct DetectionResult
    {
        public BakedLakeChunkData Data;

        public int CandidateRegionCount;
        public int AcceptedRegionCount;
        public int RejectedSmallRegionCount;
        public int PotentialLakePixelCount;
    }

    public static LakeColorPalette SamplePaletteFromReference(
        Texture2D reference,
        Color32 goldenMarkerColor,
        int goldenTolerance,
        int sampleRadius,
        float colorDistanceThreshold)
    {
        if (reference == null)
            throw new System.ArgumentNullException(nameof(reference));

        if (!reference.isReadable)
        {
            throw new System.InvalidOperationException(
                $"Texture '{reference.name}' is not readable.");
        }

        Color32[] pixels = reference.GetPixels32();

        int width = reference.width;
        int height = reference.height;

        Dictionary<int, int> counts = new Dictionary<int, int>();
        int goldenHits = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 color = pixels[y * width + x];

                if (!LakeColorPalette.IsNearGolden(
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
            FindClosestMarkerHint(
                pixels,
                width,
                height,
                goldenMarkerColor,
                out Color32 closest,
                out int distance,
                out int closestX,
                out int closestY
                );

            throw new System.InvalidOperationException("No golden pixels found in reference lake.");
        }

        List<LakeColorSample> samples = new List<LakeColorSample>(counts.Count);

        foreach (KeyValuePair<int, int> pair in counts)
        {
            Color32 color = UnpackColor(pair.Key);
            samples.Add(new LakeColorSample(color, pair.Value));
        }

        samples.Sort((a, b) => b.Count.CompareTo(a.Count));

        return new LakeColorPalette
        {
            GoldenMarkerColor = goldenMarkerColor,
            GoldenMarkerTolerance = goldenTolerance,
            SampleRadius = sampleRadius,
            ColorDistanceThreshold = colorDistanceThreshold,
            Samples = samples.ToArray()
        };
    }

    public static DetectionResult DetectAndBake(
        Texture2D visual,
        LakeColorPalette palette,
        DetectionSettings settings)
    {
        if (visual == null)
            throw new System.ArgumentNullException(nameof(visual));

        if (palette == null || !palette.HasSamples)
        {
            throw new System.InvalidOperationException("Lake color palette has no sample. Sameple a reference lake first.");
        }

        if (!visual.isReadable)
        {
            throw new System.InvalidOperationException($"Texture '{visual.name}' is not readable.");
        }

        int width = visual.width;
        int height = visual.height;

        Color32[] pixels = visual.GetPixels32();

        bool[] potential = new bool[width * height];
        var potentialCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 color = pixels[i];

            if (LakeColorPalette.IsNearGolden(
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

        List<BakedLakeRegion> acceptedRegions = new List<BakedLakeRegion>();

        var candidateCount = 0;
        var rejectedSmallCount = 0;
        var regionId = 0;

        var minPixels = Mathf.Max(1, settings.MinLakePixels);
        var bigThreshold = Mathf.Max(minPixels, settings.BigLakePixelThreshold);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var startIndex = y * width + x;

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

                var minX = int.MaxValue;
                var minY = int.MaxValue;
                var maxX = int.MinValue;
                var maxY = int.MinValue;

                long sumX = 0;
                long sumY = 0;

                for (int i = 0; i < regionPixels.Count; i++)
                {
                    var index = regionPixels[i];

                    var pixelX = index % width;
                    var pixelY = index / width;

                    sumX += pixelX;
                    sumY += pixelY;

                    if(pixelX < minX)
                        minX = pixelX;

                    if(pixelY < minY)
                        minY = pixelY;

                    if(pixelX > maxX)
                        maxX = pixelX;

                    if (pixelY > maxY)
                        maxY = pixelY;
                }

                float centerU = (sumX / (float)regionPixels.Count + 0.5f) / width;
                float centerV = (sumY / (float)regionPixels.Count + 0.5f) / height;

                RectInt bounds = new RectInt(
                    minX,
                    minY,
                    maxX - minX + 1,
                    maxY - minY + 1);

                var isBig = regionPixels.Count >= bigThreshold;

                acceptedRegions.Add(new BakedLakeRegion(
                    regionId++,
                    regionPixels.Count,
                    bounds,
                    new Vector2(centerU, centerV),
                    isBig,
                    BuildRowSpans(regionPixels, width)));
            }
        }

        BakedLakeChunkData bakedData = new BakedLakeChunkData
        {
            IsLocked = true,
            TextureWidth = width,
            TextureHeight = height,
            BakedUtcTicks = System.DateTime.UtcNow.Ticks,
            Regions = acceptedRegions
        };

        return new DetectionResult
        {
            Data = bakedData,
            CandidateRegionCount = candidateCount,
            AcceptedRegionCount = acceptedRegions.Count,
            RejectedSmallRegionCount = rejectedSmallCount,
            PotentialLakePixelCount = potentialCount 
        };
    }

    public static Texture2D BuildPreviewMask(
        BakedLakeChunkData data,
        Color lakeColor,
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

        Color32 lake = lakeColor;
        Color32 empty = emptyColor;

        for (int y = 0; y < data.TextureHeight; y++)
        {
            for (int x = 0; x < data.TextureWidth; x++)
            {
                int index = y * data.TextureWidth + x;
                colors[index] = data.IsBlockedPixel(x, y) ? lake : empty;
            }
        }

        tex.SetPixels32(colors);
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        return tex;
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
            var dy = y - centerY;

            for (int x = minX; x <= maxX; x++)
            {
                var dx = x - centerX;

                if (dx * dx + dy * dy > radiusSquared)
                    continue;

                Color32 color = pixels[y * width + x];

                if (LakeColorPalette.IsNearGolden(color, golden, goldenTolerance))
                    continue;

                var key = PackColor(color);

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

        var startIndex = startY * width + startX;

        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        int[] dx4 = { 1, -1, 0, 0 };
        int[] dy4 = { 0, 0, 1, -1 };

        int[] dx8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy8 = { 0, 0, 1, -1, 1, -1, 1, -1 };

        int[]dx = diagonals ? dx8 : dx4;
        int[]dy = diagonals ? dy8 : dy4;

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();

            result.Add(index);

            var x = index % width;
            var y = index / width;

            for (int i = 0; i < dx.Length; i++)
            {
                var nextX = x + dx[i];
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

    private static void FindClosestMarkerHint(
        Color32[] pixels,
        int width,
        int height,
        Color32 target,
        out Color32 closest,
        out int distance,
        out int closestX,
        out int closestY)
    {
        closest = pixels.Length > 0 ? pixels[0] : default;
        distance = int.MaxValue;
        closestX = 0;
        closestY = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = pixels[y * width + x];

                bool goldish =
                    color.r >= 180 &&
                    color.g >= 120 &&
                    color.b >= 120 &&
                    color.r + color.g > color.b * 3;

                var rawDistance = LakeColorPalette.ColorDistance(color, target);
                var roundedDistance = Mathf.RoundToInt(rawDistance);

                if (goldish && roundedDistance < distance)
                {
                    distance = roundedDistance;
                    closest = color;
                    closestX = x;
                    closestY = y;
                }
            }
        }

        if (distance != int.MaxValue)
            return;

        for (int i = 0; i < pixels.Length; i++)
        {
            var rawDistance = LakeColorPalette.ColorDistance(pixels[i], target);
            var roundedDistance = Mathf.RoundToInt(rawDistance);

            if (roundedDistance < distance)
            {
                distance = roundedDistance;
                closest = pixels[i];
                closestX = i % width;
                closestY = i / width;
            }
        }
    }

    private static int PackColor(Color32 color)
    {
        return (color.r << 16)| (color.g << 8) | color.b;
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
