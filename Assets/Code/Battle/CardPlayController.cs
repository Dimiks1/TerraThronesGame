using UnityEngine;

public class CardPlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private PlayerEconomy playerEconomy;
    [SerializeField] private BaseSpawnZone baseSpawnZone;
    [SerializeField] private TurnManager turnManager;

    public bool CanPlayCard(CardData cardData, HexTile targetTile)
    {
        if (turnManager != null && !turnManager.IsPlayerTurn())
            return false;

        if (cardData == null)
            return false;

        if (targetTile == null)
            return false;

        if (playerEconomy == null)
        {
            Debug.LogError("[CardPlayController] playerEconomy не назначен.");
            return false;
        }

        if (!playerEconomy.CanAfford(cardData.cost))
            return false;

        switch (cardData.cardType)
        {
            case CardType.Unit:
                return CanPlayUnit(cardData, targetTile);

            case CardType.Building:
                return CanPlayBuilding(cardData, targetTile);

            case CardType.Spell:
                return CanPlaySpell(cardData, targetTile);

            default:
                return false;
        }
    }

    public bool TryPlayCard(CardData cardData, HexTile targetTile)
    {
        if (!CanPlayCard(cardData, targetTile))
        {
            Debug.Log("[CardPlayController] Карту нельзя разыграть на этот тайл.");
            return false;
        }

        if (!playerEconomy.TrySpendGold(cardData.cost))
            return false;

        bool success = false;

        switch (cardData.cardType)
        {
            case CardType.Unit:
                success = PlayUnit(cardData, targetTile);
                break;

            case CardType.Building:
                success = PlayBuilding(cardData, targetTile);
                break;

            case CardType.Spell:
                success = PlaySpell(cardData, targetTile);
                break;
        }

        if (!success)
        {
            Debug.LogWarning("[CardPlayController] Золото списалось, но карта не разыгралась. Позже стоит добавить refund.");
            return false;
        }

        Debug.Log($"[CardPlayController] Разыграна карта: {cardData.cardName}, type: {cardData.cardType}");
        return true;
    }

    private bool CanPlayUnit(CardData cardData, HexTile targetTile)
    {
        if (cardData.unitPrefab == null)
            return false;

        if (!targetTile.IsWalkable)
            return false;

        if (unitSpawner != null && unitSpawner.IsTileOccupied(targetTile))
            return false;

        if (baseSpawnZone == null)
        {
            Debug.LogError("[CardPlayController] baseSpawnZone не назначен.");
            return false;
        }

        if (!baseSpawnZone.CanSpawnPlayerUnit(targetTile))
            return false;

        return true;
    }

    private bool CanPlayBuilding(CardData cardData, HexTile targetTile)
    {
        if (cardData.unitPrefab == null)
            return false;

        if (!targetTile.IsWalkable)
            return false;

        if (unitSpawner != null && unitSpawner.IsTileOccupied(targetTile))
            return false;

        if (baseSpawnZone == null)
        {
            Debug.LogError("[CardPlayController] baseSpawnZone не назначен.");
            return false;
        }

        if (!baseSpawnZone.CanSpawnPlayerUnit(targetTile))
            return false;

        return true;
    }

    private bool CanPlaySpell(CardData cardData, HexTile targetTile)
    {
        // Для MVP разрешаем каст на любую клетку карты.
        // Даже если там нет юнита — карта просто сработает в пустоту.
        return targetTile != null;
    }

    private bool PlayUnit(CardData cardData, HexTile targetTile)
    {
        if (unitSpawner == null)
        {
            Debug.LogError("[CardPlayController] unitSpawner не назначен.");
            return false;
        }

        return unitSpawner.TrySpawnUnit(cardData, targetTile);
    }

    private bool PlayBuilding(CardData cardData, HexTile targetTile)
    {
        if (unitSpawner == null)
        {
            Debug.LogError("[CardPlayController] unitSpawner не назначен.");
            return false;
        }

        return unitSpawner.TrySpawnUnit(cardData, targetTile);
    }

    private bool PlaySpell(CardData cardData, HexTile targetTile)
    {
        if (unitSpawner == null)
        {
            Debug.LogError("[CardPlayController] unitSpawner не назначен.");
            return false;
        }

        BattleUnit unit = unitSpawner.GetUnitAtTile(targetTile);

        if (unit != null)
        {
            unit.TakeDamage(cardData.spellDamage);

            if (unit.CurrentHealth <= 0)
                unitSpawner.RemoveUnit(unit);

            Debug.Log($"[CardPlayController] Заклинание '{cardData.cardName}' нанесло {cardData.spellDamage} урона.");
        }
        else
        {
            Debug.Log($"[CardPlayController] Заклинание '{cardData.cardName}' применено по пустой клетке.");
        }

        // Если у спелла есть визуальный prefab — можно создать временный эффект.
        if (cardData.unitPrefab != null)
        {
            Vector3 position = targetTile.transform.position + Vector3.up * 0.5f;
            GameObject effect = Instantiate(cardData.unitPrefab, position, cardData.unitPrefab.transform.rotation);
            Destroy(effect, 2f);
        }

        return true;
    }
}