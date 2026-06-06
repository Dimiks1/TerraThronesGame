using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text moveRangeText;
    [SerializeField] private TMP_Text stateText;

    private void Awake()
    {
        Hide();
    }

    public void ShowUnit(BattleUnit unit)
    {
        if (unit == null || unit.SourceCard == null)
        {
            Hide();
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        CardData card = unit.SourceCard;

        if (artworkImage != null)
        {
            artworkImage.sprite = card.artwork;
            artworkImage.enabled = card.artwork != null;
        }

        if (nameText != null)
            nameText.text = card.cardName;

        if (healthText != null)
            healthText.text = $"HP: {unit.CurrentHealth}";

        if (attackText != null)
            attackText.text = $"ATK: {unit.Attack}";

        if (rangeText != null)
            rangeText.text = $"Range: {unit.Range}";

        if (moveRangeText != null)
            moveRangeText.text = $"Move: {unit.MoveRange}";

        if (stateText != null)
            stateText.text = unit.HasMovedThisTurn ? "Already moved" : "Ready";
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
