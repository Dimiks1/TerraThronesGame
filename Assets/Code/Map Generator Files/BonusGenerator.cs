using System.Collections.Generic;
using UnityEngine;
using static HexGridGenerator;

// Bonus places for 2 bases per player (for 1 and 3 bases in the future).
public class BonusPlacer
{
    private readonly HexGridGenerator owner;

    private readonly int bonusMinBaseDist = 7; //question mark
    private readonly int bonusMaxBaseDist = 11;
    private readonly int bonusMinBetween = 7;

    public BonusPlacer(HexGridGenerator owner)
    {
        this.owner = owner;
    }

    // PUBLIC ENTRY
    // We clear the state and generate bonuses for the 2-base mode.
    public void PlaceBonuses()
    {
        if (owner.gridConfig == null) return;

        // clearing
        if (owner.placedBonusCoords == null) owner.placedBonusCoords = new List<Vector2Int>();
        else owner.placedBonusCoords.Clear();

        GenerateBonusesForTwoBases();
    }


    // HIGH-LEVEL FLOW
    // Firstly we generate neutral bonus buildings, secondly we starting to generate bonuses for players bases.
    private void GenerateBonusesForTwoBases()
    {
        int n = owner.gridConfig.gridRadius;

        // 1) Neutral bonuses
        int spawnScale = (int)(n / 2.5); // for map scale
        TryPlaceBonusAt(new Vector2Int(-n + 5, 0), owner.gridConfig.bonusNeutralTile);
        TryPlaceBonusAt(new Vector2Int(n - 5, 0), owner.gridConfig.bonusNeutralTile);

        // 2) Bonuses for green bases
        if (owner.greenBaseCoords != null && owner.greenBaseCoords.Count == 2)
        {
            GenerateBonusesForBase(owner.greenBaseCoords[0], isBottomBase: true, isLeftBase: true, owner.gridConfig.bonusNeutralTile);
            GenerateBonusesForBase(owner.greenBaseCoords[1], isBottomBase: true, isLeftBase: false, owner.gridConfig.bonusNeutralTile);
        }

        // 3) Bonuses for yellow bases
        if (owner.yellowBaseCoords != null && owner.yellowBaseCoords.Count == 2)
        {
            GenerateBonusesForBase(owner.yellowBaseCoords[0], isBottomBase: false, isLeftBase: true, owner.gridConfig.bonusNeutralTile);
            GenerateBonusesForBase(owner.yellowBaseCoords[1], isBottomBase: false, isLeftBase: false, owner.gridConfig.bonusNeutralTile);
        }
    }

    // For one base we calculate 2 centers for bonus buildings with radius of 2.
    private void GenerateBonusesForBase(Vector2Int baseCoord, bool isBottomBase, bool isLeftBase, HexTileType bonusType)
    {
        if (bonusType == null) return;

        int n = owner.gridConfig.gridRadius;
        foreach (var target in ComputeBonusTargets(n, isBottomBase, isLeftBase))
            FindAndPlaceBonusInZone(target, radius: 2, baseCoord, bonusType);
    }

    // CORE PLACEMENT
    // Searches for a valid tile within a hex radius around the centerthe using axial distance logic and applies a bonus.
    // Uses reservoir sampling; does not create candidate lists.
    private void FindAndPlaceBonusInZone(Vector2Int center, int radius, Vector2Int baseCoord, HexTileType bonusType)
    {
        if (bonusType == null) return;

        int seen = 0;
        Vector2Int chosen = default;

        for (int dq = -radius; dq <= radius; dq++)
        {
            int rMin = Mathf.Max(-radius, -dq - radius);
            int rMax = Mathf.Min(radius, -dq + radius);

            for (int dr = rMin; dr <= rMax; dr++)
            {
                var c = new Vector2Int(center.x + dq, center.y + dr);
                if (!IsValidBonusLocation(c, baseCoord)) continue;

                // reservoir sampling
                seen++;
                if (Random.Range(0, seen) == 0) chosen = c;
            }
        }

        if (seen > 0)
            TryPlaceBonusAt(chosen, bonusType);
    }

    //Placing bonus tile, adding <ProtectedHex> component and block the neighbors as well.
    private void TryPlaceBonusAt(Vector2Int coord, HexTileType tileType)
    {
        if (tileType == null) return;

        owner.ReplaceHexTile(coord, tileType);
        owner.placedBonusCoords.Add(coord);

        owner.hexGrid[coord].gameObject.AddComponent<ProtectedHex>();

        // We using bonusAdjacencyProtected instead of ProtecteHex because we want to replace this tiles in the future,
        // but not for rivers and lakes. Even if the game placing roads first instead of roads/lakes, maybe in future
        // we will want to change that.
        foreach (var d in hexDirs)
        {
            var neigh = new Vector2Int(coord.x + d.x, coord.y + d.y);
            if (owner.hexGrid.ContainsKey(neigh))
                owner.bonusAdjacencyProtected.Add(neigh);
        }
    }

    // Full validation of the bonus tile (defense/barriers/distances).
    private bool IsValidBonusLocation(Vector2Int coord, Vector2Int baseCoord)
    {
        if (!owner.hexGrid.TryGetValue(coord, out var tile) || tile == null)
            return false;

        if (tile.TryGetComponent<ProtectedHex>(out _))
            return false;

        if (owner.IsForbiddenTile(tile))
            return false;

        int d = owner.AxialDistance(coord, baseCoord);
        if (d < bonusMinBaseDist || d > bonusMaxBaseDist)
            return false;

        var placed = owner.placedBonusCoords;
        if (placed != null && placed.Count > 0)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (owner.AxialDistance(coord, placed[i]) < bonusMinBetween)
                    return false;
            }
        }
        return true;
    }

    // GEOMETRY (targets) (calculating centers).
    private static IEnumerable<Vector2Int> ComputeBonusTargets(int n, bool isBottom, bool isLeft)
    {
        int r1 = -n / 2 + 2;
        int r2 = -n / 2 + 3;

        int a1 = n / 4;
        int a2 = n / 2 + 2;

        // left: q = (-n - r) + a; right: q = n - a
        Vector2Int p1 = new Vector2Int(isLeft ? (-n - r1) + a1 : n - a1, r1);
        Vector2Int p2 = new Vector2Int(isLeft ? (-n - r2) + a2 : n - a2, r2);

        if (!isBottom)
        {
            p1 = new Vector2Int(-p1.x, -p1.y);
            p2 = new Vector2Int(-p2.x, -p2.y);
        }

        yield return p1;
        yield return p2;
    }
}




