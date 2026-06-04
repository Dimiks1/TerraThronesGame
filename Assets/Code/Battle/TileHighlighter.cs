using UnityEngine;

public class TileHighlighter : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 1f);

    private HexTile currentTile;
    private Renderer currentRenderer;
    private Color originalColor;
    private bool hasOriginalColor;

    public void Show(HexTile tile, bool isValid)
    {
        if (tile == null)
        {
            Hide();
            return;
        }

        if (currentTile == tile)
        {
            ApplyColor(isValid);
            return;
        }

        Hide();

        currentTile = tile;
        currentRenderer = tile.GetComponentInChildren<Renderer>();

        if (currentRenderer == null)
            return;

        originalColor = currentRenderer.material.color;
        hasOriginalColor = true;

        ApplyColor(isValid);
    }

    public void Hide()
    {
        if (currentRenderer != null && hasOriginalColor)
        {
            currentRenderer.material.color = originalColor;
        }

        currentTile = null;
        currentRenderer = null;
        hasOriginalColor = false;
    }

    private void ApplyColor(bool isValid)
    {
        if (currentRenderer == null)
            return;

        currentRenderer.material.color = isValid ? validColor : invalidColor;
    }
}
