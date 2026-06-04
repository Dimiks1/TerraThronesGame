using System.Collections.Generic;
using UnityEngine;
using static HexGridGenerator;

public class RiverGenerator
{
    private readonly HexGridGenerator owner;

    private const int RiversCount = 7;
    private const int MinRiverLength = 4;
    private const int MaxRiverLength = 15;

    private const float StraightBias = 0.2f;
    private const float ExtraConnectChance = 0.2f;

    private const int MaxGenerationAttempts = 25;

    private readonly List<List<Vector2Int>> _riverPaths = new();
    private readonly HashSet<Vector2Int> _riverOccupied = new();
    public IReadOnlyCollection<Vector2Int> RiverOccupied => _riverOccupied;
    private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _riverAdj = new();

    private readonly Dictionary<int, (HexTileType tileType, int rotIndex)> _riverVariantCache = new();
    private bool _riverVariantCacheBuilt = false;

    public RiverGenerator(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    public void GenerateRivers()
    {
        ClearState();

        if (owner.gridConfig == null)
        {
            Debug.LogWarning("RiverGenerator: gridConfig is null.");
            return;
        }

        if (owner.hexGrid == null || owner.hexGrid.Count == 0)
        {
            Debug.LogWarning("RiverGenerator: hexGrid is empty.");
            return;
        }

        for (int i = 0; i < RiversCount; i++)
        {
            List<Vector2Int> path = TryGenerateRiverPath();

            if (path == null || path.Count < MinRiverLength)
            {
                Debug.LogWarning($"RiverGenerator: failed to generate river #{i + 1}.");
                continue;
            }

            RegisterRiverPath(path);
            AddExtraConnections(path);
        }

        ApplyRiversToMap();

        Debug.Log($"RiverGenerator: generated rivers={_riverPaths.Count}, occupiedTiles={_riverOccupied.Count}.");
    }

    private void ClearState()
    {
        _riverPaths.Clear();
        _riverOccupied.Clear();
        _riverAdj.Clear();
    }

    private List<Vector2Int> TryGenerateRiverPath()
    {
        int mapRadius = owner.gridConfig.gridRadius;

        for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            int targetLength = Random.Range(MinRiverLength, MaxRiverLength + 1);

            if (!TryPickStartCoord(mapRadius, out Vector2Int start))
                return null;

            var path = new List<Vector2Int>(targetLength) { start };
            var localPathSet = new HashSet<Vector2Int> { start };

            Vector2Int current = start;
            int previousDirectionIndex = -1;

            for (int step = 1; step < targetLength; step++)
            {
                List<Vector2Int> candidates = CollectValidNeighbors(
                    current,
                    mapRadius,
                    localPathSet
                );

                if (candidates.Count == 0)
                    break;

                Vector2Int chosen = ChooseNextCoord(
                    current,
                    candidates,
                    previousDirectionIndex
                );

                path.Add(chosen);
                localPathSet.Add(chosen);

                previousDirectionIndex = HexMaskUtility.DirectionIndex(current, chosen);
                current = chosen;
            }

            if (path.Count >= MinRiverLength)
                return path;
        }

        return null;
    }

    private bool TryPickStartCoord(int mapRadius, out Vector2Int start)
    {
        start = default;
        int seen = 0;

        foreach (Vector2Int coord in owner.hexGrid.Keys)
        {
            if (!IsValidRiverCoord(coord, mapRadius, null))
                continue;

            seen++;

            if (Random.Range(0, seen) == 0)
                start = coord;
        }

        return seen > 0;
    }

    private List<Vector2Int> CollectValidNeighbors(
        Vector2Int coord,
        int mapRadius,
        HashSet<Vector2Int> localPathSet)
    {
        var result = new List<Vector2Int>(6);

        foreach (Vector2Int dir in HexGridGenerator.hexDirs)
        {
            Vector2Int neighbor = coord + dir;

            if (IsValidRiverCoord(neighbor, mapRadius, localPathSet))
                result.Add(neighbor);
        }

        return result;
    }

    private bool IsValidRiverCoord(
        Vector2Int coord,
        int mapRadius,
        HashSet<Vector2Int> localPathSet)
    {
        if (!owner.hexGrid.ContainsKey(coord))
            return false;

        if (owner.AxialDistance(coord) >= mapRadius - 1)
            return false;

        if (localPathSet != null && localPathSet.Contains(coord))
            return false;

        if (_riverOccupied.Contains(coord))
            return false;

        if (owner.IsCoordForbidden(coord))
            return false;

        if (owner.bonusAdjacencyProtected != null &&
            owner.bonusAdjacencyProtected.Contains(coord))
            return false;

        return true;
    }

    private Vector2Int ChooseNextCoord(
        Vector2Int current,
        List<Vector2Int> candidates,
        int previousDirectionIndex)
    {
        if (candidates.Count == 1)
            return candidates[0];

        if (previousDirectionIndex < 0)
            return candidates[Random.Range(0, candidates.Count)];

        Vector2Int directCandidate = default;
        bool hasDirectCandidate = false;

        var weighted = new List<(Vector2Int coord, float weight)>(candidates.Count);
        float totalWeight = 0f;

        foreach (Vector2Int candidate in candidates)
        {
            int directionIndex = HexMaskUtility.DirectionIndex(current, candidate);

            if (directionIndex < 0)
                continue;

            int angularDistance = HexMaskUtility.AngularDistance(previousDirectionIndex, directionIndex);

            if (angularDistance == 0)
            {
                directCandidate = candidate;
                hasDirectCandidate = true;
            }

            float weight = 1f / (1f + angularDistance);
            weighted.Add((candidate, weight));
            totalWeight += weight;
        }

        if (hasDirectCandidate && Random.value < StraightBias)
            return directCandidate;

        if (weighted.Count == 0 || totalWeight <= 0f)
            return candidates[Random.Range(0, candidates.Count)];

        float roll = Random.value * totalWeight;
        float accumulator = 0f;

        foreach (var item in weighted)
        {
            accumulator += item.weight;

            if (roll <= accumulator)
                return item.coord;
        }

        return weighted[weighted.Count - 1].coord;
    }

