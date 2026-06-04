// BasePlacer.cs
using System.Collections.Generic;
using UnityEngine;
using static HexGridGenerator;

public class BasePlacer
{

    private HexGridGenerator owner;
    public BasePlacer(HexGridGenerator owner) { this.owner = owner; }

    public void PlacePlayerBases()
    {
        owner.greenBaseCoords.Clear();
        owner.yellowBaseCoords.Clear();

        int n = owner.gridConfig.gridRadius;
        int quarter = n / 4;

        // I hardcoded this coordinates because it's important for balance to make bases in the same range from all
        // important places on the map later. It's not a big deal to make it pretty "random" in radius 1, for example,
        // (we have this logic in bonus generator) but for the first iteration we will just hardcode this coordinates to test gamedesign
        if (owner.gridConfig.basesPerPlayer == 1)
        {
            // Green base at the bottom
            owner.greenBaseCoords.Add(new Vector2Int(n / 2 - 2, -n + 4));
            // Yellow base on top
            owner.yellowBaseCoords.Add(new Vector2Int(-n / 2 + 2, n - 4));
        }
        else if (owner.gridConfig.basesPerPlayer == 2)
        {

            owner.greenBaseCoords.Add(new Vector2Int(0, -n + 4));
            owner.greenBaseCoords.Add(new Vector2Int(n - 4, -n + 4));

            owner.yellowBaseCoords.Add(new Vector2Int(0, n - 4));
            owner.yellowBaseCoords.Add(new Vector2Int(-n + 4, n - 4));
        }
        else if (owner.gridConfig.basesPerPlayer == 3)
        {

            owner.greenBaseCoords.Add(new Vector2Int(0, -n + 4));
            owner.greenBaseCoords.Add(new Vector2Int(n - 4, -n + 4));
            owner.greenBaseCoords.Add(new Vector2Int(n / 2 - 2, -n + 4));

            owner.yellowBaseCoords.Add(new Vector2Int(0, n - 4));
            owner.yellowBaseCoords.Add(new Vector2Int(-n + 4, n - 4));
            owner.yellowBaseCoords.Add(new Vector2Int(-n / 2 + 2, n - 4));
        }

        // Placing bases
        foreach (Vector2Int coord in owner.greenBaseCoords)
        {

            owner.ReplaceHexTile(coord, owner.gridConfig.greenBaseTile);
            ProtectBaseAndNeighbors(coord); // and protect neighbors for future structures on it
        }

        foreach (Vector2Int coord in owner.yellowBaseCoords)
        {
            owner.ReplaceHexTile(coord, owner.gridConfig.yellowBaseTile);
            ProtectBaseAndNeighbors(coord);
        }
    }
    private void ProtectBaseAndNeighbors(Vector2Int baseCoord)
    {
        if (!owner.hexGrid.ContainsKey(baseCoord)) return;

        List<Vector2Int> protectCoords = new List<Vector2Int> { baseCoord };
        foreach (var d in hexDirs)
            protectCoords.Add(baseCoord + d);

        foreach (var c in protectCoords)
        {
            if (!owner.hexGrid.ContainsKey(c)) continue;
            var tile = owner.hexGrid[c];
            if (tile != null && tile.GetComponent<ProtectedHex>() == null)
            {
                tile.gameObject.AddComponent<ProtectedHex>();
            }
        }
    }
}


