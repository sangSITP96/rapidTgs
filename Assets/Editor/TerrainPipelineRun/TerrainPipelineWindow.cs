using UnityEditor;
using UnityEngine;

public class TerrainPipelineWindow : EditorWindow
{
    [Header("Input Biomes")]
    [SerializeField] private BiomeInputs forest;
    [SerializeField] private BiomeInputs grass;
    [SerializeField] private BiomeInputs lake;
    [SerializeField] private BiomeInputs mountain;

    [Header("Target Runtime")]
    [SerializeField] private InfiniteMapStreamer streamer;

    [Header("Output")]
    [SerializeField] private DefaultAsset tilesRoot;
    [SerializeField] private DefaultAsset mapChunkRoot;

    [Header("Seed + Grid")]
    [SerializeField] private int seed = 1337;
    [SerializeField] private int tileSize = 256;

   
}
