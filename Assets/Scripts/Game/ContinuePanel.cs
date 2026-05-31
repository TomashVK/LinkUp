using TMPro;
using UnityEngine;

public class ContinuePanel : MonoBehaviour
{
    [SerializeField] private MoveCounter moveCounter;
    [SerializeField] private TMP_Text coinBalanceText;
    [SerializeField] private int movesPerContinue = 5;
    [SerializeField] private int coinCost = 100;

    public void Show()
    {
        if (coinBalanceText != null && CoinService.Instance != null)
            coinBalanceText.text = $"Coins: {CoinService.Instance.Coins}";
        gameObject.SetActive(true);
    }

    public void OnWatchAd()
    {
        moveCounter.AddMoves(movesPerContinue);
        gameObject.SetActive(false);
    }

    public void OnUseCoins()
    {
        if (CoinService.Instance == null || !CoinService.Instance.TrySpend(coinCost)) return;
        moveCounter.AddMoves(movesPerContinue);
        gameObject.SetActive(false);
    }
}
