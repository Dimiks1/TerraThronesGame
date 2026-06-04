using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnYOffset = 0.08f;
    [SerializeField] private bool usePrefabRotation = true;
    [SerializeField] private Vector3 fallbackSpriteRotation = new Vector3(90f, 0f, 0f);

    private readonly Dictionary<HexTile, BattleUnit> unitsByTile = new Dictionary<HexTile, BattleUnit>();

    public bool IsTileOccupied(HexTile tile)
    {
        return tile != null && unitsByTile.ContainsKey(tile);
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

        Vector3 spawnPosition = tile.transform.position + Vector3.up * spawnYOffset;

        Quaternion rotation = usePrefabRotation
            ? cardData.unitPrefab.transform.rotation
            : Quaternion.Euler(fallbackSpriteRotation);

        GameObject unitObject = Instantiate(cardData.unitPrefab, spawnPosition, rotation);

        BattleUnit unit = unitObject.GetComponent<BattleUnit>();
        if (unit == null)
            unit = unitObject.AddComponent<BattleUnit>();

        unit.Initialize(cardData, tile);

        unitsByTile.Add(tile, unit);

        Debug.Log($"[UnitSpawner] Заспавнен юнит '{cardData.cardName}' на тайле {tile.Coordinates}");

        return true;
    }
}
