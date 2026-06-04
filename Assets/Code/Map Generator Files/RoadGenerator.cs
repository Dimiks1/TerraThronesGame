using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HexGridGenerator;

public class RoadGenerator
{
    private readonly HexGridGenerator owner;

    private const int MinStraightLength = 3;
    private const int MaxStraightLength = 4;
    private const float ArcTryChance = 0.9f;

    public readonly Dictionary<Vector2Int, List<Vector2Int>> _bonusRoadPaths = new();
    private readonly HashSet<Vector2Int> _occupiedRoadTiles = new();
    private readonly Dictionary<Vector2Int, int> _forcedBonusSideByRoadTile = new();

    private readonly List<Vector2Int> _unifiedRoadCoordsList = new();
    public readonly HashSet<Vector2Int> _unifiedRoadCoordsSet = new();
    public readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _unifiedAdj = new();

    private readonly Dictionary<Vector2Int, Vector2Int> _neutralBonusOutgoing = new();
    private readonly Dictionary<Vector2Int, List<Vector2Int>> _neutralBonusRoadPaths = new();

    private readonly Dictionary<int, (HexTileType tileType, int rotIndex)> _roadVariantCache = new();
    private bool _roadVariantCacheBuilt = false;

    public RoadGenerator(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    public void GenerateRoads()
    {
        GenerateBonusRoads();
        BuildNeutralShortestPathsFromSavedOutgoing();
        BuildUnifiedRoadNetwork();
        ApplyRoadsToMap();
    }

    private void GenerateBonusRoads()
    {
        _bonusRoadPaths.Clear();
        _neutralBonusOutgoing.Clear();
        _neutralBonusRoadPaths.Clear();
        _occupiedRoadTiles.Clear();
        _forcedBonusSideByRoadTile.Clear();

        var allBases = new List<Vector2Int>();
        allBases.AddRange(owner.greenBaseCoords);
        allBases.AddRange(owner.yellowBaseCoords);

        foreach (var bonus in owner.placedBonusCoords)
        {
            Vector2Int nearestBase = allBases
                .OrderBy(b => owner.AxialDistance(bonus, b))
                .First();

            var banned = new HashSet<Vector2Int>(owner.placedBonusCoords);
            Vector2Int? outgoing = GetOutgoingNeighborByRotation(bonus);

            TrySaveNeutralBonusOutgoing(bonus, outgoing);

            if (outgoing.HasValue)
            {
                var path = TryBuildProcessedRoadPath(outgoing.Value, new[] { nearestBase }, banned, _occupiedRoadTiles);

                if (TryStoreRoad(_bonusRoadPaths, bonus, path, outgoing))
                    continue;
            }
        }
    }

    private List<Vector2Int> PostProcessPath(
    List<Vector2Int> path,
    HashSet<Vector2Int> banned,
    HashSet<Vector2Int> occupied,
    Vector2Int? neutralBonus = null)
    {
        if (path == null || path.Count == 0)
            return null;

        path = ReplaceStraightSegmentsWithArcs(path, banned);

        if (path == null || path.Count == 0)
            return null;

        HashSet<Vector2Int> ignoredIntersections = neutralBonus.HasValue
            ? GetBonusNeighborhood(neutralBonus.Value)
            : null;

        path = TruncatePathAtFirstIntersection(path, occupied, ignoredIntersections);

        if (path == null || path.Count == 0)
            return null;

        return path;
    }

    private List<Vector2Int> TryBuildProcessedRoadPath(
    Vector2Int start,
    IEnumerable<Vector2Int> targets,
    HashSet<Vector2Int> banned,
    HashSet<Vector2Int> occupied,
    Vector2Int? neutralBonus = null)
    {
        if (!owner.hexGrid.ContainsKey(start))
            return null;

        foreach (var target in targets)
        {
            if (!owner.hexGrid.ContainsKey(target))
                continue;

            var path = owner.FindPath( start, target, treatProtectedAsWalkable: true, banned: banned);

            path = PostProcessPath(path, banned, occupied, neutralBonus);

            if (path != null && path.Count > 0)
                return path;
        }

        return null;
    }

    private bool TryStoreRoad(
    Dictionary<Vector2Int, List<Vector2Int>> storage,
    Vector2Int bonus,
    List<Vector2Int> path,
    Vector2Int? forcedEntry = null)
    {
        if (path == null || path.Count == 0)
            return false;

        storage[bonus] = path;

        foreach (var coord in path)
            _occupiedRoadTiles.Add(coord);

        if (forcedEntry.HasValue)
            RegisterBonusEntry(forcedEntry.Value, bonus);

        return true;
    }

    private void RegisterBonusEntry(Vector2Int entryCoord, Vector2Int bonusCoord)
    {
        int bonusDir = HexMaskUtility.DirectionIndex(entryCoord, bonusCoord);

        if (bonusDir >= 0)
            _forcedBonusSideByRoadTile[entryCoord] = bonusDir;
    }

    private void TrySaveNeutralBonusOutgoing(Vector2Int bonus, Vector2Int? outgoing)
    {
        if (!outgoing.HasValue)
            return;

        if (!owner.hexGrid.TryGetValue(bonus, out HexTile tile) || tile == null || tile.TileType == null)
            return;

        if (owner.gridConfig == null || owner.gridConfig.bonusNeutralTile == null)
            return;

        if (tile.TileType != owner.gridConfig.bonusNeutralTile)
            return;

        int n = owner.gridConfig.gridRadius;

        Vector2Int leftNeutral = new Vector2Int(-n + 5, 0);
        Vector2Int rightNeutral = new Vector2Int(n - 5, 0);

        if (bonus == leftNeutral || bonus == rightNeutral)
            _neutralBonusOutgoing[bonus] = outgoing.Value;
    }

    private List<Vector2Int> TruncatePathAtFirstIntersection(
    List<Vector2Int> path,
    HashSet<Vector2Int> occupied,
    HashSet<Vector2Int> ignoredIntersections = null)
    {
        if (path == null || path.Count == 0)
            return null;

        if (occupied == null || occupied.Count == 0)
            return path;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int coord = path[i];

            if (ignoredIntersections != null && ignoredIntersections.Contains(coord))
                continue;

            if (!occupied.Contains(coord))
                continue;

            if (i == 0)
                return path.GetRange(0, 1);

            return path.GetRange(0, i + 1);
        }

        return path;
    }

