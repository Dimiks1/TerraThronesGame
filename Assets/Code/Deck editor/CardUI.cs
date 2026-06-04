// CardUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI references")]
    public Image artworkImage; // перетащи сюда child Image компонента префаба
    public TextMeshProUGUI nameText; // опционально

    // Храним данные, если понадобится
    private CardData data;

    // Заполнить UI данными карты
    public void Setup(CardData cardData)
    {
        data = cardData;

        if (nameText != null)
            nameText.text = cardData.cardName;

        if (artworkImage != null)
        {
            if (cardData != null && cardData.artwork != null)
            {
                artworkImage.sprite = cardData.artwork;
                artworkImage.enabled = true;
                // artworkImage.SetNativeSize(); // если хочешь размер под sprite
            }
            else
            {
                artworkImage.sprite = null;
                artworkImage.enabled = false; // или оставь placeholder
            }
        }
        Debug.Log($"CardUI.Setup for {cardData.cardName} - artwork sprite: {(cardData.artwork ? cardData.artwork.name : "null")}");
    }

    // опционально можно получить CardData
    public CardData GetCardData() => data;
}


