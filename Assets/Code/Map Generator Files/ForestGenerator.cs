using System.Collections.Generic;
using UnityEngine;
using static HexGridGenerator;

public class ForestGenerator
{
    public const string ForestChildName = "ForestOverlay";
    public const string ForestChildNameCluster = "ForestOverlayCluster";

    private readonly HexGridGenerator owner;

    private readonly List<Vector2Int> forestClusterCenters = new();
    private readonly Dictionary<Vector2Int, int> forestClusterRadiusByCenter = new();
    private readonly Dictionary<Vector2Int, HexGridConfig.ForestOverlaySet> forestClusterSetByCenter = new();

    public ForestGenerator(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    public void GenerateForests()
    {
        if (owner.gridConfig == null)
        {
            Debug.LogWarning("ForestGenerator: gridConfig is null.");
            return;
        }

        if (!owner.gridConfig.useForestClusters)
            return;

        if (owner.hexGrid == null || owner.hexGrid.Count == 0)
        {
            Debug.LogWarning("ForestGenerator: hexGrid is empty.");
            return;
        }

        InitializeForestClusterSeed();
        BuildForestClusterCenters();
        ApplyForestClusters();
    }

    private void InitializeForestClusterSeed()
    {
        if (!owner.gridConfig.randomizeForestClusterSeed)
        {
            owner.gridConfig.forestClusterOffset =
                owner.gridConfig.forestClusterBaseOffset;

            return;
        }

        owner.gridConfig.forestClusterSeed =
            Random.Range(int.MinValue, int.MaxValue);

        var random = new System.Random(owner.gridConfig.forestClusterSeed);

        float jitterX = (float)random.NextDouble() * 2f - 1f;
        float jitterY = (float)random.NextDouble() * 2f - 1f;

        owner.gridConfig.forestClusterOffset =
            owner.gridConfig.forestClusterBaseOffset +
            new Vector2(jitterX, jitterY) * owner.gridConfig.forestClusterSeedJitter;
    }

    private void BuildForestClusterCenters()
    {
        forestClusterCenters.Clear();
        forestClusterRadiusByCenter.Clear();
        forestClusterSetByCenter.Clear();

        var mask = new Dictionary<Vector2Int, float>(owner.hexGrid.Count);

        foreach (var pair in owner.hexGrid)
        {
            Vector2Int coord = pair.Key;
            HexTile tile = pair.Value;

            if (!CanUseTileForForest(coord, tile))
                continue;

            float maskValue = SampleForestClusterMask01(coord);

            if (maskValue >= owner.gridConfig.forestClusterMaskThreshold)
                mask[coord] = maskValue;
        }

        if (mask.Count == 0)
            return;

        var peaks = new List<Vector2Int>();

        foreach (var pair in mask)
        {
            Vector2Int coord = pair.Key;
            float value = pair.Value;

            if (IsLocalPeak(coord, value, mask))
                peaks.Add(coord);
        }

        peaks.Sort((a, b) => mask[b].CompareTo(mask[a]));

        int minSpacing = Mathf.Max(1, owner.gridConfig.forestClusterMinSpacing);
        int maxCenters = Mathf.Max(1, owner.gridConfig.forestClusterMaxCenters);

        foreach (Vector2Int peak in peaks)
        {
            if (!IsFarEnoughFromExistingCenters(peak, minSpacing))
                continue;

            forestClusterCenters.Add(peak);

            int radius = CalculateClusterRadius(peak);
            forestClusterRadiusByCenter[peak] = radius;

            PickOverlaySetForCenter(peak);

            if (forestClusterCenters.Count >= maxCenters)
                break;
        }
    }

    private bool IsLocalPeak(
        Vector2Int coord,
        float value,
        Dictionary<Vector2Int, float> mask)
    {
        foreach (Vector2Int dir in HexGridGenerator.hexDirs)
        {
            Vector2Int neighbor = coord + dir;

            if (mask.TryGetValue(neighbor, out float neighborValue) &&
                neighborValue > value)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsFarEnoughFromExistingCenters(Vector2Int coord, int minSpacing)
    {
        for (int i = 0; i < forestClusterCenters.Count; i++)
        {
            if (owner.AxialDistance(coord, forestClusterCenters[i]) < minSpacing)
                return false;
        }

        return true;
    }

    private int CalculateClusterRadius(Vector2Int center)
    {
        int minRadius = Mathf.Max(2, owner.gridConfig.forestClusterRadiusRange.x);
        int maxRadius = Mathf.Max(minRadius + 1, owner.gridConfig.forestClusterRadiusRange.y);

        return Mathf.RoundToInt(
            Mathf.Lerp(minRadius, maxRadius, NoiseUtility.Hash01(center))
        );
    }

    private void ApplyForestClusters()
    {
        if (forestClusterCenters.Count == 0)
            return;

        int placedCount = 0;

        foreach (var pair in owner.hexGrid)
        {
            Vector2Int coord = pair.Key;
            HexTile tile = pair.Value;

            if (!CanUseTileForForest(coord, tile))
                continue;

            if (!TryGetNearestCluster(coord, out Vector2Int center, out int distance, out int radius))
                continue;

            float normalizedDistance = radius > 0
                ? Mathf.Clamp01((float)distance / radius)
                : 1f;

            float maskValue = SampleForestClusterMask01(coord);

            GameObject prefab = PickForestPrefab(center, maskValue, normalizedDistance);

            RemoveExistingForestOverlay(tile);

            if (prefab == null)
                continue;

            PlaceForestOverlay(tile, prefab, coord);
            placedCount++;
        }

        Debug.Log($"ForestGenerator: centers={forestClusterCenters.Count}, placed={placedCount}.");
    }

    private bool TryGetNearestCluster(
        Vector2Int coord,
        out Vector2Int nearestCenter,
        out int nearestDistance,
        out int nearestRadius)
    {
        nearestCenter = default;
        nearestDistance = int.MaxValue;
        nearestRadius = 0;

        bool found = false;

        for (int i = 0; i < forestClusterCenters.Count; i++)
        {
            Vector2Int center = forestClusterCenters[i];

            if (!forestClusterRadiusByCenter.TryGetValue(center, out int radius))
                continue;

            int distance = owner.AxialDistance(coord, center);

            if (distance > radius)
                continue;

            if (distance >= nearestDistance)
                continue;

            nearestCenter = center;
            nearestDistance = distance;
            nearestRadius = radius;
            found = true;
        }

        return found;
    }

    private GameObject PickForestPrefab(
        Vector2Int center,
        float maskValue,
        float normalizedDistance)
    {
        HexGridConfig.ForestOverlaySet set = PickOverlaySetForCenter(center);

        if (set == null)
            return null;

        float sparseThreshold = Mathf.Clamp01(owner.gridConfig.forestClusterSparseThreshold);
        float mediumThreshold = Mathf.Clamp01(owner.gridConfig.forestClusterMediumThreshold);
        float denseThreshold = Mathf.Clamp01(owner.gridConfig.forestClusterDenseThreshold);

        // Чем ближе к краю кластера, тем сложнее получить густой лес.
        float edgePenalty = Mathf.Lerp(0f, 0.25f, normalizedDistance);
        float density = Mathf.Clamp01(maskValue - edgePenalty);

        if (density >= denseThreshold && set.dense != null)
            return set.dense;

        if (density >= mediumThreshold && set.medium != null)
            return set.medium;

        if (density >= sparseThreshold && set.sparse != null)
            return set.sparse;

        return null;
    }

    private HexGridConfig.ForestOverlaySet PickOverlaySetForCenter(Vector2Int center)
    {
        if (forestClusterSetByCenter.TryGetValue(center, out var cachedSet))
            return cachedSet;

        float roll = NoiseUtility.Hash01(center);

        HexGridConfig.ForestOverlaySet chosen =
            roll < owner.gridConfig.forestSetAProbability
                ? owner.gridConfig.forestSetA
                : owner.gridConfig.forestSetB;

        if (!IsValidOverlaySet(chosen))
        {
            if (IsValidOverlaySet(owner.gridConfig.forestSetA))
                chosen = owner.gridConfig.forestSetA;
            else if (IsValidOverlaySet(owner.gridConfig.forestSetB))
                chosen = owner.gridConfig.forestSetB;
            else
                chosen = null;
        }

        forestClusterSetByCenter[center] = chosen;
        return chosen;
    }

    private bool IsValidOverlaySet(HexGridConfig.ForestOverlaySet set)
    {
        return set != null &&
               (set.sparse != null || set.medium != null || set.dense != null);
    }

    private void PlaceForestOverlay(
        HexTile tile,
        GameObject prefab,
        Vector2Int coord)
    {
        GameObject child = Object.Instantiate(prefab, tile.transform);

        child.name = ForestChildNameCluster;
        child.transform.localPosition = new Vector3(
            0f,
            owner.gridConfig.forestOverlayYCluster,
            0f
        );

        int rotationStep = NoiseUtility.StableRotation60(coord);
        child.transform.localRotation = Quaternion.Euler(0f, rotationStep * 60f, 0f);
    }

    private void RemoveExistingForestOverlay(HexTile tile)
    {
        if (tile == null)
            return;

        Transform oldCluster = tile.transform.Find(ForestChildNameCluster);
        if (oldCluster != null)
            Object.Destroy(oldCluster.gameObject);
    }

    private bool CanUseTileForForest(Vector2Int coord, HexTile tile)
    {
        if (tile == null || tile.TileType == null)
            return false;

        if (tile.gameObject.GetComponent<ProtectedHex>() != null)
            return false;

        if (owner.IsCoordForbidden(coord))
            return false;

        if (!IsGrassTile(tile))
            return false;

        if (owner.bonusAdjacencyProtected != null &&
            owner.bonusAdjacencyProtected.Contains(coord))
            return false;

        return true;
    }

    private bool IsGrassTile(HexTile tile)
    {
        if (tile == null || tile.TileType == null)
            return false;

        if (owner.gridConfig.grassTile != null &&
            tile.TileType == owner.gridConfig.grassTile)
            return true;

        string id = tile.TileType.tileId;
        string name = tile.TileType.tileName;

        return id == "1" || name == "grass";
    }

    private float SampleForestClusterMask01(Vector2Int coord)
    {
        Vector3 position = owner.CalculatePerfectPosition(coord.x, coord.y);

        float noiseX =
            (position.x + owner.gridConfig.forestClusterOffset.x) *
            owner.gridConfig.forestClusterMaskScale;

        float noiseZ =
            (position.z + owner.gridConfig.forestClusterOffset.y) *
            owner.gridConfig.forestClusterMaskScale;

        float value = NoiseUtility.FBm(
            noiseX,
            noiseZ,
            owner.gridConfig.forestClusterMaskOctaves,
            owner.gridConfig.forestClusterMaskPersistence,
            owner.gridConfig.forestClusterMaskLacunarity
        );

        value = NoiseUtility.ApplyContrast(
            value,
            owner.gridConfig.forestClusterMaskContrast
        );

        return Mathf.Clamp01(value);
    }

    public static bool HasForestOverlay(HexTile tile)
    {
        if (tile == null)
            return false;

        return tile.transform.Find(ForestChildName) != null ||
               tile.transform.Find(ForestChildNameCluster) != null;
    }
}