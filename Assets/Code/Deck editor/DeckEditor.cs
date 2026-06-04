using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DeckEditor : MonoBehaviour
{
    public Transform allCardsContent;
    public Transform deckContent;
    public GameObject cardPrefab;
    public GameObject deckCardPrefab;
    public Button saveButton;
    public Button backButton;

    public float forcedWidth = 120f;
    public float forcedHeight = 40f;

    public List<CardData> allCards;
    private List<CardData> playerDeck = new List<CardData>();

    void Start()
    {
        if (allCardsContent == null || cardPrefab == null)
        {
            Debug.LogError("Assign allCardsContent and cardPrefab in inspector!");
            return;
        }

        foreach (CardData cardData in allCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, allCardsContent, false);
            CardUI ui = cardObj.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.Setup(cardData); // <- ключевая строка: заполняем artwork из cardData
                Debug.Log($"Created button for {cardData.cardName}, artwork: {(cardData.artwork ? cardData.artwork.name : "NULL")}");

            }
            else
            {
                Debug.LogWarning("CardPrefab missing CardUI component!");
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                CardData copy = cardData; // безопасное замыкание
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => AddCardToDeck(copy));
            }
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(() =>
            {
                Debug.Log("Возврат в главное меню!");
                SceneManager.LoadScene("MainMenu");
            });
        }
    }

    public void AddCardToDeck(CardData cardData)
    {
        playerDeck.Add(cardData);

        var go = Instantiate(deckCardPrefab, deckContent, false);
        var ui = go.GetComponent<DeckCardUI>();
        if (ui) ui.Setup(cardData, this);

        // по желанию: прокрутить к началу
        var scroll = deckContent.GetComponentInParent<ScrollRect>();
        if (scroll) scroll.verticalNormalizedPosition = 1f;
    }

    public void RemoveCardFromDeck(DeckCardUI ui)
    {
        if (ui == null) return;
        playerDeck.Remove(ui.Data);
        Destroy(ui.gameObject);
        Debug.Log("Карта удалилась!");
    }

    void SaveDeck()
    {
        Debug.Log("Сохраняем колоду...");
        foreach (CardData card in playerDeck)
            Debug.Log(card.cardName);
    }
}


