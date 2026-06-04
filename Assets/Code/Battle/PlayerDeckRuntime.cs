using System.Collections.Generic;
using UnityEngine;

public class PlayerDeckRuntime : MonoBehaviour
{
    public List<CardData> DrawPile { get; private set; } = new List<CardData>();
    public List<CardData> Hand { get; private set; } = new List<CardData>();
    public List<CardData> DiscardPile { get; private set; } = new List<CardData>();

    public void LoadDeckFromSave()
    {
        DrawPile.Clear();
        Hand.Clear();
        DiscardPile.Clear();

        DeckSaveData saveData = DeckStorage.LoadDeck();

        Debug.Log($"[PlayerDeckRuntime] Найдено ID в сохранении: {saveData.cardIds.Count}");

        foreach (string cardId in saveData.cardIds)
        {
            CardData card = CardDatabase.Instance.GetCardById(cardId);

            if (card == null)
            {
                Debug.LogWarning($"[PlayerDeckRuntime] Не удалось загрузить карту с id: {cardId}");
                continue;
            }

            DrawPile.Add(card);
            Debug.Log($"[PlayerDeckRuntime] Загружена карта: {card.cardId} / {card.cardName}");
        }

        Debug.Log($"[PlayerDeckRuntime] Колода загружена. Карт в draw pile: {DrawPile.Count}");
    }

    public void Shuffle()
    {
        for (int i = 0; i < DrawPile.Count; i++)
        {
            int randomIndex = Random.Range(i, DrawPile.Count);

            CardData temp = DrawPile[i];
            DrawPile[i] = DrawPile[randomIndex];
            DrawPile[randomIndex] = temp;
        }

        Debug.Log("[PlayerDeckRuntime] Колода перемешана.");
    }

    public CardData DrawCard()
    {
        if (DrawPile.Count == 0)
        {
            Debug.LogWarning("[PlayerDeckRuntime] DrawPile пуст.");
            return null;
        }

        CardData card = DrawPile[0];
        DrawPile.RemoveAt(0);
        Hand.Add(card);

        Debug.Log($"[PlayerDeckRuntime] Взята карта в руку: {card.cardId} / {card.cardName}");

        return card;
    }

    public List<CardData> DrawCards(int count)
    {
        List<CardData> drawnCards = new List<CardData>();

        for (int i = 0; i < count; i++)
        {
            CardData card = DrawCard();

            if (card == null)
                break;

            drawnCards.Add(card);
        }

        return drawnCards;
    }

    public void PlayCardFromHand(CardData card)
    {
        if (card == null)
            return;

        if (Hand.Remove(card))
        {
            DiscardPile.Add(card);
            Debug.Log($"[PlayerDeckRuntime] Карта разыграна: {card.cardId} / {card.cardName}");
        }
        else
        {
            Debug.LogWarning($"[PlayerDeckRuntime] Карта не найдена в руке: {card.cardId} / {card.cardName}");
        }
    }
}
