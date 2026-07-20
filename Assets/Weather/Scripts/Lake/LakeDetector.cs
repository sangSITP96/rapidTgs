using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather.Lake
{
    /// <summary>
    /// Exposes lakes from baked MapChunkData for weather systems.
    /// No texture, grayscale mask, color, collider, or TGS scan runs at runtime.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class LakeDetector : MonoBehaviour
    {
        [SerializeField] private InfiniteMapStreamer _mapStreamer;

        [Header("Baked Chunk Data")]
        [SerializeField] private bool _rescanWhenChunksChange = true;
        [SerializeField] private int _minimumLakePixels = 1;
        [Tooltip("Normalizes baked pixel area to the units used by existing Cloud/Fog thresholds.")]
        [SerializeField, Min(8)] private int _weatherSizeReferenceResolution = 48;
        [SerializeField] private bool _logDetectedLakes;

        private readonly List<Lake> _lakes = new();
        public IReadOnlyList<Lake> Lakes => _lakes;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_mapStreamer != null)
                _mapStreamer.LoadedChunksChanged += HandleLoadedChunksChanged;
        }

        private void OnDisable()
        {
            if (_mapStreamer != null)
                _mapStreamer.LoadedChunksChanged -= HandleLoadedChunksChanged;
        }

        private void Start()
        {
            DetectLakes();
        }

        private void ResolveReferences()
        {
            if (_mapStreamer == null)
                _mapStreamer = FindFirstObjectByType<InfiniteMapStreamer>();
        }

        private void HandleLoadedChunksChanged()
        {
            if (_rescanWhenChunksChange)
                DetectLakes();
        }

        public void DetectLakes()
        {
            ResolveReferences();
            _lakes.Clear();

            if (_mapStreamer == null)
            {
                Debug.LogError("LakeDetector: InfiniteMapStreamer is required.");
                return;
            }

            foreach (Vector2Int coord in _mapStreamer.LoadedChunkCoords)
            {
                MapChunkData chunkData = _mapStreamer.GetChunkData(coord);

                if (chunkData == null ||
                    chunkData.BakedLakes == null ||
                    !chunkData.BakedLakes.IsLocked)
                {
                    continue;
                }

                if (!_mapStreamer.TryGetChunkWorldBounds(
                        coord,
                        out float minX,
                        out float maxX,
                        out float minZ,
                        out float maxZ))
                {
                    continue;
                }

                AddLakesFromBakedData(
                    chunkData.BakedLakes,
                    coord,
                    minX,
                    maxX,
                    minZ,
                    maxZ);
            }

            if (!_logDetectedLakes)
                return;

            foreach (Lake lake in _lakes)
            {
                Debug.Log(
                    $"[Lake] id={lake.Id}, chunk={lake.SourceChunkCoord}, " +
                    $"weatherSize={lake.Size:F2}, bakedPixels={lake.BakedPixelCount}, " +
                    $"center={lake.Center}");
            }
        }

        private void AddLakesFromBakedData(
            BakedLakeChunkData bakedData,
            Vector2Int coord,
            float minX,
            float maxX,
            float minZ,
            float maxZ)
        {
            if (bakedData.Regions == null)
                return;

            int minimumPixels = Mathf.Max(1, _minimumLakePixels);

            for (int i = 0; i < bakedData.Regions.Count; i++)
            {
                BakedLakeRegion region = bakedData.Regions[i];

                if (region == null || region.PixelCount < minimumPixels)
                    continue;

                float worldX = Mathf.Lerp(minX, maxX, region.CenterUV.x);
                float worldZ = Mathf.Lerp(minZ, maxZ, region.CenterUV.y);
                float weatherSize = GetNormalizedWeatherSize(
                    region.PixelCount,
                    bakedData.TextureWidth,
                    bakedData.TextureHeight);

                _lakes.Add(new Lake(GetStableLakeId(coord, region.Id))
                {
                    SourceChunkCoord = coord,
                    Size = weatherSize,
                    BakedPixelCount = region.PixelCount,
                    Center = new Vector2(worldX, worldZ)
                });
            }
        }

        private float GetNormalizedWeatherSize(
            int pixelCount,
            int textureWidth,
            int textureHeight)
        {
            if (textureWidth <= 0 || textureHeight <= 0)
                return pixelCount;

            float reference = Mathf.Max(8, _weatherSizeReferenceResolution);
            float scaleX = reference / textureWidth;
            float scaleY = reference / textureHeight;
            return pixelCount * scaleX * scaleY;
        }

        private int GetStableLakeId(Vector2Int coord, int regionId)
        {
            int columns = Mathf.Max(1, _mapStreamer.Columns);
            int chunkIndex = coord.y * columns + coord.x;

            unchecked
            {
                return chunkIndex * 1000003 + regionId;
            }
        }
    }
}
