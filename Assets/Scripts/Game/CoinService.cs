using UnityEngine;

public class CoinService : MonoBehaviour
{
    public static CoinService Instance { get; private set; }

    public int Coins { get; private set; } = 9999;

    private void Awake()
    {
        Instance = this;
    }

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }
}
