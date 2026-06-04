//using UnityEngine;
//using static HexGridGenerator;

//public class HeightGenerator
//{
//    private readonly HexGridGenerator owner;

//    public HeightGenerator(HexGridGenerator owner)
//    {
//        this.owner = owner;
//    }

//    public void ApplyHeights()
//    {
//        int changed = 0;
//        int skippedProtected = 0;
//        int skippedForbidden = 0;
//        int skippedNotGrass = 0;

//        foreach (var pair in owner.hexGrid)
//        {
//            Vector2Int coord = pair.Key;
//            HexTile tile = pair.Value;

//            if (tile == null)
//                continue;

//            if (tile.gameObject.GetComponent<ProtectedHex>() != null)
//            {
//                skippedProtected++;
//                continue;
//            }

//            if (owner.IsCoordForbidden(coord))
//            {
//                skippedForbidden++;
//                continue;
//            }

//            if (!IsGrassTile(tile))
//            {
//                skippedNotGrass++;
//                continue;
//            }

//            float height01 = SampleHeight01(coord);
//            float y = CalculateHeightY(height01);

//            Transform transform = tile.transform;

//            if (!Mathf.Approximately(transform.position.y, y))
//                changed++;

//            transform.position = new Vector3(
//                transform.position.x,
//                y,
//                transform.position.z
//            );
//        }

//        Debug.Log(
//            $"HeightGenerator: changed={changed}, " +
//            $"skippedProtected={skippedProtected}, " +
//            $"skippedForbidden={skippedForbidden}, " +
//            $"skippedNotGrass={skippedNotGrass}"
//        );
//    }

//    private float CalculateHeightY(float height01)
//    {
//        float hillThreshold = Mathf.Clamp01(owner.gridConfig.hillThreshold);
//        float mountainThreshold = Mathf.Clamp01(owner.gridConfig.mountainThreshold);
//        float peakThreshold = Mathf.Clamp01(owner.gridConfig.peakThreshold);

//        float step = owner.gridConfig.heightStep > 0f
//            ? owner.gridConfig.heightStep
//            : Mathf.Max(0.5f, owner.gridConfig.hexSize * 0.5f);

//        if (height01 >= peakThreshold)
//            return 1.5f * step;

//        if (height01 >= mountainThreshold)
//            return 1.0f * step;

//        if (height01 >= hillThreshold)
//            return 0.5f * step;

//        return 0f;
//    }

//    private float SampleHeight01(Vector2Int coord)
//    {
//        Vector3 position = owner.CalculatePerfectPosition(coord.x, coord.y);

//        float warpX = Mathf.PerlinNoise(position.x * 0.03f, position.z * 0.03f);
//        float warpZ = Mathf.PerlinNoise(
//            (position.x + 100f) * 0.03f,
//            (position.z + 100f) * 0.03f
//        );

//        float dx = (warpX - 0.5f) * 2f * owner.gridConfig.domainWarpStrength;
//        float dz = (warpZ - 0.5f) * 2f * owner.gridConfig.domainWarpStrength;

//        float noiseX =
//            (position.x + owner.gridConfig.heightOffset.x + dx) *
//            owner.gridConfig.heightScale;

//        float noiseZ =
//            (position.z + owner.gridConfig.heightOffset.y + dz) *
//            owner.gridConfig.heightScale;

//        return Mathf.Clamp01(FBm(noiseX, noiseZ));
//    }

//    private float FBm(float x, float y)
//    {
//        int octaves = Mathf.Max(1, owner.gridConfig.heightOctaves);

//        float amplitude = 1f;
//        float frequency = 1f;

//        float sum = 0f;
//        float normalization = 0f;

//        for (int octave = 0; octave < octaves; octave++)
//        {
//            sum += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
//            normalization += amplitude;

//            amplitude *= Mathf.Clamp01(owner.gridConfig.heightPersistence);
//            frequency *= Mathf.Max(1.0f, owner.gridConfig.heightLacunarity);
//        }

//        return normalization > 0f
//            ? sum / normalization
//            : 0f;
//    }

//    private bool IsGrassTile(HexTile tile)
//    {
//        if (tile == null || tile.TileType == null)
//            return false;

//        if (owner.gridConfig.grassTile != null &&
//            tile.TileType == owner.gridConfig.grassTile)
//            return true;

//        string id = tile.TileType.tileId;
//        string name = tile.TileType.tileName;

//        return id == "1" || name == "grass";
//    }
//}
