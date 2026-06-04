using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HexGridGenerator;

public class LakeGenerator
{
    private readonly HexGridGenerator owner;
    private readonly IReadOnlyCollection<Vector2Int> riverOccupied;

    private const int LakesCount = 6;
    private const int MinLakeSize = 8;
    private const int MaxLakeSize = 45;

    private const float Compactness = 0.85f;

    private const int MaxLakeAttempts = 100;
    private const int MinLakeSeparation = 2;
    private const int RiverBuffer = 1;

    private readonly List<HashSet<Vector2Int>> _lakeRegions = new();
    private readonly HashSet<Vector2Int> _lakeOccupied = new();
    private readonly HashSet<Vector2Int> _lakeProximityBlocked = new();

    private readonly Dictionary<int, (HexTileType tileType, int rotIndex)> _lakeVariantCache = new();
    private bool _lakeVariantCacheBuilt = false;

    public LakeGenerator(
        HexGridGenerator owner,
        IReadOnlyCollection<Vector2Int> riverOccupied = null)
    {
        this.owner = owner;
        this.riverOccupied = riverOccupied ?? new HashSet<Vector2Int>();
    }

    public void GenerateLakes()
    {
        _lakeRegions.Clear();
        _lakeOccupied.Clear();
        _lakeProximityBlocked.Clear();

        if (owner.gridConfig == null)
        {
            Debug.LogWarning("LakeGenerator: gridConfig is null.");
            return;
        }

        if (owner.hexGrid == null || owner.hexGrid.Count == 0)
        {
            Debug.LogWarning("LakeGenerator: hexGrid is empty.");
            return;
        }

        int generatedCount = 0;

        for (int i = 0; i < LakesCount; i++)
        {
            if (!TryGenerateLake(out HashSet<Vector2Int> lake))
            {
                Debug.LogWarning($"LakeGenerator: failed to generate lake #{i + 1}.");
                continue;
            }

            _lakeRegions.Add(lake);
            RegisterLake(lake);

            generatedCount++;
        }

        ApplyLakesToMap();

        Debug.Log($"LakeGenerator: generated lakes={generatedCount}, occupiedTiles={_lakeOccupied.Count}.");
    }
    private bool TryGenerateLake(out HashSet<Vector2Int> result)
    {
        result = null;

        EnsureLakeVariantCacheBuilt();

        for (int attempt = 0; attempt < MaxLakeAttempts; attempt++)
        {
            int targetSize = Random.Range(MinLakeSize, MaxLakeSize + 1);

            if (!TryPickLakeSeed(out Vector2Int seed))
                return false;

            HashSet<Vector2Int> lake = GrowLake(seed, targetSize);

            if (lake == null || lake.Count < MinLakeSize)
                continue;

            if (!ValidateLake(lake))
                continue;

            result = lake;
            return true;
        }

        return false;
    }

    private bool TryPickLakeSeed(out Vector2Int seed)
    {
        seed = default;

        int seen = 0;

        foreach (Vector2Int coord in owner.hexGrid.Keys)
        {
            if (!IsValidLakeCoord(coord, null))
                continue;

            seen++;

            if (Random.Range(0, seen) == 0)
                seed = coord;
        }

        return seen > 0;
    }

    private HashSet<Vector2Int> GrowLake(Vector2Int seed, int targetSize)
    {
        var lake = new HashSet<Vector2Int> { seed };
        var frontier = new HashSet<Vector2Int>();

        AddValidNeighborsToFrontier(seed, lake, frontier);

        while (lake.Count < targetSize && frontier.Count > 0)
        {
            Vector2Int chosen = ChooseFrontierCell(frontier, lake);

            frontier.Remove(chosen);
            lake.Add(chosen);

            AddValidNeighborsToFrontier(chosen, lake, frontier);
        }

        return lake;
    }

    private void AddValidNeighborsToFrontier(
        Vector2Int coord,
        HashSet<Vector2Int> lake,
        HashSet<Vector2Int> frontier)
    {
        foreach (Vector2Int dir in HexGridGenerator.hexDirs)
        {
            Vector2Int neighbor = coord + dir;

            if (lake.Contains(neighbor))
                continue;

            if (frontier.Contains(neighbor))
                continue;

            if (!IsValidLakeCoord(neighbor, lake))
                continue;

            frontier.Add(neighbor);
        }
    }

