using UnityEngine;

public class CoinService : MonoBehaviour
{
    public static event System.Action CoinsChanged;

    public static CoinService Instance { get; private set; }

    public int Coins { get; private set; }

    private void Awake()
    {
        Instance = this;
        Coins = SaveService.Instance.Data.coins;
    }

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        CoinsChanged?.Invoke();
        SaveService.Instance.SetCoins(Coins);
        return true;
    }

    public void Refund(int amount)
    {
        Coins += amount;
        CoinsChanged?.Invoke();
        SaveService.Instance.SetCoins(Coins);
    }
}
