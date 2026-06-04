using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDeckRuntime playerDeck;
    [SerializeField] private Transform handRoot;
    [SerializeField] private BattleHandCardView cardPrefab;

    [Header("Gameplay References")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private TileHighlighter tileHighlighter;

    [Header("Drag UI")]
    [SerializeField] private Transform dragLayer;

    [Header("Card Layout")]
    [SerializeField] private Vector2 cardSize = new Vector2(160f, 220f);

    public void DrawStartingHand(int cardsCount)
    {
        if (playerDeck == null)
        {
            Debug.LogError("[HandController] playerDeck не назначен.");
            return;
        }

        if (handRoot == null)
        {
            Debug.LogError("[HandController] handRoot не назначен.");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("[HandController] cardPrefab не назначен.");
            return;
        }

        PrepareHandRoot();
        ClearHandVisuals();

        List<CardData> cards = playerDeck.DrawCards(cardsCount);

        foreach (CardData card in cards)
        {
            BattleHandCardView view = Instantiate(cardPrefab, handRoot, false);

            FixCardRectTransform(view);
            view.Setup(card, this);

            if (view.GetComponent<BattleHandCardDrag>() == null)
                view.gameObject.AddComponent<BattleHandCardDrag>();
        }

        Debug.Log($"[HandController] —оздано карт в UI руки: {cards.Count}");
    }

    public Transform GetDragLayer()
    {
        if (dragLayer != null)
            return dragLayer;

        Canvas canvas = handRoot != null ? handRoot.GetComponentInParent<Canvas>() : null;
        return canvas != null ? canvas.transform : handRoot;
    }

    public void UpdateDragHighlight(Vector2 screenPosition, CardData cardData)
    {
        HexTile tile = GetTileUnderScreenPosition(screenPosition);
        bool valid = CanPlayCardOnTile(cardData, tile);

        if (tileHighlighter != null)
            tileHighlighter.Show(tile, valid);
    }

    public void ClearDragHighlight()
    {
        if (tileHighlighter != null)
            tileHighlighter.Hide();
    }

    public bool TryPlayCardFromDrag(BattleHandCardView cardView, Vector2 screenPosition)
    {
        if (cardView == null || cardView.Data == null)
            return false;

        HexTile tile = GetTileUnderScreenPosition(screenPosition);

        if (!CanPlayCardOnTile(cardView.Data, tile))
        {
            Debug.Log("[HandController]  арту нельз€ поставить на этот тайл.");
            return false;
        }

        bool spawned = unitSpawner.TrySpawnUnit(cardView.Data, tile);

        if (!spawned)
            return false;

        playerDeck.PlayCardFromHand(cardView.Data);

        return true;
    }

    private bool CanPlayCardOnTile(CardData cardData, HexTile tile)
    {
        if (cardData == null)
            return false;

        if (cardData.unitPrefab == null)
            return false;

        if (tile == null)
            return false;

        if (!tile.IsWalkable)
            return false;

        if (unitSpawner != null && unitSpawner.IsTileOccupied(tile))
            return false;

        return true;
    }

    private HexTile GetTileUnderScreenPosition(Vector2 screenPosition)
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;

            if (gameplayCamera == null)
            {
                Debug.LogError("[HandController] gameplayCamera не назначена и Camera.main не найдена.");
                return null;
            }
        }

        Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();
            return tile;
        }

        return null;
    }

    private void PrepareHandRoot()
    {
        RectTransform rootRect = handRoot as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 0f);
            rootRect.sizeDelta = new Vector2(1000f, 200f);
        }

        HorizontalLayoutGroup layout = handRoot.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = handRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;

        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void FixCardRectTransform(BattleHandCardView view)
    {
        RectTransform rect = view.GetComponent<RectTransform>();

        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.sizeDelta = cardSize;

        LayoutElement layoutElement = view.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = view.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredWidth = cardSize.x;
        layoutElement.preferredHeight = cardSize.y;
        layoutElement.minWidth = cardSize.x;
        layoutElement.minHeight = cardSize.y;
    }

    private void ClearHandVisuals()
    {
        for (int i = handRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(handRoot.GetChild(i).gameObject);
        }
    }
}