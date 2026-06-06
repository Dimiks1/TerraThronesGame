using System;
using System.Collections;
using UnityEngine;

public enum BattleTurnOwner
{
    Player,
    Enemy
}

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEconomy playerEconomy;
    [SerializeField] private UnitSpawner unitSpawner;

    [Header("Settings")]
    [SerializeField] private bool skipEnemyTurnForNow = true;
    [SerializeField] private float enemyTurnDelay = 0.5f;

    [SerializeField] private HandController handController;
    [SerializeField] private bool drawCardOnPlayerTurnStart = true;
    [SerializeField] private bool drawCardOnFirstTurn = false;

    public BattleTurnOwner CurrentTurnOwner { get; private set; } = BattleTurnOwner.Player;
    public int TurnNumber { get; private set; } = 1;

    public event Action<BattleTurnOwner, int> OnTurnChanged;

    private bool isChangingTurn;

    private void Start()
    {
        StartPlayerTurn(drawCardOnFirstTurn);
    }

    public bool IsPlayerTurn()
    {
        return CurrentTurnOwner == BattleTurnOwner.Player && !isChangingTurn;
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn())
        {
            Debug.Log("[TurnManager] Сейчас нельзя закончить ход игрока.");
            return;
        }

        StartCoroutine(EndPlayerTurnRoutine());
    }

    private IEnumerator EndPlayerTurnRoutine()
    {
        isChangingTurn = true;

        CurrentTurnOwner = BattleTurnOwner.Enemy;
        OnTurnChanged?.Invoke(CurrentTurnOwner, TurnNumber);

        Debug.Log("[TurnManager] Ход игрока завершён. Ход противника.");

        if (skipEnemyTurnForNow)
        {
            yield return new WaitForSeconds(enemyTurnDelay);
            EndEnemyTurn();
        }

        isChangingTurn = false;
    }

    private void EndEnemyTurn()
    {
        TurnNumber++;
        StartPlayerTurn(drawCardOnPlayerTurnStart);
    }

    private void StartPlayerTurn(bool drawCard)
    {
        CurrentTurnOwner = BattleTurnOwner.Player;

        if (playerEconomy != null)
            playerEconomy.AddTurnGold();

        if (unitSpawner != null)
            unitSpawner.ResetAllPlayerUnitsTurnState();

        if (drawCard && handController != null)
            handController.DrawCardToHand();

        OnTurnChanged?.Invoke(CurrentTurnOwner, TurnNumber);

        Debug.Log($"[TurnManager] Начался ход игрока. Turn: {TurnNumber}");
    }
}