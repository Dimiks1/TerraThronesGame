using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public CardData SourceCard { get; private set; }
    public HexTile CurrentTile { get; private set; }

    public int CurrentHealth { get; private set; }
    public int Attack { get; private set; }
    public int Range { get; private set; }
    public int MoveRange { get; private set; }

    public bool HasMovedThisTurn { get; private set; }

    public bool IsUnit => SourceCard != null && SourceCard.cardType == CardType.Unit;
    public bool IsBuilding => SourceCard != null && SourceCard.cardType == CardType.Building;
    public bool CanMove => IsUnit && MoveRange > 0;

    public void Initialize(CardData cardData, HexTile tile)
    {
        SourceCard = cardData;
        CurrentTile = tile;

        CurrentHealth = cardData.health;
        Attack = cardData.attack;
        Range = cardData.range;
        MoveRange = cardData.moveRange;

        HasMovedThisTurn = false;

        Debug.Log($"[BattleUnit] Spawned {cardData.cardType}: {cardData.cardId} / {cardData.cardName} on tile {tile.Coordinates}");
    }

    public void SetCurrentTile(HexTile tile)
    {
        CurrentTile = tile;
    }

    public void MarkMoved()
    {
        HasMovedThisTurn = true;
    }

    public void ResetTurnState()
    {
        HasMovedThisTurn = false;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        CurrentHealth -= amount;

        Debug.Log($"[BattleUnit] {SourceCard.cardName} получил урон: {amount}. HP осталось: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Debug.Log($"[BattleUnit] {SourceCard.cardName} погиб/разрушен.");
        }
    }
}