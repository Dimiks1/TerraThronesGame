using UnityEngine;

public enum RarityType
{
    Common,     // Обычная
    Rare,       // Редкая
    Epic,       // Эпическая
    Legendary   // Легендарная
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card")]
public class CardData : ScriptableObject
{
    public string cardName;      // Имя карты
    public int cost;             // Стоимость
    public int health;           // Здоровье
    public int attack;           // Атака
    public int range;            // Дальность атаки
    public int moveRange;        // Скорость/продвижение
    public RarityType rarity;    // Редкость (легендарность)
    public Sprite artwork;       // Картинка
    [TextArea]
    public string description;   // Описание
}

