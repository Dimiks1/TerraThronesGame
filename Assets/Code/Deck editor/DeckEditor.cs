using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeckEditor : MonoBehaviour
{
    [Header("UI")]
    public Transform allCardsContent;
    public Transform deckContent;
    public GameObject cardPrefab;
    public GameObject deckCardPrefab;
    public Button saveButton;
    public Button backButton;

    [Header("Card Size")]
    public float forcedWidth = 120f;
    public float forcedHeight = 40f;

    [Header("Cards")]
    public List<CardData> allCards = new List<CardData>();

    private readonly List<CardData> playerDeck = new List<CardData>();

    private void Start()
    {
        if (allCardsContent == null || cardPrefab == null)
        {
            Debug.LogError("[DeckEditor] Assign allCardsContent and cardPrefab in inspector!");
            return;
        }

        CreateAllCardButtons();

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveDeck);
        }
        else
        {
            Debug.LogWarning("[DeckEditor] SaveButton не назначен в инспекторе.");
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(BackToMainMenu);
        }
        else
        {
            Debug.LogWarning("[DeckEditor] BackButton не назначен в инспекторе.");
        }
    }

    private void CreateAllCardButtons()
    {
        foreach (CardData cardData in allCards)
        {
            if (cardData == null)
                continue;

            GameObject cardObj = Instantiate(cardPrefab, allCardsContent, false);

            CardUI ui = cardObj.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.Setup(cardData);
                Debug.Log($"[DeckEditor] Created button for {cardData.cardName}, id: {cardData.cardId}");
            }
            else
            {
                Debug.LogWarning("[DeckEditor] CardPrefab missing CardUI component!");
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                CardData copy = cardData;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => AddCardToDeck(copy));
            }
        }
    }

    public void AddCardToDeck(CardData cardData)
    {
        if (cardData == null)
            return;

        playerDeck.Add(cardData);

        if (deckCardPrefab != null && deckContent != null)
        {
            GameObject go = Instantiate(deckCardPrefab, deckContent, false);

            DeckCardUI ui = go.GetComponent<DeckCardUI>();
            if (ui != null)
                ui.Setup(cardData, this);
        }

        ScrollRect scroll = deckContent != null ? deckContent.GetComponentInParent<ScrollRect>() : null;
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;

        Debug.Log($"[DeckEditor] Добавлена карта: {cardData.cardName}, id: {cardData.cardId}");
    }

    public void RemoveCardFromDeck(DeckCardUI ui)
    {
        if (ui == null)
            return;

        CardData card = ui.Data;

        if (card != null)
        {
            playerDeck.Remove(card);
            Debug.Log($"[DeckEditor] Удалена карта: {card.cardName}, id: {card.cardId}");
        }

        Destroy(ui.gameObject);
    }

    private void SaveDeck()
    {
        if (playerDeck.Count == 0)
        {
            Debug.LogWarning("[DeckEditor] Колода пустая. Сохранять нечего.");
            return;
        }

        DeckStorage.SaveDeck(playerDeck);

        Debug.Log("[DeckEditor] Сохранённые карты:");
        foreach (CardData card in playerDeck)
        {
            if (card == null)
                continue;

            Debug.Log($"- {card.cardId} / {card.cardName}");
        }
    }

    private void BackToMainMenu()
    {
        Debug.Log("[DeckEditor] Возврат в главное меню.");
        SceneManager.LoadScene("MainMenu");
    }
}