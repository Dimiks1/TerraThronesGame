using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public CardData SourceCard { get; private set; }
    public HexTile CurrentTile { get; private set; }

    public int CurrentHealth { get; private set; }
    public int Attack { get; private set; }
    public int Range { get; private set; }
    public int MoveRange { get; private set; }

    public void Initialize(CardData cardData, HexTile tile)
    {
        SourceCard = cardData;
        CurrentTile = tile;

        CurrentHealth = cardData.health;
        Attack = cardData.attack;
        Range = cardData.range;
        MoveRange = cardData.moveRange;

        Debug.Log($"[BattleUnit] Spawned unit from card: {cardData.cardId} / {cardData.cardName} on tile {tile.Coordinates}");
    }
}
