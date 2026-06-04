using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(BattleHandCardView))]
public class BattleHandCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BattleHandCardView cardView;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private LayoutElement layoutElement;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;

    private bool isDragging;

    private void Awake()
    {
        cardView = GetComponent<BattleHandCardView>();
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        layoutElement = GetComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardView == null || cardView.Data == null || cardView.Owner == null)
            return;

        isDragging = true;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;

        if (cardView.CanvasGroup != null)
        {
            cardView.CanvasGroup.blocksRaycasts = false;
            cardView.CanvasGroup.alpha = 0.85f;
        }

        Transform dragParent = cardView.Owner.GetDragLayer();

        if (dragParent != null)
        {
            transform.SetParent(dragParent, false);
            transform.SetAsLastSibling();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localScale = Vector3.one;

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        MoveToPointer(eventData);
        cardView.Owner.UpdateDragHighlight(eventData.position, cardView.Data);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (cardView.CanvasGroup != null)
        {
            cardView.CanvasGroup.blocksRaycasts = true;
            cardView.CanvasGroup.alpha = 1f;
        }

        bool played = cardView.Owner.TryPlayCardFromDrag(cardView, eventData.position);

        if (played)
        {
            Destroy(gameObject);
        }
        else
        {
            ReturnToHand();
        }

        cardView.Owner.ClearDragHighlight();
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (rectTransform == null)
            return;

        RectTransform parentRect = rectTransform.parent as RectTransform;

        if (parentRect == null)
            return;

        Camera uiCamera = null;

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    private void ReturnToHand()
    {
        transform.SetParent(originalParent, false);
        transform.SetSiblingIndex(originalSiblingIndex);

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localScale = Vector3.one;

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;
    }
}