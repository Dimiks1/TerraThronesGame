using System.Collections.Generic;
using UnityEngine;

public class BorderGenerator
{
    private readonly HexGridGenerator owner;

    public BorderGenerator(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    public void GenerateBorders()
    {
        if (owner.gridConfig == null)
            return;

        if (!HasAnyBorderTile())
            return;

        int radius = owner.gridConfig.gridRadius;

        if (radius <= 0)
            return;

        var coords = new List<Vector2Int>(owner.hexGrid.Keys);

        foreach (Vector2Int coord in coords)
        {
            int distance = owner.AxialDistance(coord);

            if (distance == radius)
            {
                TryPlaceBorder(coord, PickVariant(
                    owner.gridConfig.borderOuterVariants,
                    owner.gridConfig.borderOuterTile
                ));
            }
            else if (distance == radius - 1)
            {
                TryPlaceBorder(coord, PickVariant(
                    owner.gridConfig.borderInnerVariants,
                    owner.gridConfig.borderInnerTile
                ));
            }
        }
    }

    private bool HasAnyBorderTile()
    {
        return owner.gridConfig.borderOuterTile != null ||
               owner.gridConfig.borderInnerTile != null ||
               HasVariants(owner.gridConfig.borderOuterVariants) ||
               HasVariants(owner.gridConfig.borderInnerVariants);
    }

    private static bool HasVariants(HexTileType[] variants)
    {
        return variants != null && variants.Length > 0;
    }

    private static HexTileType PickVariant(HexTileType[] variants, HexTileType fallback)
    {
        if (variants != null && variants.Length > 0)
        {
            int index = Random.Range(0, variants.Length);
            return variants[index];
        }

        return fallback;
    }

    private void TryPlaceBorder(Vector2Int coord, HexTileType borderTile)
    {
        if (borderTile == null)
            return;

        if (!owner.hexGrid.TryGetValue(coord, out HexTile existing) || existing == null)
            return;

        if (existing.gameObject.GetComponent<HexGridGenerator.ProtectedHex>() != null)
            return;

        if (existing.TileType == owner.gridConfig.greenBaseTile ||
            existing.TileType == owner.gridConfig.yellowBaseTile)
            return;

        owner.ReplaceHexTile(coord, borderTile);

        if (owner.hexGrid.TryGetValue(coord, out HexTile newBorderTile) &&
            newBorderTile != null &&
            newBorderTile.gameObject.GetComponent<HexGridGenerator.ProtectedHex>() == null)
        {
            newBorderTile.gameObject.AddComponent<HexGridGenerator.ProtectedHex>();
        }
    }
}