    private Vector2Int ChooseFrontierCell(
        HashSet<Vector2Int> frontier,
        HashSet<Vector2Int> lake)
    {
        float totalWeight = 0f;
        var weighted = new List<(Vector2Int coord, float weight)>(frontier.Count);

        foreach (Vector2Int candidate in frontier)
        {
            int connected = CountLakeNeighbors(candidate, lake);

            float weight = Mathf.Pow(
                1f + connected,
                Mathf.Lerp(1f, 4f, Compactness)
            );

            weight *= 0.9f + 0.2f * Random.value;

            weighted.Add((candidate, weight));
            totalWeight += weight;
        }

        if (weighted.Count == 0 || totalWeight <= 0f)
            return frontier.First();

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

    private bool IsValidLakeCoord(
        Vector2Int coord,
        HashSet<Vector2Int> currentLake)
    {
        if (!owner.hexGrid.TryGetValue(coord, out HexTile tile) || tile == null)
            return false;

        if (currentLake != null && currentLake.Contains(coord))
            return false;

        if (_lakeOccupied.Contains(coord))
            return false;

        if (_lakeProximityBlocked.Contains(coord))
            return false;

        if (tile.gameObject.GetComponent<ProtectedHex>() != null)
            return false;

        if (owner.IsForbiddenTile(tile))
            return false;

        if (IsCoordNearSet(coord, riverOccupied, RiverBuffer))
            return false;

        return true;
    }

    private bool IsCoordNearSet(
        Vector2Int coord,
        IReadOnlyCollection<Vector2Int> coords,
        int radius)
    {
        if (coords == null || coords.Count == 0)
            return false;

        foreach (Vector2Int other in coords)
        {
            if (owner.AxialDistance(coord, other) <= radius)
                return true;
        }

        return false;
    }

    private void RegisterLake(HashSet<Vector2Int> lake)
    {
        if (lake == null || lake.Count == 0)
            return;

        foreach (Vector2Int coord in lake)
            _lakeOccupied.Add(coord);

        foreach (Vector2Int mapCoord in owner.hexGrid.Keys)
        {
            if (_lakeProximityBlocked.Contains(mapCoord))
                continue;

            if (IsCoordNearSet(mapCoord, lake, MinLakeSeparation))
                _lakeProximityBlocked.Add(mapCoord);
        }
    }

    private bool ValidateLake(HashSet<Vector2Int> lake)
    {
        EnsureLakeVariantCacheBuilt();

        foreach (Vector2Int coord in lake)
        {
            int mask = BuildLakeMask(coord, lake);

            if (HexMaskUtility.CountBits(mask) < 2)
                return false;

            if (!HexMaskUtility.HasAdjacentBits(mask))
                return false;

            if (!_lakeVariantCache.ContainsKey(mask))
            {
                Debug.LogWarning(
                    $"Lake rejected: no lake variant for mask {System.Convert.ToString(mask, 2).PadLeft(6, '0')} at {coord}."
                );

                return false;
            }
        }

        return true;
    }

    private int CountLakeNeighbors(
        Vector2Int coord,
        HashSet<Vector2Int> lake)
    {
        int count = 0;

        foreach (Vector2Int dir in HexGridGenerator.hexDirs)
        {
            if (lake.Contains(coord + dir))
                count++;
        }

        return count;
    }

    private int BuildLakeMask(
        Vector2Int coord,
        HashSet<Vector2Int> lake)
    {
        int mask = 0;

        for (int dirIndex = 0; dirIndex < HexGridGenerator.hexDirs.Length; dirIndex++)
        {
            Vector2Int neighbor = coord + HexGridGenerator.hexDirs[dirIndex];

            if (lake.Contains(neighbor))
                mask |= 1 << dirIndex;
        }

        return mask;
    }

    private void ApplyLakesToMap()
    {
        EnsureLakeVariantCacheBuilt();

        int replacedCount = 0;

        foreach (HashSet<Vector2Int> lake in _lakeRegions)
        {
            foreach (Vector2Int coord in lake)
            {
                if (!owner.hexGrid.TryGetValue(coord, out HexTile existing) || existing == null)
                    continue;

                if (existing.gameObject.GetComponent<ProtectedHex>() != null)
                    continue;

                int mask = BuildLakeMask(coord, lake);

                if (!_lakeVariantCache.TryGetValue(mask, out var variant))
                    continue;

                Quaternion rotation = Quaternion.Euler(0f, variant.rotIndex * 60f, 0f);

                owner.ReplaceHexTileWithRotation(coord, variant.tileType, rotation);

                if (owner.hexGrid.TryGetValue(coord, out HexTile newLakeTile) &&
                    newLakeTile != null &&
                    newLakeTile.gameObject.GetComponent<ProtectedHex>() == null)
                {
                    newLakeTile.gameObject.AddComponent<ProtectedHex>();
                }

                replacedCount++;
            }
        }
        Debug.Log($"LakeGenerator: replaced {replacedCount} lake tiles.");
    }

    private void EnsureLakeVariantCacheBuilt()
    {
        if (_lakeVariantCacheBuilt)
            return;

        BuildLakeVariantCache();
        _lakeVariantCacheBuilt = true;
    }

    private void BuildLakeVariantCache()
    {
        _lakeVariantCache.Clear();

        if (owner.gridConfig == null || owner.gridConfig.lakeVariants == null)
            return;

        foreach (var variant in owner.gridConfig.lakeVariants)
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

                if (!_lakeVariantCache.ContainsKey(rotatedMask))
                    _lakeVariantCache[rotatedMask] = (variant.prefab, finalRotation);
            }
        }
    }
}