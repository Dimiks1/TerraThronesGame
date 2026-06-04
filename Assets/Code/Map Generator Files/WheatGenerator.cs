using UnityEngine;
using static HexGridGenerator;

public class WheatGenerator
{
    public const string WheatChildName = "WheatOverlay";

    private readonly HexGridGenerator owner;

    public WheatGenerator(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    public void GenerateWheat()
    {
        if (owner.gridConfig == null)
        {
            Debug.LogWarning("WheatGenerator: gridConfig is null.");
            return;
        }

        if (!owner.gridConfig.useWheatNoise)
            return;

        if (owner.gridConfig.wheatOverlay == null)
            return;

        if (owner.hexGrid == null || owner.hexGrid.Count == 0)
        {
            Debug.LogWarning("WheatGenerator: hexGrid is empty.");
            return;
        }

        int placedCount = 0;

        float threshold = Mathf.Clamp01(owner.gridConfig.wheatThreshold);

        foreach (var pair in owner.hexGrid)
        {
            Vector2Int coord = pair.Key;
            HexTile tile = pair.Value;

            if (!CanUseTileForWheat(coord, tile))
                continue;

            RemoveExistingWheatOverlay(tile);

            float value = SampleWheat01(coord);

            if (value < threshold)
                continue;

            PlaceWheatOverlay(tile, coord);
            placedCount++;
        }

        Debug.Log($"WheatGenerator: placed={placedCount}.");
    }

    private bool CanUseTileForWheat(Vector2Int coord, HexTile tile)
    {
        if (tile == null || tile.TileType == null)
            return false;

        if (tile.gameObject.GetComponent<ProtectedHex>() != null)
            return false;

        if (owner.IsCoordForbidden(coord))
            return false;

        if (!IsGrassTile(tile))
            return false;

        if (ForestGenerator.HasForestOverlay(tile))
            return false;

        return true;
    }

    private void RemoveExistingWheatOverlay(HexTile tile)
    {
        Transform oldWheat = tile.transform.Find(WheatChildName);

        if (oldWheat != null)
            Object.Destroy(oldWheat.gameObject);
    }

    private void PlaceWheatOverlay(HexTile tile, Vector2Int coord)
    {
        GameObject child = Object.Instantiate(owner.gridConfig.wheatOverlay, tile.transform);

        child.name = WheatChildName;
        child.transform.localPosition = new Vector3(
            0f,
            owner.gridConfig.wheatOverlayY,
            0f
        );

        int rotationStep = NoiseUtility.StableRotation60(coord);
        child.transform.localRotation = Quaternion.Euler(0f, rotationStep * 60f, 0f);
    }

    private float SampleWheat01(Vector2Int coord)
    {
        Vector3 position = owner.CalculatePerfectPosition(coord.x, coord.y);

        float noiseX =
            (position.x + owner.gridConfig.forestOffset.x) *
            owner.gridConfig.forestScale;

        float noiseZ =
            (position.z + owner.gridConfig.forestOffset.y) *
            owner.gridConfig.forestScale;

        return Mathf.Clamp01(
            NoiseUtility.FBm(
                noiseX,
                noiseZ,
                owner.gridConfig.forestOctaves,
                owner.gridConfig.forestPersistence,
                owner.gridConfig.forestLacunarity
            )
        );
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
}