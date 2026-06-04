using UnityEngine;

[CreateAssetMenu(fileName = "HexTileType", menuName = "Hex/TyleType", order = 0)]
public class HexTileType : ScriptableObject
{
    [Header("Identity")]
    public string tileId;
    public string tileName;

    [Header("Rendering")]
    public GameObject modelPrefab;
    public Material material;

    [Header("Gameplay")]
    public bool isWalcable = true;
    public int movementCost = 1;

    [Header("Optional metadata")]
    public Color gizmoColor = Color.green;
}
