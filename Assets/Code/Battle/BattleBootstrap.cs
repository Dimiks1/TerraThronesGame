using System.Collections;
using UnityEngine;

public class BattleBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeckRuntime playerDeck;
    [SerializeField] private HandController handController;

    [Header("Starting Hand")]
    [SerializeField] private int startingHandSize = 5;

    private IEnumerator Start()
    {
        // Ждём один кадр, чтобы HexGridGenerator успел выполнить свой Start()
        // и карта точно была сгенерирована.
        yield return null;

        if (playerDeck == null)
        {
            Debug.LogError("[BattleBootstrap] playerDeck не назначен.");
            yield break;
        }

        if (handController == null)
        {
            Debug.LogError("[BattleBootstrap] handController не назначен.");
            yield break;
        }

        Debug.Log("[BattleBootstrap] Генерация карты должна быть завершена. Загружаю колоду...");

        playerDeck.LoadDeckFromSave();
        playerDeck.Shuffle();

        Debug.Log("[BattleBootstrap] ID карт в боевой колоде:");

        foreach (CardData card in playerDeck.DrawPile)
        {
            if (card == null)
                continue;

            Debug.Log($"- {card.cardId} / {card.cardName}");
        }

        handController.DrawStartingHand(startingHandSize);
    }
}