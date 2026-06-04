using UnityEngine;

public enum RarityType
{
    Common,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;

    [Header("Card Info")]
    public string cardName;
    public int cost;
    public int health;
    public int attack;
    public int range;
    public int moveRange;
    public RarityType rarity;
    public Sprite artwork;

    [TextArea]
    public string description;

    [Header("Gameplay")]
    public GameObject unitPrefab;
}