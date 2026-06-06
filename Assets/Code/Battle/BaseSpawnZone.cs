using System.Collections.Generic;
using UnityEngine;

public class BaseSpawnZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridGenerator gridGenerator;
    [SerializeField] private UnitSpawner unitSpawner;

    [Header("Base Spawn")]
    [SerializeField] private int playerBaseSpawnRadius = 2;

    [Header("Barracks Spawn")]
    [SerializeField] private string barracksCardId = "barracks_001";
    [SerializeField] private int barracksSpawnRadius = 1;

    public bool CanSpawnPlayerUnit(HexTile tile)
    {
        if (tile == null)
            return false;

        if (IsNearPlayerBase(tile))
            return true;

        if (IsNearPlayerBarracks(tile))
            return true;

        return false;
    }

    private bool IsNearPlayerBase(HexTile tile)
    {
        if (gridGenerator == null)
        {
            gridGenerator = FindFirstObjectByType<HexGridGenerator>();

            if (gridGenerator == null)
            {
                Debug.LogError("[BaseSpawnZone] HexGridGenerator не найден.");
                return false;
            }
        }

        if (gridGenerator.greenBaseCoords == null || gridGenerator.greenBaseCoords.Count == 0)
            return false;

        foreach (Vector2Int baseCoord in gridGenerator.greenBaseCoords)
        {
            int distance = gridGenerator.AxialDistance(tile.Coordinates, baseCoord);

            if (distance <= playerBaseSpawnRadius)
                return true;
        }

        return false;
    }

    private bool IsNearPlayerBarracks(HexTile tile)
    {
        if (unitSpawner == null)
            return false;

        IReadOnlyList<BattleUnit> allUnits = unitSpawner.GetAllUnits();

        foreach (BattleUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            if (unit.SourceCard == null)
                continue;

            if (unit.CurrentTile == null)
                continue;

            if (unit.SourceCard.cardType != CardType.Building)
                continue;

            if (unit.SourceCard.cardId != barracksCardId)
                continue;

            int distance = gridGenerator.AxialDistance(tile.Coordinates, unit.CurrentTile.Coordinates);

            if (distance <= barracksSpawnRadius)
                return true;
        }

        return false;
    }
}