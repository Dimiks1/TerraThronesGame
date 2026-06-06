using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UnitSelectionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private HexGridGenerator gridGenerator;
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private TileHighlighter tileHighlighter;
    [SerializeField] private UnitInfoPanel unitInfoPanel;
    [SerializeField] private TurnManager turnManager;

    [Header("Raycast Layers")]
    [SerializeField] private LayerMask unitLayerMask;
    [SerializeField] private LayerMask tileLayerMask = ~0;

    private BattleUnit selectedUnit;
    private readonly HashSet<HexTile> reachableTiles = new HashSet<HexTile>();

    private void Start()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (gridGenerator == null)
            gridGenerator = FindFirstObjectByType<HexGridGenerator>();

        Debug.Log("[UnitSelectionController] Initialized.");
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (turnManager != null && !turnManager.IsPlayerTurn())
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        HandleLeftClick(mousePosition);
    }

    private void HandleLeftClick(Vector2 screenPosition)
    {
        BattleUnit clickedUnit = GetUnitUnderScreenPosition(screenPosition);

        if (clickedUnit != null)
        {
            SelectUnit(clickedUnit);
            return;
        }

        HexTile clickedTile = GetTileUnderScreenPosition(screenPosition);

        if (selectedUnit != null && clickedTile != null && reachableTiles.Contains(clickedTile))
        {
            TryMoveSelectedUnit(clickedTile);
            return;
        }

        ClearSelection();
    }

    private BattleUnit GetUnitUnderScreenPosition(Vector2 screenPosition)
    {
        if (gameplayCamera == null)
        {
            Debug.LogError("[UnitSelectionController] gameplayCamera == null");
            return null;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, unitLayerMask, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"[UnitSelectionController] Hit unit layer object: {hit.collider.name}");

            BattleUnit unit = hit.collider.GetComponentInParent<BattleUnit>();

            if (unit != null)
            {
                Debug.Log($"[UnitSelectionController] Raycast hit unit: {unit.SourceCard.cardName}");
                return unit;
            }

            Debug.LogWarning($"[UnitSelectionController] Попали в объект слоя Unit, но BattleUnit в родителях не найден: {hit.collider.name}");
        }
        else
        {
            Debug.Log("[UnitSelectionController] Unit raycast hit nothing.");
        }

        return null;
    }

    private HexTile GetTileUnderScreenPosition(Vector2 screenPosition)
    {
        if (gameplayCamera == null)
            return null;

        Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, tileLayerMask, QueryTriggerInteraction.Ignore))
        {
            HexTile tile = hit.collider.GetComponentInParent<HexTile>();

            if (tile != null)
                Debug.Log($"[UnitSelectionController] Raycast hit tile: {tile.Coordinates}");

            return tile;
        }

        return null;
    }

    private void SelectUnit(BattleUnit unit)
    {
        if (unit == null)
            return;

        selectedUnit = unit;

        if (unitInfoPanel != null)
            unitInfoPanel.ShowUnit(selectedUnit);

        BuildReachableTiles(selectedUnit);

        if (tileHighlighter != null)
        {
            tileHighlighter.ClearAreaHighlights();
            tileHighlighter.HighlightMovementTiles(reachableTiles);
            tileHighlighter.HighlightSelectedUnitTile(selectedUnit.CurrentTile);
        }

        Debug.Log($"[UnitSelectionController] Выбран юнит: {unit.SourceCard.cardName}");
    }

    private void TryMoveSelectedUnit(HexTile targetTile)
    {
        if (selectedUnit == null || targetTile == null)
            return;

        if (!selectedUnit.CanMove)
        {
            Debug.Log("[UnitSelectionController] Этот объект не может двигаться.");
            return;
        }

        if (selectedUnit.HasMovedThisTurn)
        {
            Debug.Log("[UnitSelectionController] Этот юнит уже ходил в этом ходу.");
            return;
        }

        if (!reachableTiles.Contains(targetTile))
        {
            Debug.Log("[UnitSelectionController] Этот тайл недоступен для движения.");
            return;
        }

        bool moved = unitSpawner.TryMoveUnit(selectedUnit, targetTile);

        if (!moved)
            return;

        if (unitInfoPanel != null)
            unitInfoPanel.ShowUnit(selectedUnit);

        ClearMovementHighlightsOnly();

        Debug.Log("[UnitSelectionController] Юнит перемещён.");
    }

    private void BuildReachableTiles(BattleUnit unit)
    {
        reachableTiles.Clear();

        if (unit == null || unit.CurrentTile == null || gridGenerator == null)
            return;

        if (!unit.CanMove)
            return;

        if (unit.HasMovedThisTurn)
            return;

        Queue<HexTile> frontier = new Queue<HexTile>();
        Dictionary<HexTile, int> distance = new Dictionary<HexTile, int>();

        frontier.Enqueue(unit.CurrentTile);
        distance[unit.CurrentTile] = 0;

        while (frontier.Count > 0)
        {
            HexTile current = frontier.Dequeue();
            int currentDistance = distance[current];

            foreach (HexTile neighbor in GetNeighbors(current))
            {
                if (neighbor == null)
                    continue;

                if (!neighbor.IsWalkable)
                    continue;

                if (unitSpawner != null && unitSpawner.IsTileOccupied(neighbor))
                    continue;

                int nextDistance = currentDistance + 1;

                if (nextDistance > unit.MoveRange)
                    continue;

                if (distance.ContainsKey(neighbor))
                    continue;

                distance[neighbor] = nextDistance;
                frontier.Enqueue(neighbor);
                reachableTiles.Add(neighbor);
            }
        }

        Debug.Log($"[UnitSelectionController] Reachable tiles count: {reachableTiles.Count}");
    }

    private IEnumerable<HexTile> GetNeighbors(HexTile tile)
    {
        if (tile == null || gridGenerator == null)
            yield break;

        Vector2Int[] dirs =
        {
            new Vector2Int(+1, 0),
            new Vector2Int(+1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, +1),
            new Vector2Int(0, +1)
        };

        foreach (Vector2Int dir in dirs)
        {
            HexTile neighbor = gridGenerator.GetHexAt(tile.Coordinates + dir);

            if (neighbor != null)
                yield return neighbor;
        }
    }

    private void ClearMovementHighlightsOnly()
    {
        reachableTiles.Clear();

        if (tileHighlighter != null)
            tileHighlighter.ClearAreaHighlights();
    }

    private void ClearSelection()
    {
        selectedUnit = null;
        reachableTiles.Clear();

        if (unitInfoPanel != null)
            unitInfoPanel.Hide();

        if (tileHighlighter != null)
            tileHighlighter.ClearAreaHighlights();

        Debug.Log("[UnitSelectionController] Выбор очищен.");
    }
}