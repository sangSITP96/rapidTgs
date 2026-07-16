using System;
using UnityEngine;

namespace Game.Weather.Fog
{
    [Serializable]
    public class Fog
    {
        public int Id;
        public int SourceLakeId;
        public Vector2 Position;
        public float SourceLakeSize;
        public FogVisualCategory VisualCategory;

        public GameObject VisualObject;
    }
}