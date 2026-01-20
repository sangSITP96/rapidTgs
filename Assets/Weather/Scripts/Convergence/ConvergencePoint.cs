using System;
using UnityEngine;

namespace Game.Weather.Convergence
{
    [Serializable]
    public class ConvergencePoint
    {
       public Vector2 Position;
       public Vector2 DriftDirection;

       public double SpawnGameSeconds;
       public double ExpireGameSeconds;

       public float AttractionStrength;
       
       public ConvergenceDebugView VisualObject;

       public bool IsExpired(double nowGameSeconds)
       {
           return nowGameSeconds >= ExpireGameSeconds;
       }
    }
}

