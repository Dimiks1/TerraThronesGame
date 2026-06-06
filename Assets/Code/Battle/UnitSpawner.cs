using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnYOffset = 0.25f;
    [SerializeField] private bool usePrefabRotation = true;
    [SerializeField] private Vector3 fallbackSpriteRotation = new Vector3(-90f, 0f, 0f);

    private readonly Dictionary<HexTile, BattleUnit> unitsByTile = new Dictionary<HexTile, BattleUnit>();
    private readonly List<BattleUnit> allUnits = new List<BattleUnit>();

    public bool IsTileOccupied(HexTile tile)
    {
        return tile != null && unitsByTile.ContainsKey(tile);
    }

    public BattleUnit GetUnitAtTile(HexTile tile)
    {
        if (tile == null)
            return null;

        return unitsByTile.TryGetValue(tile, out BattleUnit unit) ? unit : null;
    }

    public bool TrySpawnUnit(CardData cardData, HexTile tile)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[UnitSpawner] cardData == null");
            return false;
        }

        if (tile == null)
        {
            Debug.LogWarning("[UnitSpawner] tile == null");
            return false;
        }

        if (cardData.unitPrefab == null)
        {
            Debug.LogWarning($"[UnitSpawner] У карты '{cardData.cardName}' не назначен unitPrefab.");
            return false;
        }

        if (IsTileOccupied(tile))
        {
            Debug.LogWarning($"[UnitSpawner] Тайла {tile.Coordinates} уже занят.");
            return false;
        }

        Vector3 spawnPosition = GetUnitWorldPosition(tile);

        Quaternion rotation = usePrefabRotation
            ? cardData.unitPrefab.transform.rotation
            : Quaternion.Euler(fallbackSpriteRotation);

        GameObject unitObject = Instantiate(cardData.unitPrefab, spawnPosition, rotation);

        EnsureUnitCollider(unitObject);

        BattleUnit unit = unitObject.GetComponent<BattleUnit>();
        if (unit == null)
            unit = unitObject.AddComponent<BattleUnit>();

        unit.Initialize(cardData, tile);

        unitsByTile.Add(tile, unit);
        if (!allUnits.Contains(unit))
            allUnits.Add(unit);

        Debug.Log($"[UnitSpawner] Заспавнен юнит '{cardData.cardName}' на тайле {tile.Coordinates}");

        return true;
    }

    public bool TryMoveUnit(BattleUnit unit, HexTile targetTile)
    {
        if (unit == null || targetTile == null)
            return false;

        HexTile oldTile = unit.CurrentTile;

        if (oldTile == null)
            return false;

        if (IsTileOccupied(targetTile))
        {
            Debug.Log("[UnitSpawner] Нельзя переместиться: целевой тайл занят.");
            return false;
        }

        if (!targetTile.IsWalkable)
        {
            Debug.Log("[UnitSpawner] Нельзя переместиться: целевой тайл непроходимый.");
            return false;
        }

        if (unitsByTile.ContainsKey(oldTile))
            unitsByTile.Remove(oldTile);

        unitsByTile[targetTile] = unit;

        unit.SetCurrentTile(targetTile);
        unit.transform.position = GetUnitWorldPosition(targetTile);
        unit.MarkMoved();

        Debug.Log($"[UnitSpawner] Юнит '{unit.SourceCard.cardName}' перемещён: {oldTile.Coordinates} -> {targetTile.Coordinates}");

        return true;
    }

    private void EnsureUnitCollider(GameObject unitObject)
    {
        if (unitObject == null)
            return;

        Collider existingCollider = unitObject.GetComponentInChildren<Collider>();

        if (existingCollider != null)
            return;

        BoxCollider boxCollider = unitObject.AddComponent<BoxCollider>();
        boxCollider.center = new Vector3(0f, 0.5f, 0f);
        boxCollider.size = new Vector3(1.5f, 1.5f, 1.5f);

        Debug.LogWarning($"[UnitSpawner] У юнита '{unitObject.name}' не было Collider. BoxCollider добавлен автоматически.");
    }

    public void ResetAllPlayerUnitsTurnState()
    {
        foreach (BattleUnit unit in allUnits)
        {
            if (unit == null)
                continue;

            unit.ResetTurnState();
        }

        Debug.Log($"[UnitSpawner] Сброшено состояние хода у юнитов: {allUnits.Count}");
    }

    public void RemoveUnit(BattleUnit unit)
    {
        if (unit == null)
            return;

        HexTile tile = unit.CurrentTile;

        if (tile != null && unitsByTile.ContainsKey(tile))
            unitsByTile.Remove(tile);

        if (allUnits.Contains(unit))
            allUnits.Remove(unit);

        if (unit.gameObject != null)
            Destroy(unit.gameObject);

        Debug.Log("[UnitSpawner] Юнит удалён с карты.");
    }

    public IReadOnlyList<BattleUnit> GetAllUnits()
    {
        return allUnits;
    }

    public Vector3 GetUnitWorldPosition(HexTile tile)
    {
        return tile.transform.position + Vector3.up * spawnYOffset;
    }
}