using UnityEngine;

namespace Game.Weather.Global
{
    public enum WindDirection
    {
        WestToEast,
        WestToNortheast,
        WestToSoutheast,
        
        // Used for temporary 'sporadic' shifts
        North,
        South,
        East
    }
    public static class WindDirectionUtil
    {
        public static Vector2 ToVector2(WindDirection direction)
        {
            // Xz plane mapping -> vector2(x,z)
            switch (direction)
            {
                case WindDirection.WestToEast:
                case WindDirection.East:
                    return Vector2.right;
                
                case WindDirection.WestToNortheast:
                    return (Vector2.right + Vector2.up).normalized;
                
                case WindDirection.WestToSoutheast:
                    return (Vector2.right + Vector2.down).normalized;
                
                case WindDirection.North:
                    return Vector2.up;
                
                case WindDirection.South:
                    return Vector2.down;
                
                default:
                    return Vector2.right;
            }
        }
    }
}