using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private Button endTurnButton;

    private void Start()
    {
        if (turnManager == null)
        {
            Debug.LogError("[TurnUI] turnManager не назначен.");
            return;
        }

        turnManager.OnTurnChanged += UpdateTurnUI;

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        UpdateTurnUI(turnManager.CurrentTurnOwner, turnManager.TurnNumber);
    }

    private void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= UpdateTurnUI;
    }

    private void OnEndTurnClicked()
    {
        turnManager.EndPlayerTurn();
    }

    private void UpdateTurnUI(BattleTurnOwner owner, int turnNumber)
    {
        if (turnText != null)
            turnText.text = $"Turn {turnNumber}: {owner}";

        if (endTurnButton != null)
            endTurnButton.interactable = owner == BattleTurnOwner.Player;
    }
}