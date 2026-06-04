using UnityEngine;
using UnityEngine.UI;

public class DeckCardUI : MonoBehaviour
{
    public Image artworkImage;     // картинка мини-карты
    private CardData data;         // какие данные отображаем
    private DeckEditor owner;      // кто нас создал (чтобы позвать удаление)

    public void Setup(CardData card, DeckEditor deckEditor)
    {
        data = card;
        owner = deckEditor;

        if (artworkImage != null && data != null && data.artwork != null)
            artworkImage.sprite = data.artwork;

        // кликом по мини-карте удаляем её из колоды
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => owner.RemoveCardFromDeck(this));
        }
    }

    public CardData Data => data;
}

