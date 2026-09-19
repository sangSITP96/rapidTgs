using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Navigation
{
    [Serializable]
    public sealed class NavigationRoute
    {
        public bool IsValid;
        public string FailureReason = string.Empty;

        public int StartCellIndex = -1;
        public int DestinationCellIndex = -1;

        public Vector3 Origin;
        public Vector3 Destination;

        public List<Vector3> Waypoints = new List<Vector3>();

        public List<int> CellIndices = new List<int>();

        public float ApproximateDistance;
        public float PathCost;

        public int WaypointCount => Waypoints != null ? Waypoints.Count : 0;

        public static NavigationRoute Invalid(string reason)
        {
            return new NavigationRoute
            {
                IsValid = false,
                FailureReason = reason ?? "Invalid route"
            };
        }

        public IReadOnlyList<Vector3> GetWaypoints() => Waypoints;

        public IReadOnlyList<int> GetCellIndices() => CellIndices;
    }
}
