using System.Collections.Generic;
using UnityEngine;

public class TileHighlighter : MonoBehaviour
{
    [Header("Single Drag Colors")]
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Movement Colors")]
    [SerializeField] private Color movementColor = new Color(0.25f, 0.65f, 1f, 1f);
    [SerializeField] private Color selectedUnitColor = new Color(1f, 1f, 0.25f, 1f);

    private readonly Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

    private HexTile currentDragTile;
    private Renderer currentDragRenderer;

    public void Show(HexTile tile, bool isValid)
    {
        if (tile == null)
        {
            Hide();
            return;
        }

        if (currentDragTile == tile)
        {
            SetRendererColor(currentDragRenderer, isValid ? validColor : invalidColor);
            return;
        }

        Hide();

        currentDragTile = tile;
        currentDragRenderer = tile.GetComponentInChildren<Renderer>();

        RememberOriginalColor(currentDragRenderer);
        SetRendererColor(currentDragRenderer, isValid ? validColor : invalidColor);
    }

    public void Hide()
    {
        if (currentDragRenderer != null)
            RestoreRenderer(currentDragRenderer);

        currentDragTile = null;
        currentDragRenderer = null;
    }

    public void HighlightMovementTiles(IEnumerable<HexTile> tiles)
    {
        ClearAreaHighlights();

        if (tiles == null)
            return;

        foreach (HexTile tile in tiles)
        {
            Renderer renderer = tile != null ? tile.GetComponentInChildren<Renderer>() : null;
            RememberOriginalColor(renderer);
            SetRendererColor(renderer, movementColor);
        }
    }

    public void HighlightSelectedUnitTile(HexTile tile)
    {
        if (tile == null)
            return;

        Renderer renderer = tile.GetComponentInChildren<Renderer>();
        RememberOriginalColor(renderer);
        SetRendererColor(renderer, selectedUnitColor);
    }

    public void ClearAreaHighlights()
    {
        List<Renderer> renderers = new List<Renderer>(originalColors.Keys);

        foreach (Renderer renderer in renderers)
        {
            RestoreRenderer(renderer);
        }

        originalColors.Clear();

        currentDragTile = null;
        currentDragRenderer = null;
    }

    private void RememberOriginalColor(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (!originalColors.ContainsKey(renderer))
            originalColors.Add(renderer, renderer.material.color);
    }

    private void RestoreRenderer(Renderer renderer)
    {
        if (renderer == null)
            return;

        if (originalColors.TryGetValue(renderer, out Color originalColor))
            renderer.material.color = originalColor;
    }

    private void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.material.color = color;
    }
}