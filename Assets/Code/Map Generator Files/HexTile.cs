using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    public Vector2Int Coordinates { get; private set; }
    public HexTileType TileType { get; private set; }

    public int MovementCost => TileType.movementCost;
    public bool IsWalkable => TileType.isWalcable;

    public void Initialize(Vector2Int coordinates, HexTileType tileType)
    {
        Coordinates = coordinates;
        TileType = tileType;
    }
}
