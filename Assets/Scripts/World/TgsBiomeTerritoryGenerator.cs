using TGS;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class TgsBiomeTerritoryGenerator : MonoBehaviour
{
    [SerializeField] private TerrainGridSystem _tgs;
    
    [Header("Territory Generation")]
    [SerializeField] private int _numTerritories = 25;

    [Range(0f, 1f)] [SerializeField] private float _territoriesOrganic = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float _territoriesAsymmetry = 0.4f;

    [Header("Biome Distribution")] 
    [SerializeField]
    private int _seed = 1;

    [SerializeField] private int _minLakes = 2;
    [SerializeField] private int _minMountains = 2;
    [SerializeField] private int _minForests = 3;

    [Header("Output")] [SerializeField] private TgsBiomeMapData _mapData;

    [Header("Cell Preview")]
    [Tooltip("Padding giữa các cell khi preview màu. Tăng nhẹ để không thấy đường nét đứt giữa các ô cùng territory.")]
    [SerializeField] private float _cellFillPadding = 0.1f;

    private void Reset()
    {
        _tgs = GetComponent<TerrainGridSystem>();
    }

    #if UNITY_EDITOR
    [ContextMenu("1. Apply TGS Grid Settings")]
    public void ApplyTgsGridSettings()
    {
        if (_tgs == null) _tgs = GetComponent<TerrainGridSystem>();
        if (_tgs == null) return;

        _tgs.gridTopology = GridTopology.Irregular;
        _tgs.numTerritories = _numTerritories;
        _tgs.territoriesOrganic = _territoriesOrganic;
        _tgs.territoriesAsymmetry = _territoriesAsymmetry;
        _tgs.transparentBackground = true;
        _tgs.showTerritories = true;
        _tgs.colorizeTerritories = false;
        _tgs.Redraw();

        AssignOrphanCellsToNearestTerritory();
        _tgs.Redraw();

        EditorUtility.SetDirty(_tgs);
        LogUnassignedCellCount();
        Debug.Log("[TGS Biome] Applied grid settings. Territories regenerated.");
    }

    [ContextMenu("2. Assign Biome Colors To Territories")]
    public void AssignBiomeColorsToTerritories()
    {
        if (_tgs == null) _tgs = GetComponent<TerrainGridSystem>();
        if (_tgs == null || _tgs.territories == null || _tgs.territories.Count == 0)
        {
            Debug.LogError("[TGS Biome] No territories. Run step 1 first.");
            return;
        }

        int count = _tgs.territories.Count;
        List<BiomeType> biomes = BuildBiomeList(count);

        if (_mapData == null)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save TgsBiomeMapData",
                "TgsBiomeMapData",
                "asset",
                "Choose place to save mapping territory -> biome");

            if (string.IsNullOrEmpty(path)) return;

            _mapData = ScriptableObject.CreateInstance<TgsBiomeMapData>();
            AssetDatabase.CreateAsset(_mapData, path);
        }

        HideAllTerritoryRegionSurfaces();
        _tgs.CellHideRegionSurfaces();

        var entries = new TgsBiomeMapData.TerritoryBiomeEntry[count];
        var fillColors = new Color[count];

        for (int t = 0; t < count; t++)
        {
            BiomeType biome = biomes[t];
            Color color = BiomePalette.GetColor(biome);

            entries[t] = new TgsBiomeMapData.TerritoryBiomeEntry
            {
                TerritoryIndex = t,
                Biome = biome
            };

            fillColors[t] = color;
            _tgs.territories[t].fillColor = color;
        }

        _tgs.territoriesColorScheme = TerritoryColorScheme.UserDefined;
        _tgs.territoriesFillColors = fillColors;
        _tgs.transparentBackground = true;
        _tgs.showTerritories = false;
        _tgs.showCells = false;
        _tgs.colorizeTerritories = false;

        _mapData.Seed = _seed;
        _mapData.TerritoryCount = count;
        _mapData.Entries = entries;

        ApplyCellBiomeColors(biomes);

        EditorUtility.SetDirty(_tgs);
        EditorUtility.SetDirty(_mapData);
        AssetDatabase.SaveAssets();

        LogUnassignedCellCount();
        LogBiomeDistribution(biomes);
        Debug.Log($"[TGS Biome] Colored {_tgs.cells.Count} cells across {count} territories.");
    }

    private void LogBiomeDistribution(IReadOnlyList<BiomeType> biomes)
    {
        int lakes = 0, mountains = 0, forests = 0, grasslands = 0;
        for (int i = 0; i < biomes.Count; i++)
        {
            switch (biomes[i])
            {
                case BiomeType.Lake: lakes++; break;
                case BiomeType.Mountain: mountains++; break;
                case BiomeType.Forest: forests++; break;
                default: grasslands++; break;
            }
        }

        Debug.Log(
            $"[TGS Biome] Distribution — Lake: {lakes}, Mountain: {mountains}, " +
            $"Forest: {forests}, Grassland: {grasslands}");
    }

    /// <summary>
    /// Tô từng cell theo territory — tránh lỗ hổng của territory mesh (vùng xám/đen giữa map).
    /// </summary>
    private void ApplyCellBiomeColors(IReadOnlyList<BiomeType> biomes)
    {
        if (_tgs.cells == null) return;

        _tgs.cellFillPadding = _cellFillPadding;

        for (int i = 0; i < _tgs.cells.Count; i++)
        {
            Cell cell = _tgs.cells[i];
            if (cell == null || !cell.visible)
                continue;

            Color color = cell.territoryIndex >= 0 && cell.territoryIndex < biomes.Count
                ? BiomePalette.GetColor(biomes[cell.territoryIndex])
                : BiomePalette.Grassland;

            if (cell.region != null)
                cell.region.customMaterial = null;

            _tgs.CellToggleRegionSurface(i, true, color, refreshGeometry: true);
        }
    }

    /// <summary>
    /// Gán cell chưa có territory cho territory của neighbor gần nhất.
    /// </summary>
    private void AssignOrphanCellsToNearestTerritory()
    {
        if (_tgs.cells == null) return;

        bool changed = true;
        int safety = 0;

        while (changed && safety++ < _tgs.cells.Count)
        {
            changed = false;

            for (int i = 0; i < _tgs.cells.Count; i++)
            {
                Cell cell = _tgs.cells[i];
                if (cell == null || !cell.visible || cell.territoryIndex >= 0)
                    continue;

                List<Cell> neighbours = cell.neighbours;
                if (neighbours == null) continue;

                for (int n = 0; n < neighbours.Count; n++)
                {
                    Cell neighbour = neighbours[n];
                    if (neighbour == null || !neighbour.visible || neighbour.territoryIndex < 0)
                        continue;

                    _tgs.CellSetTerritory(i, neighbour.territoryIndex);
                    changed = true;
                    break;
                }
            }
        }
    }

    private void HideAllTerritoryRegionSurfaces()
    {
        int terrCount = _tgs.territories.Count;
        for (int t = 0; t < terrCount; t++)
        {
            int regionCount = _tgs.territories[t].regions.Count;
            for (int r = 0; r < regionCount; r++)
                _tgs.TerritoryHideRegionSurface(t, r);
        }
    }

    private void LogUnassignedCellCount()
    {
        if (_tgs.cells == null) return;

        int unassigned = 0;
        for (int i = 0; i < _tgs.cells.Count; i++)
        {
            Cell cell = _tgs.cells[i];
            if (cell != null && cell.visible && cell.territoryIndex < 0)
                unassigned++;
        }

        if (unassigned > 0)
        {
            Debug.LogWarning(
                $"[TGS Biome] {unassigned} visible cells have no territory. " +
                "They may appear as dark gaps — lower Num Territories or increase Territories Max Range.");
        }
    }


    private List<BiomeType> BuildBiomeList(int territoryCount)
    {
        var rng = new System.Random(_seed);
        var list = new List<BiomeType>(territoryCount);

        int minLake = Mathf.Min(_minLakes, territoryCount);
        int minMountain = Mathf.Min(_minMountains, territoryCount);
        int minForest = Mathf.Min(_minForests, territoryCount);
        int requiredSpecial = minLake + minMountain + minForest;

        if (requiredSpecial > territoryCount)
        {
            Debug.LogWarning(
                $"[TGS Biome] Min Lakes+Mountains+Forests ({requiredSpecial}) > Num Territories ({territoryCount}). " +
                "Some minimums were clamped.");
            while (requiredSpecial > territoryCount && minForest > 0) { minForest--; requiredSpecial--; }
            while (requiredSpecial > territoryCount && minMountain > 0) { minMountain--; requiredSpecial--; }
            while (requiredSpecial > territoryCount && minLake > 0) { minLake--; requiredSpecial--; }
        }

        for (int i = 0; i < territoryCount; i++)
            list.Add(BiomeType.Grassland);

        PlaceMinimum(list, BiomeType.Lake, minLake, rng);
        PlaceMinimum(list, BiomeType.Mountain, minMountain, rng);
        PlaceMinimum(list, BiomeType.Forest, minForest, rng);

        // Phần còn lại giữ Grassland. Chỉ random hoá thêm khi còn dư slot sau khi đã đủ minimum.
        int extraSlots = territoryCount - (minLake + minMountain + minForest);
        if (extraSlots > 1)
        {
            BiomeType[] all = { BiomeType.Grassland, BiomeType.Forest, BiomeType.Lake, BiomeType.Mountain };
            int converted = 0;
            int maxExtraNonGrassland = extraSlots - 1;

            for (int i = 0; i < list.Count && converted < maxExtraNonGrassland; i++)
            {
                if (list[i] != BiomeType.Grassland)
                    continue;

                BiomeType pick = all[rng.Next(all.Length)];
                if (pick == BiomeType.Grassland)
                    continue;

                list[i] = pick;
                converted++;
            }
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        EnsureMinimum(list, BiomeType.Lake, minLake);
        EnsureMinimum(list, BiomeType.Mountain, minMountain);
        EnsureMinimum(list, BiomeType.Forest, minForest);

        return list;
    }

    private static void PlaceMinimum(List<BiomeType> list, BiomeType biome, int min, System.Random rng)
    {
        int have = 0;
        for(int i = 0; i<list.Count; i++)
            if (list[i] == biome)
                have++;

        while (have < min)
        {
            var idx = rng.Next(list.Count);
            if(list[idx] == biome) continue;
            list[idx] = biome;
            have++;
        }
    }

    private static void EnsureMinimum(List<BiomeType> list, BiomeType biome, int min)
    {
        int have = 0;
        for(int i = 0; i<list.Count; i++)
            if (list[i] == biome)
                have++;

        for (int i = 0; i < list.Count && have < min; i++)
        {
            if (list[i] == BiomeType.Grassland)
            {
                list[i] = biome;
                have++;
            }
        }
    }
    #endif
}