    private Vector2Int? GetOutgoingNeighborByRotation(Vector2Int bonusCoord)
    {
        if (!owner.hexGrid.TryGetValue(bonusCoord, out var bonusTile) || bonusTile?.TileType == null)
            return null;

        float worldY = bonusTile.transform.rotation.eulerAngles.y;

        int sector = Mathf.RoundToInt(worldY / 60f) % 6;
        if (sector < 0)
            sector += 6;

        Vector2Int dir = HexGridGenerator.hexDirs[sector];
        Vector2Int neighbor = bonusCoord + dir;

        return owner.hexGrid.ContainsKey(neighbor) ? neighbor : null;
    }

    private List<Vector2Int> ReplaceStraightSegmentsWithArcs(
    List<Vector2Int> path,
    HashSet<Vector2Int> banned)
    {
        if (path == null || path.Count < 3)
            return path;

        var rng = new System.Random();
        var result = new List<Vector2Int>();

        int index = 0;

        while (index < path.Count)
        {
            if (index >= path.Count - 1)
            {
                result.Add(path[index]);
                index++;
                continue;
            }

            Vector2Int first = path[index];
            Vector2Int second = path[index + 1];
            Vector2Int direction = second - first;

            int segmentEnd = index + 1;

            while (segmentEnd + 1 < path.Count)
            {
                Vector2Int nextDirection = path[segmentEnd + 1] - path[segmentEnd];

                if (nextDirection != direction)
                    break;

                segmentEnd++;
            }

            int straightLength = segmentEnd - index;

            if (straightLength >= MinStraightLength)
            {
                int maxLength = Mathf.Min(MaxStraightLength, straightLength);
                bool replaced = false;

                for (int length = maxLength; length >= MinStraightLength; length--)
                {
                    if (rng.NextDouble() > ArcTryChance)
                        continue;

                    int firstSide = rng.NextDouble() < 0.5 ? 1 : -1;
                    int secondSide = -firstSide;
                    int[] sides = { firstSide, secondSide };

                    for (int s = 0; s < sides.Length; s++)
                    {
                        int side = sides[s];

                        List<Vector2Int> candidate = TryMakeArcReplacement(path, index, length, banned, side);

                        if (candidate == null)
                            continue;

                        var existingOutside = new HashSet<Vector2Int>();

                        foreach (var coord in result)
                            existingOutside.Add(coord);

                        for (int k = index + length + 1; k < path.Count; k++)
                            existingOutside.Add(path[k]);

                        bool intersects = false;

                        foreach (var coord in candidate)
                        {
                            if (existingOutside.Contains(coord))
                            {
                                intersects = true;
                                break;
                            }
                        }

                        if (intersects)
                            continue;

                        if (result.Count > 0 && result[result.Count - 1] == candidate[0])
                        {
                            for (int k = 1; k < candidate.Count; k++)
                                result.Add(candidate[k]);
                        }
                        else
                        {
                            foreach (var coord in candidate)
                                result.Add(coord);
                        }

                        index = index + length + 1;
                        replaced = true;
                        break;
                    }

                    if (replaced)
                        break;
                }

                if (replaced)
                    continue;

                result.Add(path[index]);
                index++;
            }
            else
            {
                result.Add(path[index]);
                index++;
            }
        }

        var compact = new List<Vector2Int>();

        foreach (var coord in result)
        {
            if (compact.Count == 0 || compact[compact.Count - 1] != coord)
                compact.Add(coord);
        }

        return compact;
    }

