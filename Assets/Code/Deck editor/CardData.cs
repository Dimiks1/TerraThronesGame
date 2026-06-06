using UnityEngine;

public enum RarityType
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum CardType
{
    Unit,
    Building,
    Spell
}

public enum UnitMovementType
{
    Land,
    Naval,
    Flying
}

public enum SpellTargetType
{
    AnyTile,
    UnitOnly,
    EnemyUnitOnly,
    FriendlyUnitOnly
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;

    [Header("Card Type")]
    public CardType cardType = CardType.Unit;

    [Header("Card Info")]
    public string cardName;
    public int cost;
    public RarityType rarity;
    public Sprite artwork;

    [TextArea]
    public string description;

    [Header("Stats")]
    public int health;
    public int attack;
    public int range;
    public int moveRange;

    [Header("Board Prefab")]
    public GameObject unitPrefab;

    [Header("Movement")]
    public UnitMovementType movementType = UnitMovementType.Land;

    [Header("Spell")]
    public SpellTargetType spellTargetType = SpellTargetType.AnyTile;
    public int spellDamage;
}