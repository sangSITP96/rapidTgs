using System.Collections.Generic;
using TGS;
using UnityEngine;

namespace Game.Navigation
{
    public sealed class TgsNavigationPathfinder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TerrainGridSystem _tgs;
        [SerializeField] private TgsBiomeMapData _biomeMapData;
        [SerializeField] private WorldTerrainQuery _terrainQuery;

        [Header("Pathfinding")]
        [SerializeField] private int _pathFindingMaxSteps = 4000;

        [SerializeField] private int _diagnosticMaxSteps = 12000;

        [SerializeField] private bool _blockUsingBakedLake = true;

        [Header("March Waypoints")]
        [SerializeField, Min(1)] private int _waypointStride = 1;

        [SerializeField, Min(0f)] private float _minWaypointSpacing = 0f;

        [Header("Debug")]
        [SerializeField] private bool _logPathResults = true;
        [SerializeField] private bool _verifyLakeAgainstBake;

        private bool _traversalSynced;
        private int _blockedLakeCellCount;

        public TerrainGridSystem Tgs => _tgs;
        public TgsBiomeMapData BiomeMapData => _biomeMapData;
        public int PathFindingMaxSteps => _pathFindingMaxSteps;
        public int WaypointStride => _waypointStride;
        public float MinWaypointSpacing => _minWaypointSpacing;

        public void ConfigureWaypointSampling(int stride, float minSpacing)
        {
            _waypointStride = Mathf.Max(1, stride);
            _minWaypointSpacing = Mathf.Max(0f, minSpacing);
        }

        private void Awake()
        {
            ResolveRefs();
        }

        public void ResolveRefs()
        {
            if (_tgs == null)
                _tgs = FindFirstObjectByType<TerrainGridSystem>();
            if (_biomeMapData == null)
            {
                var generator = FindFirstObjectByType<TgsBiomeTerritoryGenerator>();
                if (generator != null)
                    _biomeMapData = generator.MapData;
            }
            if (_terrainQuery == null)
                _terrainQuery = FindFirstObjectByType<WorldTerrainQuery>();
        }

        public void SetBiomeMapData(TgsBiomeMapData mapData)
        {
            _biomeMapData = mapData;
            _traversalSynced = false;
        }

        public void SyncTraversalFromBiomes(bool force = false)
        {
            ResolveRefs();

            if (!force && _traversalSynced)
                return;

            if (_tgs == null || _tgs.cells == null || _biomeMapData == null)
            {
                Debug.LogWarning($"{nameof(TgsNavigationPathfinder)}: Cannot sync traversal — missing TGS or BiomeMapData.");
                return;
            }

            _blockedLakeCellCount = 0;
            int cellCount = _tgs.cells.Count;
            int bakeBlockedExtra = 0;

            for (int i = 0; i < cellCount; i++)
            {
                Cell cell = _tgs.cells[i];
                if (cell == null)
                    continue;

                bool blocked = IsRoutingBlockedCell(i, out bool fromBake);
                _tgs.CellSetCanCross(i, !blocked);

                if (blocked)
                {
                    _blockedLakeCellCount++;
                    if (fromBake)
                        bakeBlockedExtra++;
                }
            }

            _traversalSynced = true;

            if (_logPathResults)
            {
                Debug.Log(
                    $"{nameof(TgsNavigationPathfinder)}: Synced canCross. " +
                    $"cells={cellCount}, blocked={_blockedLakeCellCount} (bakeExtra≈{bakeBlockedExtra})");
            }

            if (_verifyLakeAgainstBake && _terrainQuery != null)
                VerifyLakeCellsAgainstBake();
        }

