using TMPro;
using UnityEngine;

public class EconomyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEconomy playerEconomy;
    [SerializeField] private TMP_Text goldText;

    private void Start()
    {
        if (playerEconomy == null)
        {
            Debug.LogError("[EconomyUI] playerEconomy не назначен.");
            return;
        }

        playerEconomy.OnGoldChanged += UpdateGoldText;
        UpdateGoldText(playerEconomy.CurrentGold);
    }

    private void OnDestroy()
    {
        if (playerEconomy != null)
            playerEconomy.OnGoldChanged -= UpdateGoldText;
    }

    private void UpdateGoldText(int gold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }
}
