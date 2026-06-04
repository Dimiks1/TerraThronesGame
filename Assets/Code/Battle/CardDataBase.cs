using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance { get; private set; }

    [Header("All Cards In Game")]
    [SerializeField] private List<CardData> allCards = new List<CardData>();

    private readonly Dictionary<string, CardData> cardsById = new Dictionary<string, CardData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CardDatabase] На сцене уже есть CardDatabase. Удаляю дубликат.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        BuildDatabase();
    }

    private void BuildDatabase()
    {
        cardsById.Clear();

        foreach (CardData card in allCards)
        {
            if (card == null)
                continue;

            if (string.IsNullOrWhiteSpace(card.cardId))
            {
                Debug.LogWarning($"[CardDatabase] У карты '{card.cardName}' не заполнен cardId.");
                continue;
            }

            if (cardsById.ContainsKey(card.cardId))
            {
                Debug.LogWarning($"[CardDatabase] Дубликат cardId: {card.cardId}. Карта '{card.cardName}' пропущена.");
                continue;
            }

            cardsById.Add(card.cardId, card);
        }

        Debug.Log($"[CardDatabase] Загружено карт в базу: {cardsById.Count}");
    }

    public CardData GetCardById(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            return null;

        if (cardsById.TryGetValue(cardId, out CardData card))
            return card;

        Debug.LogWarning($"[CardDatabase] Карта с id '{cardId}' не найдена.");
        return null;
    }
}
