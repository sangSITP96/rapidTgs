using System;
using UnityEngine;

namespace Game.Weather.Convergence
{
    [Serializable]
    public class ConvergencePoint
    {
       public int PointId;
       public Vector2 Position;

       public double SpawnGameSeconds;
       public double ExpireGameSeconds;

       public float AttractionStrength;
       public float CurrentIntensity;
       public int HeldCloudCount;
       
       public ConvergenceDebugView VisualObject;

       public bool IsExpired(double nowGameSeconds)
       {
           return nowGameSeconds >= ExpireGameSeconds;
       }
    }
}