    private bool IsTileUseableForArc(Vector2Int coord, HashSet<Vector2Int> banned)
    {
        if (!owner.hexGrid.TryGetValue(coord, out HexTile tile))
            return false;

        if (tile == null || tile.TileType == null)
            return false;

        if (banned != null && banned.Contains(coord))
            return false;

        // ProtectedHex Ќ≈ запрещаем дл€ дорог.
        // ќн нужен, чтобы реки/озЄра/прочие генераторы не трогали область бонуса,
        // но дорога к бонусу имеет право проходить через эти клетки.

        if (owner.IsForbiddenTile(tile))
            return false;

        return true;
    }

    private List<Vector2Int> TryMakeArcReplacement(
    List<Vector2Int> path,
    int i,
    int length,
    HashSet<Vector2Int> banned,
    int side)
    {
        Vector2Int start = path[i];
        Vector2Int next = path[i + 1];
        Vector2Int straightDir = next - start;
        int dirIndex = -1;

        for (int d = 0; d < HexGridGenerator.hexDirs.Length; d++)
        {
            if (HexGridGenerator.hexDirs[d] == straightDir)
            {
                dirIndex = d;
                break;
            }
        }
        if (dirIndex == -1)
            return null;

        int offsetIndex = (dirIndex + 2 * side + 6) % 6;
        Vector2Int offsetDir = HexGridGenerator.hexDirs[offsetIndex];

        int steps = length;
        int maxOffset = Mathf.Max(1, Mathf.CeilToInt(steps / 2.0f));

        var generated = new List<Vector2Int>();

        Vector2Int current = start;
        generated.Add(current);

        for (int step = 1; step <= steps; step++)
        {
            float fraction = Mathf.Sin(Mathf.PI * step / (steps + 1));
            int offset = Mathf.Max(0, Mathf.RoundToInt(fraction * maxOffset));

            Vector2Int target = new Vector2Int(
                start.x + straightDir.x * step + offsetDir.x * offset,
                start.y + straightDir.y * step + offsetDir.y * offset
            );

            if (!TryGetBestNeighborTowards(current, target, banned, out Vector2Int bestNeighbor))
                return null;

            generated.Add(bestNeighbor);
            current = bestNeighbor;
        }

        Vector2Int end = path[i + length];

        if (current != end)
        {
            const int safety = 4;
            bool reached = false;

            for (int attempt = 0; attempt < safety && current != end; attempt++)
            {
                if (!TryGetBestNeighborTowards(current, end, banned, out Vector2Int bestNeighbor))
                    break;

                generated.Add(bestNeighbor);
                current = bestNeighbor;

                if (current == end)
                {
                    reached = true;
                    break;
                }
            }
            if (!reached && current != end)
                return null;
        }

        for (int k = 1; k < generated.Count; k++)
        {
            if (owner.AxialDistance(generated[k - 1], generated[k]) != 1)
                return null;
        }
        return generated;
    }

    private bool TryGetBestNeighborTowards(
    Vector2Int current,
    Vector2Int target,
    HashSet<Vector2Int> banned,
    out Vector2Int bestNeighbor)
    {
        bestNeighbor = current;
        int bestDistance = int.MaxValue;

        foreach (var dir in HexGridGenerator.hexDirs)
        {
            Vector2Int neighbor = current + dir;

            if (!IsTileUseableForArc(neighbor, banned))
                continue;

            int distance = owner.AxialDistance(neighbor, target);

            if (distance < bestDistance ||
                (distance == bestDistance &&
                 (neighbor.x < bestNeighbor.x ||
                  (neighbor.x == bestNeighbor.x && neighbor.y < bestNeighbor.y))))
            {
                bestDistance = distance;
                bestNeighbor = neighbor;
            }
        }

        return bestNeighbor != current;
    }

