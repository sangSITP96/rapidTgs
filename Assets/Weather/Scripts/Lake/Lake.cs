using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather.Lake
{
    public class Lake
    {
        public int Id { get; set; }
        public List<int> CellIndies { get; set; } = new();
        public Vector2 Center { get; set; }
        public float Size { get; set; }
        public Vector2Int SourceChunkCoord { get; set; }

        public float GetCloudSpawnChance(float baseChange = 0.2f)
        {
            float sizeNormalized = Mathf.Clamp01(Size / 10f);
            return Mathf.Lerp(0.1f, 0.4f, sizeNormalized) * baseChange;
        }

        public Lake(int id)
        {
            this.Id = id;
        }
    }
}