        public int TryGetCellIndexAtWorld(Vector3 worldPos)
        {
            ResolveRefs();
            if (_tgs == null || _tgs.cells == null || _tgs.cells.Count == 0)
                return -1;

            Cell cell = _tgs.CellGetAtPosition(worldPos, worldSpace: true);
            int index = _tgs.CellGetIndex(cell);
            if (index >= 0)
                return index;

            Vector3 onGridPlane = worldPos;
            onGridPlane.y = _tgs.transform.position.y;
            cell = _tgs.CellGetAtPosition(onGridPlane, worldSpace: true);
            index = _tgs.CellGetIndex(cell);
            if (index >= 0)
                return index;

            return FindNearestCellIndex(worldPos);
        }

        private int FindNearestCellIndex(Vector3 worldPos)
        {
            if (_tgs == null || _tgs.cells == null)
                return -1;

            float bestDistSq = float.MaxValue;
            int bestIndex = -1;
            Vector3 flat = new Vector3(worldPos.x, 0f, worldPos.z);

            for (int i = 0; i < _tgs.cells.Count; i++)
            {
                Cell c = _tgs.cells[i];
                if (c == null)
                    continue;

                Vector3 centroid = _tgs.CellGetCentroid(i, worldSpace: true);
                float dx = centroid.x - flat.x;
                float dz = centroid.z - flat.z;
                float dSq = dx * dx + dz * dz;
                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestIndex = i;
                }
            }

            const float maxDist = 2.5f;
            if (bestIndex < 0 || bestDistSq > maxDist * maxDist)
            {
                if (_logPathResults)
                {
                    Debug.LogWarning(
                        $"{nameof(TgsNavigationPathfinder)}: No TGS cell near troop at {worldPos}. " +
                        $"nearestDist={(bestIndex >= 0 ? Mathf.Sqrt(bestDistSq) : -1f):0.00}. " +
                        "Check TGS position/scale vs map, and that marble is inside the grid.");
                }

                return -1;
            }

            if (_logPathResults)
            {
                Debug.Log(
                    $"{nameof(TgsNavigationPathfinder)}: Start cell resolved via nearest centroid " +
                    $"(cell={bestIndex}, dist={Mathf.Sqrt(bestDistSq):0.00}).");
            }

