using System;
using UnityEngine;

public class PlayerEconomy : MonoBehaviour
{
    [Header("Gold")]
    [SerializeField] private int startingGold = 3;
    [SerializeField] private int goldPerTurn = 2;

    public int CurrentGold { get; private set; }
    public int GoldPerTurn => goldPerTurn;

    public event Action<int> OnGoldChanged;

    private void Awake()
    {
        CurrentGold = startingGold;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool CanAfford(int cost)
    {
        return CurrentGold >= cost;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerEconomy] Нельзя потратить отрицательное количество золота.");
            return false;
        }

        if (!CanAfford(amount))
        {
            Debug.Log($"[PlayerEconomy] Не хватает золота. Нужно: {amount}, есть: {CurrentGold}");
            return false;
        }

        CurrentGold -= amount;
        Debug.Log($"[PlayerEconomy] Потрачено золота: {amount}. Осталось: {CurrentGold}");

        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }

    public void AddTurnGold()
    {
        AddGold(goldPerTurn);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        CurrentGold += amount;
        Debug.Log($"[PlayerEconomy] Получено золота: {amount}. Теперь: {CurrentGold}");

        OnGoldChanged?.Invoke(CurrentGold);
    }
}
