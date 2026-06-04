using System.Collections.Generic;
using UnityEngine;

public static class DeckStorage
{
    private const string SelectedDeckKey = "SelectedDeck";

    public static void SaveDeck(List<CardData> cards)
    {
        DeckSaveData saveData = new DeckSaveData();

        foreach (CardData card in cards)
        {
            if (card == null)
                continue;

            if (string.IsNullOrWhiteSpace(card.cardId))
            {
                Debug.LogWarning($"[DeckStorage] У карты '{card.cardName}' не заполнен cardId. Она не будет сохранена.");
                continue;
            }

            saveData.cardIds.Add(card.cardId);
        }

        string json = JsonUtility.ToJson(saveData);

        PlayerPrefs.SetString(SelectedDeckKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[DeckStorage] Колода сохранена. Карт: {saveData.cardIds.Count}");
        Debug.Log($"[DeckStorage] JSON: {json}");
    }

    public static DeckSaveData LoadDeck()
    {
        if (!PlayerPrefs.HasKey(SelectedDeckKey))
        {
            Debug.LogWarning("[DeckStorage] Сохранённая колода не найдена.");
            return new DeckSaveData();
        }

        string json = PlayerPrefs.GetString(SelectedDeckKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("[DeckStorage] Сохранённая колода пустая.");
            return new DeckSaveData();
        }

        DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json);

        if (saveData == null)
        {
            Debug.LogWarning("[DeckStorage] Не удалось прочитать сохранённую колоду.");
            return new DeckSaveData();
        }

        return saveData;
    }

    public static void ClearDeck()
    {
        PlayerPrefs.DeleteKey(SelectedDeckKey);
        PlayerPrefs.Save();

        Debug.Log("[DeckStorage] Сохранённая колода удалена.");
    }
}
