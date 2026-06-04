using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BattleHandCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text nameText;

    public CardData Data { get; private set; }
    public HandController Owner { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    private void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();

        if (artworkImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);

            foreach (Image image in images)
            {
                if (image.gameObject != gameObject)
                {
                    artworkImage = image;
                    break;
                }
            }

            if (artworkImage == null && images.Length > 0)
                artworkImage = images[0];
        }

        if (nameText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
                nameText = texts[0];
        }
    }

    public void Setup(CardData cardData, HandController owner)
    {
        Data = cardData;
        Owner = owner;

        if (cardData == null)
        {
            Debug.LogWarning("[BattleHandCardView] cardData == null");
            return;
        }

        if (artworkImage != null)
        {
            artworkImage.sprite = cardData.artwork;
            artworkImage.enabled = cardData.artwork != null;
        }

        if (nameText != null)
            nameText.text = cardData.cardName;
    }
}