    private void RegisterRiverPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
            return;

        _riverPaths.Add(path);

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int coord = path[i];

            AddRiverNode(coord);
            _riverOccupied.Add(coord);

            if (i + 1 < path.Count)
                AddRiverEdge(path[i], path[i + 1]);
        }
    }

    private void AddExtraConnections(List<Vector2Int> path)
    {
        var localPathSet = new HashSet<Vector2Int>(path);

        foreach (Vector2Int coord in path)
        {
            foreach (Vector2Int dir in HexGridGenerator.hexDirs)
            {
                Vector2Int neighbor = coord + dir;

                if (!localPathSet.Contains(neighbor))
                    continue;

                if (!IsOrderedPair(coord, neighbor))
                    continue;

                if (HasRiverEdge(coord, neighbor))
                    continue;

                if (Random.value <= ExtraConnectChance)
                    AddRiverEdge(coord, neighbor);
            }
        }
    }

    private void AddRiverNode(Vector2Int coord)
    {
        if (!_riverAdj.ContainsKey(coord))
            _riverAdj[coord] = new HashSet<Vector2Int>();
    }

    private void AddRiverEdge(Vector2Int from, Vector2Int to)
    {
        AddRiverNode(from);
        AddRiverNode(to);

        _riverAdj[from].Add(to);
        _riverAdj[to].Add(from);
    }

    private bool HasRiverEdge(Vector2Int from, Vector2Int to)
    {
        return _riverAdj.TryGetValue(from, out HashSet<Vector2Int> neighbors) &&
               neighbors.Contains(to);
    }

    private static bool IsOrderedPair(Vector2Int a, Vector2Int b)
    {
        return a.x < b.x || (a.x == b.x && a.y <= b.y);
    }

    private void ApplyRiversToMap()
    {
        if (_riverOccupied.Count == 0)
            return;

        int replacedCount = 0;

        foreach (Vector2Int coord in _riverOccupied)
        {
            if (!owner.hexGrid.TryGetValue(coord, out HexTile existing) || existing == null)
                continue;

            if (existing.gameObject.GetComponent<ProtectedHex>() != null)
                continue;

            if (owner.bonusAdjacencyProtected != null &&
                owner.bonusAdjacencyProtected.Contains(coord))
                continue;

            int mask = BuildRiverMask(coord);

            (HexTileType tileType, int rotIndex) = DetermineRiverTileAndRotation(mask);

            if (tileType == null)
                continue;

            Quaternion rotation = Quaternion.Euler(0f, rotIndex * 60f, 0f);
            owner.ReplaceHexTileWithRotation(coord, tileType, rotation);

            if (owner.hexGrid.TryGetValue(coord, out HexTile newRiverTile) &&
                newRiverTile != null &&
                newRiverTile.gameObject.GetComponent<ProtectedHex>() == null)
            {
                newRiverTile.gameObject.AddComponent<ProtectedHex>();
            }

            replacedCount++;
        }

        Debug.Log($"RiverGenerator: replaced {replacedCount} river tiles.");
    }

    private int BuildRiverMask(Vector2Int coord)
    {
        int mask = 0;

        if (!_riverAdj.TryGetValue(coord, out HashSet<Vector2Int> neighbors))
            return mask;

        for (int dirIndex = 0; dirIndex < HexGridGenerator.hexDirs.Length; dirIndex++)
        {
            Vector2Int neighbor = coord + HexGridGenerator.hexDirs[dirIndex];

            if (neighbors.Contains(neighbor))
                mask |= 1 << dirIndex;
        }

        return mask;
    }

    private (HexTileType tileType, int rotIndex) DetermineRiverTileAndRotation(int targetMask)
    {
        if (!_riverVariantCacheBuilt)
            BuildRiverVariantCache();

        if (_riverVariantCache.TryGetValue(targetMask, out var result))
            return result;

        return (null, 0);
    }

    private void BuildRiverVariantCache()
    {
        _riverVariantCache.Clear();

        if (owner.gridConfig == null || owner.gridConfig.riverVariants == null)
        {
            _riverVariantCacheBuilt = true;
            return;
        }

        foreach (var variant in owner.gridConfig.riverVariants)
        {
            if (variant == null || variant.prefab == null)
                continue;

            int canonicalMask = HexMaskUtility.BuildMaskFromDirections(variant.maskDirs);

            if (canonicalMask == 0)
                continue;

            for (int rotation = 0; rotation < 6; rotation++)
            {
                int rotatedMask = HexMaskUtility.RotateMask(canonicalMask, rotation);
                int finalRotation = (rotation + variant.baseRotation) % 6;

                if (finalRotation < 0)
                    finalRotation += 6;

                if (!_riverVariantCache.ContainsKey(rotatedMask))
                    _riverVariantCache[rotatedMask] = (variant.prefab, finalRotation);
            }
        }

        _riverVariantCacheBuilt = true;
    }
}