    private void BuildUnifiedRoadNetwork()
    {
        _unifiedRoadCoordsList.Clear();
        _unifiedRoadCoordsSet.Clear();
        _unifiedAdj.Clear();

        if ((_bonusRoadPaths == null || _bonusRoadPaths.Count == 0) &&
            (_neutralBonusRoadPaths == null || _neutralBonusRoadPaths.Count == 0))
        {
            Debug.LogWarning("BuildUnifiedRoadNetwork: no road paths found.");
            return;
        }

        void AddPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
                return;

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];

                AddRoadNode(current);

                if (i + 1 >= path.Count)
                    continue;

                Vector2Int next = path[i + 1];

                AddRoadNode(next);
                AddRoadEdge(current, next);
            }
        }

        foreach (var pair in _bonusRoadPaths)
            AddPath(pair.Value);

        foreach (var pair in _neutralBonusRoadPaths)
            AddPath(pair.Value);

        int edgeCount = _unifiedAdj.Sum(pair => pair.Value.Count) / 2;

        Debug.Log(
            $"BuildUnifiedRoadNetwork: nodes={_unifiedRoadCoordsList.Count}, edges={edgeCount}, " +
            $"bonusPaths={_bonusRoadPaths.Count}, neutralPaths={_neutralBonusRoadPaths.Count}");
    }

    private void AddRoadNode(Vector2Int coord)
    {
        if (!_unifiedRoadCoordsSet.Contains(coord))
        {
            _unifiedRoadCoordsSet.Add(coord);
            _unifiedRoadCoordsList.Add(coord);
        }

        if (!_unifiedAdj.ContainsKey(coord))
            _unifiedAdj[coord] = new HashSet<Vector2Int>();
    }

    private void AddRoadEdge(Vector2Int from, Vector2Int to)
    {
        if (!_unifiedAdj.ContainsKey(from))
            _unifiedAdj[from] = new HashSet<Vector2Int>();

        if (!_unifiedAdj.ContainsKey(to))
            _unifiedAdj[to] = new HashSet<Vector2Int>();

        _unifiedAdj[from].Add(to);
        _unifiedAdj[to].Add(from);
    }

    private void ApplyRoadsToMap()
    {
        if (_unifiedAdj == null || _unifiedAdj.Count == 0)
        {
            Debug.LogWarning("ApplyRoadsToMap: unified road network is empty.");
            return;
        }

        int replacedCount = 0;

        foreach (var coord in _unifiedRoadCoordsList)
        {
            if (!owner.hexGrid.TryGetValue(coord, out HexTile existing) || existing == null)
                continue;

            if (existing.gameObject.GetComponent<ProtectedHex>() != null)
                continue;

            if (!_unifiedAdj.TryGetValue(coord, out HashSet<Vector2Int> neighbors))
                continue;

            int mask = BuildRoadMask(coord, neighbors);

            if (_forcedBonusSideByRoadTile.TryGetValue(coord, out int bonusDir))
                mask |= (1 << bonusDir);

            (HexTileType tileType, int rotIndex) = DetermineRoadTileAndRotation(mask);

            if (tileType == null)
                continue;

            ReplaceHexTileWithRotation(coord, tileType, rotIndex);

            if (owner.hexGrid.TryGetValue(coord, out HexTile newRoadTile) && newRoadTile != null && newRoadTile.gameObject.GetComponent<ProtectedHex>() == null)
            {
                newRoadTile.gameObject.AddComponent<ProtectedHex>();
            }
            replacedCount++;
        }

        Debug.Log($"ApplyRoadsToMap: replaced {replacedCount} road tiles.");
    }

    private int BuildRoadMask(Vector2Int coord, HashSet<Vector2Int> neighbors)
    {
        int mask = 0;

        for (int d = 0; d < HexGridGenerator.hexDirs.Length; d++)
        {
            Vector2Int neighbor = coord + HexGridGenerator.hexDirs[d];

            if (neighbors.Contains(neighbor))
                mask |= (1 << d);
        }

        return mask;
    }

    private void ReplaceHexTileWithRotation(Vector2Int coord, HexTileType tileType, int rotIndex)
    {
        Quaternion rotation = Quaternion.Euler(0f, rotIndex * 60f, 0f);
        owner.ReplaceHexTileWithRotation(coord, tileType, rotation);
    }

    private (HexTileType, int) DetermineRoadTileAndRotation(int targetMask)
    {
        if (!_roadVariantCacheBuilt)
            BuildRoadVariantCache();

        if (_roadVariantCache.TryGetValue(targetMask, out var exact))
            return exact;

        return DetermineRoadTileFallback(targetMask, owner.gridConfig.roadVariants);
    }

    private void BuildRoadVariantCache()
    {
        _roadVariantCache.Clear();

        if (owner.gridConfig == null || owner.gridConfig.roadVariants == null)
        {
            _roadVariantCacheBuilt = true;
            return;
        }

        foreach (var variant in owner.gridConfig.roadVariants)
        {
            if (variant == null || variant.prefab == null)
                continue;

            int canonicalMask = HexMaskUtility.BuildMaskFromDirections(variant.maskDirs);

            if (canonicalMask == 0)
                continue;

            for (int rotation = 0; rotation < 6; rotation++)
            {
                int rotatedMask = HexMaskUtility.RotateMask(canonicalMask, rotation);

                int finalRotation = HexMaskUtility.NormalizeDirectionIndex(rotation + variant.baseRotation);

                if (!_roadVariantCache.ContainsKey(rotatedMask))
                    _roadVariantCache[rotatedMask] = (variant.prefab, finalRotation);
            }
        }
        _roadVariantCacheBuilt = true;
    }

    private (HexTileType, int) DetermineRoadTileFallback(
     int targetMask,
     List<HexGridConfig.RoadVariant> variants)
    {
        if (variants == null || variants.Count == 0)
            return (null, 0);

        int targetConnectionCount = HexMaskUtility.CountBits(targetMask);
        int firstTargetDir = HexMaskUtility.FirstSetBitIndex(targetMask);

        foreach (var variant in variants)
        {
            if (variant == null || variant.prefab == null)
                continue;

            int variantMask = HexMaskUtility.BuildMaskFromDirections(variant.maskDirs);

            if (HexMaskUtility.CountBits(variantMask) != targetConnectionCount)
                continue;

            int firstVariantDir = HexMaskUtility.FirstSetBitIndex(variantMask);

            if (firstTargetDir < 0 || firstVariantDir < 0)
                return (variant.prefab, variant.baseRotation);

            int rotation = HexMaskUtility.NormalizeDirectionIndex(firstTargetDir - firstVariantDir);
            int finalRotation = HexMaskUtility.NormalizeDirectionIndex(rotation + variant.baseRotation);

            return (variant.prefab, finalRotation);
        }

        Debug.LogWarning(
            $"DetermineRoadTileAndRotation: no variant found for mask {System.Convert.ToString(targetMask, 2).PadLeft(6, '0')}");

        return (null, 0);
    }

    private void BuildNeutralShortestPathsFromSavedOutgoing()
    {
        if (_neutralBonusOutgoing == null || _neutralBonusOutgoing.Count == 0)
        {
            Debug.Log("BuildNeutralShortestPathsFromSavedOutgoing: no saved neutral outgoing entries.");
            return;
        }

        int n = owner.gridConfig.gridRadius;

        Vector2Int yellowRight = new Vector2Int(0, n - 4);
        Vector2Int yellowLeft = new Vector2Int(-n + 4, n - 4);

        var banned = new HashSet<Vector2Int>(owner.placedBonusCoords);

        foreach (var pair in _neutralBonusOutgoing.ToList())
        {
            Vector2Int bonus = pair.Key;
            Vector2Int start = pair.Value;

            Vector2Int target = bonus.x >= 0 ? yellowRight : yellowLeft;

            var path = TryBuildProcessedRoadPath(
                start,
                new[] { target },
                banned,
                _occupiedRoadTiles,
                neutralBonus: bonus);

            if (TryStoreRoad(_neutralBonusRoadPaths, bonus, path, start))
            {
                Debug.Log($"Neutral road created for bonus {bonus}, length={path.Count}");
                continue;
            }

            Debug.LogWarning($"BuildNeutralShortestPathsFromSavedOutgoing: no neutral path for bonus {bonus}");
        }
    }

    private HashSet<Vector2Int> GetBonusNeighborhood(Vector2Int bonus)
    {
        var neighborhood = new HashSet<Vector2Int>
    {
        bonus
    };

        foreach (var dir in HexGridGenerator.hexDirs)
            neighborhood.Add(bonus + dir);

        return neighborhood;
    }
}
