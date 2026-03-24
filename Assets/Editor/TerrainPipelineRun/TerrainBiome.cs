using System;
using UnityEngine;

public enum TerrainBiome
{
   Forest = 0,
   Grassland = 1,
   Lake = 2,
   Mountain =3
}

[Serializable]
public class BiomeInputs
{
    public Texture2D Height;
    public Texture2D Visual;
    public Texture2D ForestMask;
    public Texture2D LakeMask;
}

public class GeneratedSheet
{
    public int Resolution;

    public Texture2D Height; 
    public Texture2D Visual; 
    public Texture2D ForestMask; 
    public Texture2D LakeMask; 
}