            return bestIndex;
        }

        public bool IsLakeCell(int cellIndex)
        {
            return IsBiomeLakeCell(cellIndex);
        }

        public NavigationRoute FindRoute(Vector3 originWorld, int destinationCellIndex)
        {
            ResolveRefs();
            SyncTraversalFromBiomes(force: true);

            if (_tgs == null)
                return Fail(NavigationPathStatus.MissingDependencies, "TerrainGridSystem missing.");

            if (_biomeMapData == null)
                return Fail(NavigationPathStatus.MissingDependencies, "TgsBiomeMapData missing.");

            int startCellIndex = TryGetCellIndexAtWorld(originWorld);
            if (startCellIndex < 0)
            {
                return Fail(
                    NavigationPathStatus.InvalidStart,
                    $"Could not resolve start cell under troop at {originWorld}. " +
                    "Troop may be outside the TGS grid bounds (check TGS transform scale/position vs map).");
            }

            if (destinationCellIndex < 0 || destinationCellIndex >= _tgs.cells.Count)
                return Fail(NavigationPathStatus.InvalidDestination, "Destination cell index out of range.");

            if (IsRoutingBlockedCell(destinationCellIndex, out _))
            {
                return Fail(
                    NavigationPathStatus.DestinationBlocked,
                    $"Destination cell {destinationCellIndex} is Lake/blocked (impassable).");
            }

            if (startCellIndex == destinationCellIndex)
            {
                Vector3 pos = GetCellWorldPosition(destinationCellIndex, originWorld.y);
                var trivial = new NavigationRoute
                {
                    IsValid = true,
                    StartCellIndex = startCellIndex,
                    DestinationCellIndex = destinationCellIndex,
                    Origin = originWorld,
                    Destination = pos,
                    ApproximateDistance = 0f,
                    PathCost = 0f
                };
                trivial.Waypoints.Add(originWorld);
                trivial.Waypoints.Add(pos);
                trivial.CellIndices.Add(destinationCellIndex);
                return trivial;
            }

            ApplyMaxSteps(_pathFindingMaxSteps);

            var pathCells = new List<int>(256);
            float totalCost;
            int pathLength = _tgs.FindPath(
                startCellIndex,
                destinationCellIndex,
                pathCells,
                out totalCost,
                maxSearchCost: 0,
                maxSteps: _pathFindingMaxSteps,
                cellGroupMask: -1,
                canCrossCheckType: CanCrossCheckType.IgnoreCanCrossCheckOnStartCell,
                ignoreCellCosts: true,
                includeInvisibleCells: true);

            if (pathLength <= 0 || pathCells.Count == 0)
                return DiagnoseFailure(originWorld, startCellIndex, destinationCellIndex);

            if (!ValidatePathCells(pathCells, startCellIndex, out int badCell))
            {
                return Fail(
                    NavigationPathStatus.NoValidRoute,
                    $"FindPath returned blocked cell {badCell}. Check biome/bake lake sync.");
            }

            NavigationRoute route = BuildRoute(originWorld, startCellIndex, destinationCellIndex, pathCells, totalCost);

            if (_logPathResults)
            {
                Debug.Log(
                    $"{nameof(TgsNavigationPathfinder)}: Route OK. " +
                    $"start={startCellIndex} dest={destinationCellIndex} " +
                    $"cells={route.CellIndices.Count} waypoints={route.WaypointCount} " +
                    $"dist≈{route.ApproximateDistance:0.00} cost={route.PathCost:0.00}");
            }

            return route;
        }

        private bool ValidatePathCells(List<int> pathCells, int startCellIndex, out int badCell)
        {
            badCell = -1;
            for (int i = 0; i < pathCells.Count; i++)
            {
                int cellIndex = pathCells[i];

                if (cellIndex == startCellIndex)
                    continue;

                if (IsRoutingBlockedCell(cellIndex, out _))
                {
                    badCell = cellIndex;
                    return false;
                }
            }

            return true;
        }

        private bool IsBiomeLakeCell(int cellIndex)
        {
            if (_tgs == null || _biomeMapData == null || cellIndex < 0 || cellIndex >= _tgs.cells.Count)
                return false;

            Cell cell = _tgs.cells[cellIndex];
            if (cell == null)
                return false;

            return _biomeMapData.GetBiomeForTerritory(cell.territoryIndex) == BiomeType.Lake;
        }

        private bool IsRoutingBlockedCell(int cellIndex, out bool blockedByBake)
        {
            blockedByBake = false;

            if (IsBiomeLakeCell(cellIndex))
                return true;

            if (!_blockUsingBakedLake || _terrainQuery == null || _tgs == null)
                return false;

            if (cellIndex < 0 || cellIndex >= _tgs.cells.Count)
                return false;

            if (CellContainsBakedLake(cellIndex))
            {
                blockedByBake = true;
                return true;
            }

            return false;
        }

        private bool CellContainsBakedLake(int cellIndex)
        {
            if (_terrainQuery == null || _tgs == null)
                return false;

            Cell cell = _tgs.cells[cellIndex];
            if (cell == null)
                return false;

            Vector3 centroid = _tgs.CellGetCentroid(cellIndex, worldSpace: true);
            if (IsWorldBlocked(centroid))
                return true;

            var points = cell.region != null ? cell.region.points : null;
            if (points == null || points.Count == 0)
                return false;

            int step = Mathf.Max(1, points.Count / 6);
            for (int i = 0; i < points.Count; i += step)
            {
                Vector3 vertexWorld = _tgs.GetWorldSpacePosition(points[i]);

                Vector3 sample = Vector3.Lerp(centroid, vertexWorld, 0.55f);
                sample.y = centroid.y;
                if (IsWorldBlocked(sample))
                    return true;
            }

            return false;
        }

        private NavigationRoute DiagnoseFailure(Vector3 originWorld, int startCellIndex, int destinationCellIndex)
        {
            int configured = Mathf.Max(1, _pathFindingMaxSteps);
            int diagnostic = Mathf.Max(configured + 1, _diagnosticMaxSteps);

            ApplyMaxSteps(diagnostic);

            var retryPath = new List<int>(256);
            float retryCost;
            int retryLength = _tgs.FindPath(
                startCellIndex,
                destinationCellIndex,
                retryPath,
                out retryCost,
                maxSearchCost: 0,
                maxSteps: diagnostic,
                cellGroupMask: -1,
                canCrossCheckType: CanCrossCheckType.IgnoreCanCrossCheckOnStartCell,
                ignoreCellCosts: true,
                includeInvisibleCells: true);

            ApplyMaxSteps(_pathFindingMaxSteps);

            if (retryLength > 0 && retryPath.Count > 0)
            {
                string reason =
                    $"Pathfinding hit maxSteps ({configured}). " +
                    $"A longer search (maxSteps={diagnostic}) found a path of {retryPath.Count} cells. " +
                    $"Increase Path Finding Max Steps.";

                if (_logPathResults)
                    Debug.LogWarning($"{nameof(TgsNavigationPathfinder)}: {reason}");

                var failed = NavigationRoute.Invalid(reason);
                failed.StartCellIndex = startCellIndex;
                failed.DestinationCellIndex = destinationCellIndex;
                failed.Origin = originWorld;
                return failed;
            }

            string noRoute =
                $"No valid route from cell {startCellIndex} to {destinationCellIndex} " +
                $"(checked maxSteps={configured} and diagnostic={diagnostic}). " +
                "Likely blocked by Lake (biome/bake) or disconnected grid.";

            if (_logPathResults)
                Debug.LogWarning($"{nameof(TgsNavigationPathfinder)}: {noRoute}");

            var invalid = NavigationRoute.Invalid(noRoute);
            invalid.StartCellIndex = startCellIndex;
            invalid.DestinationCellIndex = destinationCellIndex;
            invalid.Origin = originWorld;
            return invalid;
        }

        private NavigationRoute BuildRoute(
            Vector3 originWorld,
            int startCellIndex,
            int destinationCellIndex,
            List<int> pathCells,
            float totalCost)
        {
            var route = new NavigationRoute
            {
                IsValid = true,
                StartCellIndex = startCellIndex,
                DestinationCellIndex = destinationCellIndex,
                Origin = originWorld,
                PathCost = totalCost
            };

            if (_waypointStride > 1 && _logPathResults)
            {
                Debug.Log(
                    $"{nameof(TgsNavigationPathfinder)}: WaypointStride={_waypointStride} ignored for march " +
                    "waypoints (using cell centroids + border gates).");
            }

            route.Waypoints.Add(originWorld);

            Vector3 previousPoint = originWorld;
            int previousCell = -1;

            for (int i = 0; i < pathCells.Count; i++)
            {
                int cellIndex = pathCells[i];
                route.CellIndices.Add(cellIndex);

                Vector3 centroid = GetCellWorldPosition(cellIndex, originWorld.y);

                if (previousCell >= 0)
                {
                    AppendSafeTransition(route.Waypoints, previousCell, cellIndex, previousPoint, centroid, originWorld.y);
                }
                else if (SegmentCrossesBlockedTerrain(previousPoint, centroid))
                {
                    Vector3 safe = Vector3.Lerp(previousPoint, centroid, 0.35f);
                    if (!IsWorldBlocked(safe))
                        AppendWaypoint(route.Waypoints, safe);
                }

                AppendWaypoint(route.Waypoints, centroid);
                previousPoint = route.Waypoints[route.Waypoints.Count - 1];
                previousCell = cellIndex;
            }

            SanitizeWaypointsOffLake(route.Waypoints);

            if (route.Waypoints.Count == 1)
            {
                Vector3 dest = GetCellWorldPosition(destinationCellIndex, originWorld.y);
                AppendWaypoint(route.Waypoints, dest);
            }

            route.Destination = route.Waypoints[route.Waypoints.Count - 1];
            route.ApproximateDistance = MeasurePathLength(route.Waypoints);
            return route;
        }

        private void SanitizeWaypointsOffLake(List<Vector3> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0 || _terrainQuery == null)
                return;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (!IsWorldBlocked(waypoints[i]))
                    continue;

                Vector3 fixedPos = waypoints[i];
                bool fixedOk = false;

                if (i > 0 && !IsWorldBlocked(waypoints[i - 1]))
                {
                    for (int s = 1; s <= 8; s++)
                    {
                        Vector3 p = Vector3.Lerp(waypoints[i], waypoints[i - 1], s / 8f);
                        if (!IsWorldBlocked(p))
                        {
                            fixedPos = p;
                            fixedOk = true;
                            break;
                        }
                    }
                }

                if (!fixedOk && i + 1 < waypoints.Count && !IsWorldBlocked(waypoints[i + 1]))
                {
                    for (int s = 1; s <= 8; s++)
                    {
                        Vector3 p = Vector3.Lerp(waypoints[i], waypoints[i + 1], s / 8f);
                        if (!IsWorldBlocked(p))
                        {
                            fixedPos = p;
                            fixedOk = true;
                            break;
                        }
                    }
                }

                if (fixedOk)
                    waypoints[i] = fixedPos;
            }

            for (int i = waypoints.Count - 1; i > 0; i--)
            {
                if (HorizontalDistanceSq(waypoints[i], waypoints[i - 1]) < 0.0001f)
                    waypoints.RemoveAt(i);
            }
        }

        private void AppendSafeTransition(
            List<Vector3> waypoints,
            int fromCell,
            int toCell,
            Vector3 fromPoint,
            Vector3 toCentroid,
            float y)
        {
            if (TryGetSharedEdgeGate(fromCell, toCell, y, out Vector3 gate))
            {
                if (IsWorldBlocked(gate))
                {
                    Vector3 gateInFrom = Vector3.Lerp(gate, fromPoint, 0.3f);
                    Vector3 gateInTo = Vector3.Lerp(gate, toCentroid, 0.3f);
                    gateInFrom.y = y;
                    gateInTo.y = y;
                    AppendWaypoint(waypoints, gateInFrom);
                    AppendWaypoint(waypoints, gateInTo);
                }
                else
                {
                    AppendWaypoint(waypoints, gate);
                }

                return;
            }

            if (!SegmentCrossesBlockedTerrain(fromPoint, toCentroid))
                return;

            int samples = Mathf.Clamp(
                Mathf.CeilToInt(HorizontalDistance(fromPoint, toCentroid) / 0.06f),
                4,
                32);

            for (int s = 1; s < samples; s++)
            {
                float t = s / (float)samples;
                Vector3 p = Vector3.Lerp(fromPoint, toCentroid, t);
                p.y = y;
                if (IsWorldBlocked(p))
                    continue;

                AppendWaypoint(waypoints, p);
            }
        }

        private bool TryGetSharedEdgeGate(int cellA, int cellB, float y, out Vector3 worldMid)
        {
            worldMid = default;
            if (_tgs == null ||
                cellA < 0 || cellB < 0 ||
                cellA >= _tgs.cells.Count || cellB >= _tgs.cells.Count)
            {
                return false;
            }

            Cell a = _tgs.cells[cellA];
            Cell b = _tgs.cells[cellB];
            List<Vector2> pa = a?.region?.points;
            List<Vector2> pb = b?.region?.points;
            if (pa == null || pb == null || pa.Count < 2 || pb.Count < 2)
                return false;

            const float epsSq = 1e-8f;
            for (int i = 0; i < pa.Count; i++)
            {
                Vector2 p0 = pa[i];
                Vector2 p1 = pa[(i + 1) % pa.Count];
                if (!ContainsApprox(pb, p0, epsSq) || !ContainsApprox(pb, p1, epsSq))
                    continue;

                Vector2 midLocal = (p0 + p1) * 0.5f;
                worldMid = _tgs.GetWorldSpacePosition(midLocal);
                worldMid.y = y;
                return true;
            }

            Vector2 sum = Vector2.zero;
            int sharedCount = 0;
            for (int i = 0; i < pa.Count; i++)
            {
                if (!ContainsApprox(pb, pa[i], epsSq))
                    continue;
                sum += pa[i];
                sharedCount++;
            }

            if (sharedCount == 0)
                return false;

            worldMid = _tgs.GetWorldSpacePosition(sum / sharedCount);
            worldMid.y = y;
            return true;
        }

        private static bool ContainsApprox(List<Vector2> points, Vector2 p, float epsSq)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if ((points[i] - p).sqrMagnitude <= epsSq)
                    return true;
            }

            return false;
        }

        private static void AppendWaypoint(List<Vector3> waypoints, Vector3 wp)
        {
            if (waypoints.Count > 0 && HorizontalDistanceSq(waypoints[waypoints.Count - 1], wp) < 0.0001f)
                return;
            waypoints.Add(wp);
        }

        private bool SegmentCrossesBlockedTerrain(Vector3 a, Vector3 b)
        {
            float dist = HorizontalDistance(a, b);
            if (dist < 0.01f)
                return IsWorldBlocked(a);

            int samples = Mathf.Clamp(Mathf.CeilToInt(dist / 0.05f), 2, 64);
            for (int s = 0; s <= samples; s++)
            {
                float t = s / (float)samples;
                if (IsWorldBlocked(Vector3.Lerp(a, b, t)))
                    return true;
            }

            return false;
        }

        private bool IsWorldBlocked(Vector3 worldPos)
        {
            if (_terrainQuery == null)
                return false;
            return _terrainQuery.IsMovementBlocked(worldPos) || _terrainQuery.IsLake(worldPos);
        }

        private void ApplyMaxSteps(int maxSteps)
        {
            if (_tgs == null || maxSteps <= 0)
                return;

            _tgs.pathFindingMaxSteps = maxSteps;
        }

        private Vector3 GetCellWorldPosition(int cellIndex, float preserveY)
        {
            Vector3 pos = _tgs.CellGetCentroid(cellIndex, worldSpace: true);
            pos.y = preserveY;
            return pos;
        }

        private void VerifyLakeCellsAgainstBake()
        {
            int mismatches = 0;
            int checkedCount = 0;

            for (int i = 0; i < _tgs.cells.Count; i++)
            {
                Cell cell = _tgs.cells[i];
                if (cell == null)
                    continue;

                bool biomeLake = _biomeMapData.GetBiomeForTerritory(cell.territoryIndex) == BiomeType.Lake;
                Vector3 world = _tgs.CellGetPosition(i, worldSpace: true);
                bool bakeLake = _terrainQuery.IsLake(world);
                checkedCount++;

                if (biomeLake != bakeLake)
                    mismatches++;
            }

            Debug.Log(
                $"{nameof(TgsNavigationPathfinder)}: Lake verify vs bake — " +
                $"checked={checkedCount}, mismatches={mismatches}");
        }

        private static NavigationRoute Fail(NavigationPathStatus status, string reason)
        {
            Debug.LogWarning($"{nameof(TgsNavigationPathfinder)}: [{status}] {reason}");
            return NavigationRoute.Invalid($"[{status}] {reason}");
        }

        private static float MeasurePathLength(List<Vector3> waypoints)
        {
            float sum = 0f;
            for (int i = 1; i < waypoints.Count; i++)
                sum += HorizontalDistance(waypoints[i - 1], waypoints[i]);
            return sum;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
